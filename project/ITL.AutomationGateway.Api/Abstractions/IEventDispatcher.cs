using ITL.AutomationGateway.Api.Domain;

namespace ITL.AutomationGateway.Api.Abstractions;

public interface IEventDispatcher
{
    Task PublishJobEventAsync(string eventType, GatewayJob job, CancellationToken ct);
}
