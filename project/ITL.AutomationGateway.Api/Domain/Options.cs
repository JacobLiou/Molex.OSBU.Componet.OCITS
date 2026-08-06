namespace ITL.AutomationGateway.Api.Domain;

public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    public int DefaultCommandTimeoutSec { get; set; } = 300;
}

public sealed class LegacyTcpOptions
{
    public const string SectionName = "LegacyTcp";

    public string ListenIp { get; set; } = "0.0.0.0";

    public int ListenPort { get; set; } = 9100;
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string SqlitePath { get; set; } = "data/gateway.db";
}

public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    public int DeliveryTimeoutSec { get; set; } = 10;

    public int MaxAttempts { get; set; } = 2;
}
