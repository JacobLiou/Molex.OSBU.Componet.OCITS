namespace ITL.AutomationGateway.Api.Domain;

public enum JobStatus
{
    Queued,
    Dispatched,
    Running,
    WaitingAck,
    Succeeded,
    Failed,
    Timeout,
    Canceled,
}

public sealed class GatewayJob
{
    public Guid JobId { get; init; }

    public string StationId { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string? Sn { get; init; }

    public string? Port { get; init; }

    public string? ParametersJson { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public JobStatus Status { get; init; }

    public string? ResultJson { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset UpdatedUtc { get; init; }

    public DateTimeOffset? StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }
}

public sealed record JobSubmitModel(
    string StationId,
    string Operation,
    string? Sn,
    string? Port,
    IReadOnlyDictionary<string, string>? Parameters,
    string IdempotencyKey,
    int? TimeoutSec);

public sealed record JobSubmitResult(Guid JobId, bool IsDuplicate, JobStatus Status);

public sealed class WebhookSubscription
{
    public Guid SubscriptionId { get; init; }

    public string StationId { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string? Secret { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }
}

