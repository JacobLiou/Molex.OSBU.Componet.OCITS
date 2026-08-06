namespace ITL.AutomationGateway.Api.Abstractions;

public interface ILegacyAutomationAdapter
{
    bool IsConnected { get; }

    Task<string> SendCommandAndWaitAsync(
        string command,
        Func<string, bool> isExpectedAck,
        TimeSpan timeout,
        CancellationToken ct);
}
