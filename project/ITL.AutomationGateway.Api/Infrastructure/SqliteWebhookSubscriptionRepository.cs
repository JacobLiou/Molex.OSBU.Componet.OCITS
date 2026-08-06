using ITL.AutomationGateway.Api.Abstractions;
using ITL.AutomationGateway.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ITL.AutomationGateway.Api.Infrastructure;

public sealed class SqliteWebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly string _connectionString;

    public SqliteWebhookSubscriptionRepository(IOptions<StorageOptions> storageOptions)
    {
        var dbPath = storageOptions.Value.SqlitePath;
        if (Path.IsPathRooted(dbPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = $"Data Source={dbPath};Cache=Shared";
        }
        else
        {
            var rooted = Path.Combine(Environment.CurrentDirectory, dbPath);
            Directory.CreateDirectory(Path.GetDirectoryName(rooted)!);
            _connectionString = $"Data Source={rooted};Cache=Shared";
        }
    }

    public async Task CreateAsync(WebhookSubscription subscription, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO webhook_subscriptions
            (
                subscription_id,
                station_id,
                url,
                secret,
                created_utc
            )
            VALUES
            (
                $subscriptionId,
                $stationId,
                $url,
                $secret,
                $createdUtc
            );
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$subscriptionId", subscription.SubscriptionId.ToString());
        cmd.Parameters.AddWithValue("$stationId", subscription.StationId);
        cmd.Parameters.AddWithValue("$url", subscription.Url);
        cmd.Parameters.AddWithValue("$secret", (object?)subscription.Secret ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdUtc", subscription.CreatedUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> ListByStationAsync(string stationId, CancellationToken ct)
    {
        const string sql = """
            SELECT subscription_id, station_id, url, secret, created_utc
            FROM webhook_subscriptions
            WHERE station_id = $stationId
            ORDER BY created_utc ASC;
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$stationId", stationId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<WebhookSubscription>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new WebhookSubscription
            {
                SubscriptionId = Guid.Parse(reader.GetString(0)),
                StationId = reader.GetString(1),
                Url = reader.GetString(2),
                Secret = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedUtc = DateTimeOffset.Parse(reader.GetString(4)),
            });
        }

        return list;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
