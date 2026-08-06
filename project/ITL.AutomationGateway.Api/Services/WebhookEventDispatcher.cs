using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ITL.AutomationGateway.Api.Abstractions;
using ITL.AutomationGateway.Api.Domain;
using Microsoft.Extensions.Options;

namespace ITL.AutomationGateway.Api.Services;

public sealed class WebhookEventDispatcher : IEventDispatcher
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhookOptions _options;
    private readonly ILogger<WebhookEventDispatcher> _logger;

    public WebhookEventDispatcher(
        IWebhookSubscriptionRepository subscriptionRepository,
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookOptions> options,
        ILogger<WebhookEventDispatcher> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishJobEventAsync(string eventType, GatewayJob job, CancellationToken ct)
    {
        var subscriptions = await _subscriptionRepository.ListByStationAsync(job.StationId, ct);
        if (subscriptions.Count == 0)
        {
            return;
        }

        var payload = new
        {
            eventType,
            eventId = Guid.NewGuid(),
            stationId = job.StationId,
            jobId = job.JobId,
            operation = job.Operation,
            sn = job.Sn,
            port = job.Port,
            status = job.Status.ToString(),
            resultJson = job.ResultJson,
            errorCode = job.ErrorCode,
            errorMessage = job.ErrorMessage,
            createdUtc = DateTimeOffset.UtcNow,
        };

        var body = JsonSerializer.Serialize(payload);
        var httpClient = _httpClientFactory.CreateClient("webhook");

        foreach (var subscription in subscriptions)
        {
            await DeliverWithRetryAsync(httpClient, subscription, body, ct);
        }
    }

    private async Task DeliverWithRetryAsync(HttpClient httpClient, WebhookSubscription subscription, string body, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= Math.Max(1, _options.MaxAttempts); attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
                request.Content = JsonContent.Create(JsonDocument.Parse(body).RootElement);
                request.Headers.TryAddWithoutValidation("X-Gateway-Event-Timestamp", DateTimeOffset.UtcNow.ToString("O"));
                request.Headers.TryAddWithoutValidation("X-Gateway-Attempt", attempt.ToString());

                if (!string.IsNullOrWhiteSpace(subscription.Secret))
                {
                    var signature = Sign(body, subscription.Secret);
                    request.Headers.TryAddWithoutValidation("X-Gateway-Signature", signature);
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.DeliveryTimeoutSec)));

                var response = await httpClient.SendAsync(request, timeoutCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                _logger.LogWarning(
                    "Webhook delivery failed. stationId={StationId} url={Url} statusCode={StatusCode} attempt={Attempt}",
                    subscription.StationId,
                    subscription.Url,
                    (int)response.StatusCode,
                    attempt);
            }
            catch (Exception ex) when (attempt < Math.Max(1, _options.MaxAttempts))
            {
                _logger.LogWarning(
                    ex,
                    "Webhook delivery exception. stationId={StationId} url={Url} attempt={Attempt}",
                    subscription.StationId,
                    subscription.Url,
                    attempt);
            }
        }
    }

    private static string Sign(string body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash);
    }
}
