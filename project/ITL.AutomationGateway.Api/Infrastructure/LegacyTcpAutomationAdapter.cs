using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using ITL.AutomationGateway.Api.Abstractions;
using ITL.AutomationGateway.Api.Domain;
using Microsoft.Extensions.Options;

namespace ITL.AutomationGateway.Api.Infrastructure;

public sealed class LegacyTcpAutomationAdapter : BackgroundService, ILegacyAutomationAdapter
{
    private readonly LegacyTcpOptions _options;
    private readonly ILogger<LegacyTcpAutomationAdapter> _logger;
    private readonly Channel<string> _incoming = Channel.CreateUnbounded<string>();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _clientLock = new();

    private TcpListener? _listener;
    private TcpClient? _client;

    public LegacyTcpAutomationAdapter(IOptions<LegacyTcpOptions> options, ILogger<LegacyTcpAutomationAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected
    {
        get
        {
            lock (_clientLock)
            {
                return _client is { Connected: true };
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ip = IPAddress.Parse(_options.ListenIp);
        _listener = new TcpListener(ip, _options.ListenPort);
        _listener.Start();
        _logger.LogInformation("Legacy TCP gateway listening at {Ip}:{Port}", _options.ListenIp, _options.ListenPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                AttachClient(client);
                _ = Task.Run(() => ReadLoopAsync(client, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Accept client failed.");
            }
        }
    }

    public async Task<string> SendCommandAndWaitAsync(
        string command,
        Func<string, bool> isExpectedAck,
        TimeSpan timeout,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(command))
        {
            throw new ArgumentException("Command cannot be empty.", nameof(command));
        }

        await _sendLock.WaitAsync(ct);
        try
        {
            var client = GetConnectedClient();
            var stream = client.GetStream();

            DrainIncomingFrames();

            var bytes = Encoding.GetEncoding("gb2312").GetBytes(command);
            await stream.WriteAsync(bytes, ct);
            await stream.FlushAsync(ct);

            _logger.LogInformation("Legacy sent command: {Command}", command.Trim());

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);

            while (!timeoutCts.IsCancellationRequested)
            {
                var frame = await _incoming.Reader.ReadAsync(timeoutCts.Token);
                _logger.LogInformation("Legacy frame received: {Frame}", frame);
                if (isExpectedAck(frame))
                {
                    return frame;
                }
            }

            throw new TimeoutException("No expected ACK frame received in time.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Timed out waiting for ACK from legacy client.");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        lock (_clientLock)
        {
            _client?.Close();
            _client?.Dispose();
            _client = null;
        }
        _listener?.Stop();
        _sendLock.Dispose();
    }

    private void AttachClient(TcpClient client)
    {
        lock (_clientLock)
        {
            _client?.Close();
            _client?.Dispose();
            _client = client;
        }

        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogInformation("Legacy client connected from {Endpoint}", endpoint);
    }

    private async Task ReadLoopAsync(TcpClient client, CancellationToken ct)
    {
        var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var stream = client.GetStream();
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested && client.Connected)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read <= 0)
                {
                    break;
                }

                var chunk = Encoding.GetEncoding("gb2312").GetString(buffer, 0, read).Replace("\0", string.Empty);
                sb.Append(chunk);
                EmitFrames(sb);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Legacy read loop failed for {Endpoint}", endpoint);
        }
        finally
        {
            _logger.LogWarning("Legacy client disconnected from {Endpoint}", endpoint);
            lock (_clientLock)
            {
                if (ReferenceEquals(_client, client))
                {
                    _client = null;
                }
            }
            client.Dispose();
        }
    }

    private void EmitFrames(StringBuilder sb)
    {
        while (true)
        {
            var all = sb.ToString();
            var idx = all.IndexOf("\r\n", StringComparison.Ordinal);
            if (idx < 0)
            {
                return;
            }

            var frame = all[..idx].Trim();
            sb.Remove(0, idx + 2);
            if (!string.IsNullOrWhiteSpace(frame))
            {
                _incoming.Writer.TryWrite(frame);
            }
        }
    }

    private void DrainIncomingFrames()
    {
        while (_incoming.Reader.TryRead(out _))
        {
        }
    }

    private TcpClient GetConnectedClient()
    {
        lock (_clientLock)
        {
            if (_client is not { Connected: true })
            {
                throw new InvalidOperationException("Legacy client is not connected.");
            }

            return _client;
        }
    }
}
