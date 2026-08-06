using ITL.AutomationGateway.Api.Domain;

namespace ITL.AutomationGateway.Api.Abstractions;

public interface IWebhookSubscriptionRepository
{
    Task CreateAsync(WebhookSubscription subscription, CancellationToken ct);

    Task<IReadOnlyList<WebhookSubscription>> ListByStationAsync(string stationId, CancellationToken ct);
}
