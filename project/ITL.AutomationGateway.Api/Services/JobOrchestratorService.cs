using System.Text.Json;
using System.Threading.Channels;
using ITL.AutomationGateway.Api.Abstractions;
using ITL.AutomationGateway.Api.Domain;
using Microsoft.Extensions.Options;

namespace ITL.AutomationGateway.Api.Services;

public sealed class JobOrchestratorService : BackgroundService, IJobOrchestrator
{
    private readonly IJobRepository _repo;
    private readonly ILegacyAutomationAdapter _adapter;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly ILogger<JobOrchestratorService> _logger;
    private readonly GatewayOptions _options;
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

    public JobOrchestratorService(
        IJobRepository repo,
        ILegacyAutomationAdapter adapter,
        IEventDispatcher eventDispatcher,
        IOptions<GatewayOptions> options,
        ILogger<JobOrchestratorService> logger)
    {
        _repo = repo;
        _adapter = adapter;
        _eventDispatcher = eventDispatcher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<JobSubmitResult> SubmitAsync(JobSubmitModel submit, CancellationToken ct)
    {
        var op = submit.Operation.Trim().ToLowerInvariant();
        if (!OperationMap.ContainsKey(op))
        {
            throw new NotSupportedException($"Unsupported operation: {submit.Operation}");
        }

        var existing = await _repo.GetByIdempotencyAsync(submit.StationId, submit.IdempotencyKey, ct);
        if (existing is not null)
        {
            return new JobSubmitResult(existing.JobId, true, existing.Status);
        }

        var now = DateTimeOffset.UtcNow;
        var job = new GatewayJob
        {
            JobId = Guid.NewGuid(),
            StationId = submit.StationId,
            Operation = op,
            Sn = submit.Sn,
            Port = submit.Port,
            ParametersJson = submit.Parameters is null ? null : JsonSerializer.Serialize(submit.Parameters),
            IdempotencyKey = submit.IdempotencyKey,
            Status = JobStatus.Queued,
            CreatedUtc = now,
            UpdatedUtc = now,
        };

        await _repo.CreateQueuedAsync(job, ct);
        await _queue.Writer.WriteAsync(job.JobId, ct);

        await _eventDispatcher.PublishJobEventAsync("job.accepted", job, ct);

        return new JobSubmitResult(job.JobId, false, JobStatus.Queued);
    }

    public Task<bool> CancelAsync(Guid jobId, CancellationToken ct)
    {
        return _repo.CancelQueuedAsync(jobId, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queued = await _repo.ListQueuedJobIdsAsync(stoppingToken);
        foreach (var jobId in queued)
        {
            await _queue.Writer.WriteAsync(jobId, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;
            try
            {
                jobId = await _queue.Reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await ProcessAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Process job failed unexpectedly. jobId={JobId}", jobId);
                await _repo.UpdateStateAsync(jobId, JobStatus.Failed, null, ErrorCodes.Unhandled, ex.Message, stoppingToken);
                await PublishCurrentAsync(jobId, "job.failed", stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _repo.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            return;
        }

        if (job.Status is JobStatus.Canceled or JobStatus.Succeeded or JobStatus.Failed or JobStatus.Timeout)
        {
            return;
        }

        await _repo.UpdateStateAsync(jobId, JobStatus.Dispatched, null, null, null, ct);
        await PublishCurrentAsync(jobId, "job.dispatched", ct);
        await _repo.UpdateStateAsync(jobId, JobStatus.Running, null, null, null, ct);
        await PublishCurrentAsync(jobId, "job.running", ct);

        var spec = BuildCommandSpec(job);
        var timeout = TimeSpan.FromSeconds(spec.TimeoutSec);

        try
        {
            await _repo.UpdateStateAsync(jobId, JobStatus.WaitingAck, null, null, null, ct);
            await PublishCurrentAsync(jobId, "job.waiting_ack", ct);
            var ack = await _adapter.SendCommandAndWaitAsync(spec.Command, spec.AckPredicate, timeout, ct);
            var resultJson = JsonSerializer.Serialize(new
            {
                ack,
                operation = job.Operation,
                sn = job.Sn,
                port = job.Port,
                finishedUtc = DateTimeOffset.UtcNow,
            });
            await _repo.UpdateStateAsync(jobId, JobStatus.Succeeded, resultJson, null, null, ct);
            await PublishCurrentAsync(jobId, "job.succeeded", ct);
        }
        catch (TimeoutException ex)
        {
            await _repo.UpdateStateAsync(jobId, JobStatus.Timeout, null, ErrorCodes.Timeout, ex.Message, ct);
            await PublishCurrentAsync(jobId, "job.timeout", ct);
        }
        catch (InvalidOperationException ex)
        {
            await _repo.UpdateStateAsync(jobId, JobStatus.Failed, null, ErrorCodes.AdapterDisconnected, ex.Message, ct);
            await PublishCurrentAsync(jobId, "job.failed", ct);
        }
        catch (NotSupportedException ex)
        {
            await _repo.UpdateStateAsync(jobId, JobStatus.Failed, null, ErrorCodes.OperationNotSupported, ex.Message, ct);
            await PublishCurrentAsync(jobId, "job.failed", ct);
        }
        catch (Exception ex)
        {
            await _repo.UpdateStateAsync(jobId, JobStatus.Failed, null, ErrorCodes.LegacyError, ex.Message, ct);
            await PublishCurrentAsync(jobId, "job.failed", ct);
        }
    }

    private async Task PublishCurrentAsync(Guid jobId, string eventType, CancellationToken ct)
    {
        var current = await _repo.GetByIdAsync(jobId, ct);
        if (current is not null)
        {
            await _eventDispatcher.PublishJobEventAsync(eventType, current, ct);
        }
    }

    private CommandSpec BuildCommandSpec(GatewayJob job)
    {
        var timeoutSec = _options.DefaultCommandTimeoutSec;
        var spec = OperationMap[job.Operation](job);
        return spec with { TimeoutSec = spec.TimeoutSec <= 0 ? timeoutSec : spec.TimeoutSec };
    }

    private static readonly Dictionary<string, Func<GatewayJob, CommandSpec>> OperationMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["open_template"] = job =>
        {
            if (string.IsNullOrWhiteSpace(job.Sn))
            {
                throw new InvalidOperationException("open_template requires sn.");
            }

            return new CommandSpec(
                $"SNNO;{job.Sn}\r\n",
                ack => ack.StartsWith("SNNO;", StringComparison.OrdinalIgnoreCase),
                180);
        },
        ["scan_nopdl"] = _ => new CommandSpec(
            "TEST;NOPDL\r\n",
            ack => ack.StartsWith("TEST;PASS;", StringComparison.OrdinalIgnoreCase)
                || ack.StartsWith("TEST;FAIL", StringComparison.OrdinalIgnoreCase),
            900),
        ["scan_pdl"] = _ => new CommandSpec(
            "TEST;PDL\r\n",
            ack => ack.StartsWith("TEST;PASS;", StringComparison.OrdinalIgnoreCase)
                || ack.StartsWith("TEST;FAIL", StringComparison.OrdinalIgnoreCase),
            900),
        ["stop"] = _ => new CommandSpec(
            "TEST;STOP\r\n",
            ack => ack.StartsWith("TEST;", StringComparison.OrdinalIgnoreCase),
            60),
        ["uv_set"] = job =>
        {
            if (string.IsNullOrWhiteSpace(job.Sn))
            {
                throw new InvalidOperationException("uv_set requires sn.");
            }

            var enabled = "1";
            if (!string.IsNullOrWhiteSpace(job.ParametersJson))
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(job.ParametersJson);
                    if (dict is not null && dict.TryGetValue("enabled", out var value) && value == "0")
                    {
                        enabled = "0";
                    }
                }
                catch
                {
                }
            }

            return new CommandSpec(
                $"TEST;UV;{job.Sn};{enabled}\r\n",
                ack => ack.StartsWith("TEST;UV;", StringComparison.OrdinalIgnoreCase),
                120);
        },
    };

    private sealed record CommandSpec(
        string Command,
        Func<string, bool> AckPredicate,
        int TimeoutSec);
}
