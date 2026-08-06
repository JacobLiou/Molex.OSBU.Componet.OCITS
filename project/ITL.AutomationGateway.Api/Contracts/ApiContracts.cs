using ITL.AutomationGateway.Api.Domain;

namespace ITL.AutomationGateway.Api.Contracts;

public sealed record SubmitJobRequest(
    string Operation,
    string? Sn,
    string? Port,
    string? ClientReqId,
    int? TimeoutSec,
    Dictionary<string, string>? Parameters);

public sealed record ApiError(string Code, string Message);

public sealed record CreateWebhookSubscriptionRequest(string Url, string? Secret);

public sealed record WebhookSubscriptionView(
    Guid SubscriptionId,
    string StationId,
    string Url,
    DateTimeOffset CreatedUtc)
{
    public static WebhookSubscriptionView From(Domain.WebhookSubscription subscription)
    {
        return new WebhookSubscriptionView(
            subscription.SubscriptionId,
            subscription.StationId,
            subscription.Url,
            subscription.CreatedUtc);
    }
}

public sealed record JobView(
    Guid JobId,
    string StationId,
    string Operation,
    string? Sn,
    string? Port,
    string Status,
    string? ResultJson,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc)
{
    public static JobView From(GatewayJob job)
    {
        return new JobView(
            job.JobId,
            job.StationId,
            job.Operation,
            job.Sn,
            job.Port,
            job.Status.ToString(),
            job.ResultJson,
            job.ErrorCode,
            job.ErrorMessage,
            job.CreatedUtc,
            job.UpdatedUtc,
            job.StartedUtc,
            job.CompletedUtc);
    }
}
