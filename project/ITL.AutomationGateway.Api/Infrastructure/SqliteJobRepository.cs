using ITL.AutomationGateway.Api.Abstractions;
using ITL.AutomationGateway.Api.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ITL.AutomationGateway.Api.Infrastructure;

public sealed class SqliteJobRepository : BackgroundService, IJobRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteJobRepository> _logger;

    public SqliteJobRepository(IOptions<StorageOptions> storageOptions, ILogger<SqliteJobRepository> logger)
    {
        _logger = logger;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);
        _logger.LogInformation("SQLite repository initialized.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    public async Task<GatewayJob?> GetByIdAsync(Guid jobId, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM jobs WHERE job_id = $jobId;
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$jobId", jobId.ToString());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task<GatewayJob?> GetByIdempotencyAsync(string stationId, string idempotencyKey, CancellationToken ct)
    {
        const string sql = """
            SELECT * FROM jobs WHERE station_id = $stationId AND idempotency_key = $idempotencyKey;
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$stationId", stationId);
        cmd.Parameters.AddWithValue("$idempotencyKey", idempotencyKey);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return Map(reader);
    }

    public async Task CreateQueuedAsync(GatewayJob job, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO jobs
            (
                job_id, station_id, operation, sn, port, parameters_json,
                idempotency_key, status, result_json, error_code, error_message,
                created_utc, updated_utc, started_utc, completed_utc
            )
            VALUES
            (
                $jobId, $stationId, $operation, $sn, $port, $parametersJson,
                $idempotencyKey, $status, $resultJson, $errorCode, $errorMessage,
                $createdUtc, $updatedUtc, $startedUtc, $completedUtc
            );
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        Bind(cmd, job);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateStateAsync(
        Guid jobId,
        JobStatus state,
        string? resultJson,
        string? errorCode,
        string? errorMessage,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var markStarted = state is JobStatus.Dispatched or JobStatus.Running or JobStatus.WaitingAck;
        var markCompleted = state is JobStatus.Succeeded or JobStatus.Failed or JobStatus.Timeout or JobStatus.Canceled;

        const string sql = """
            UPDATE jobs
            SET
                status = $status,
                result_json = $resultJson,
                error_code = $errorCode,
                error_message = $errorMessage,
                updated_utc = $updatedUtc,
                started_utc = COALESCE(started_utc, $startedUtc),
                completed_utc = CASE WHEN $completedUtc IS NULL THEN completed_utc ELSE $completedUtc END
            WHERE job_id = $jobId;
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$jobId", jobId.ToString());
        cmd.Parameters.AddWithValue("$status", state.ToString());
        cmd.Parameters.AddWithValue("$resultJson", (object?)resultJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$errorCode", (object?)errorCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$errorMessage", (object?)errorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$updatedUtc", now.ToString("O"));
        cmd.Parameters.AddWithValue("$startedUtc", markStarted ? now.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$completedUtc", markCompleted ? now.ToString("O") : DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> CancelQueuedAsync(Guid jobId, CancellationToken ct)
    {
        const string sql = """
            UPDATE jobs
            SET
                status = $status,
                updated_utc = $updatedUtc,
                completed_utc = $completedUtc,
                error_code = $errorCode,
                error_message = $errorMessage
            WHERE job_id = $jobId
              AND status = $queued;
            """;

        var now = DateTimeOffset.UtcNow;
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$status", JobStatus.Canceled.ToString());
        cmd.Parameters.AddWithValue("$updatedUtc", now.ToString("O"));
        cmd.Parameters.AddWithValue("$completedUtc", now.ToString("O"));
        cmd.Parameters.AddWithValue("$errorCode", ErrorCodes.Canceled);
        cmd.Parameters.AddWithValue("$errorMessage", "Canceled by caller.");
        cmd.Parameters.AddWithValue("$jobId", jobId.ToString());
        cmd.Parameters.AddWithValue("$queued", JobStatus.Queued.ToString());

        var changed = await cmd.ExecuteNonQueryAsync(ct);
        return changed > 0;
    }

    public async Task<int> CountQueuedByStationAsync(string stationId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM jobs
            WHERE station_id = $stationId AND status = $status;
            """;
        return await ExecuteCountAsync(sql, stationId, JobStatus.Queued.ToString(), ct);
    }

    public async Task<int> CountRunningByStationAsync(string stationId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM jobs
            WHERE station_id = $stationId
              AND status IN ($dispatched, $running, $waitingAck);
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$stationId", stationId);
        cmd.Parameters.AddWithValue("$dispatched", JobStatus.Dispatched.ToString());
        cmd.Parameters.AddWithValue("$running", JobStatus.Running.ToString());
        cmd.Parameters.AddWithValue("$waitingAck", JobStatus.WaitingAck.ToString());
        var val = await cmd.ExecuteScalarAsync(ct);
        return val is null ? 0 : Convert.ToInt32(val);
    }

    public async Task<IReadOnlyList<Guid>> ListQueuedJobIdsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT job_id
            FROM jobs
            WHERE status = $status
            ORDER BY created_utc ASC;
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$status", JobStatus.Queued.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Guid>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(Guid.Parse(reader.GetString(0)));
        }
        return list;
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS jobs
            (
                job_id TEXT NOT NULL PRIMARY KEY,
                station_id TEXT NOT NULL,
                operation TEXT NOT NULL,
                sn TEXT NULL,
                port TEXT NULL,
                parameters_json TEXT NULL,
                idempotency_key TEXT NOT NULL,
                status TEXT NOT NULL,
                result_json TEXT NULL,
                error_code TEXT NULL,
                error_message TEXT NULL,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL,
                started_utc TEXT NULL,
                completed_utc TEXT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_jobs_station_idempotency
            ON jobs(station_id, idempotency_key);

            CREATE INDEX IF NOT EXISTS ix_jobs_status_created
            ON jobs(status, created_utc);

            CREATE TABLE IF NOT EXISTS webhook_subscriptions
            (
                subscription_id TEXT NOT NULL PRIMARY KEY,
                station_id TEXT NOT NULL,
                url TEXT NOT NULL,
                secret TEXT NULL,
                created_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_webhook_station_created
            ON webhook_subscriptions(station_id, created_utc);
            """;

        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<int> ExecuteCountAsync(string sql, string stationId, string status, CancellationToken ct)
    {
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("$stationId", stationId);
        cmd.Parameters.AddWithValue("$status", status);
        var val = await cmd.ExecuteScalarAsync(ct);
        return val is null ? 0 : Convert.ToInt32(val);
    }

    private static GatewayJob Map(SqliteDataReader reader)
    {
        return new GatewayJob
        {
            JobId = Guid.Parse(reader.GetString(reader.GetOrdinal("job_id"))),
            StationId = reader.GetString(reader.GetOrdinal("station_id")),
            Operation = reader.GetString(reader.GetOrdinal("operation")),
            Sn = ReadNullable(reader, "sn"),
            Port = ReadNullable(reader, "port"),
            ParametersJson = ReadNullable(reader, "parameters_json"),
            IdempotencyKey = reader.GetString(reader.GetOrdinal("idempotency_key")),
            Status = Enum.Parse<JobStatus>(reader.GetString(reader.GetOrdinal("status"))),
            ResultJson = ReadNullable(reader, "result_json"),
            ErrorCode = ReadNullable(reader, "error_code"),
            ErrorMessage = ReadNullable(reader, "error_message"),
            CreatedUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_utc"))),
            UpdatedUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_utc"))),
            StartedUtc = ParseNullableDate(ReadNullable(reader, "started_utc")),
            CompletedUtc = ParseNullableDate(ReadNullable(reader, "completed_utc")),
        };
    }

    private static void Bind(SqliteCommand cmd, GatewayJob job)
    {
        cmd.Parameters.AddWithValue("$jobId", job.JobId.ToString());
        cmd.Parameters.AddWithValue("$stationId", job.StationId);
        cmd.Parameters.AddWithValue("$operation", job.Operation);
        cmd.Parameters.AddWithValue("$sn", (object?)job.Sn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$port", (object?)job.Port ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$parametersJson", (object?)job.ParametersJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$idempotencyKey", job.IdempotencyKey);
        cmd.Parameters.AddWithValue("$status", job.Status.ToString());
        cmd.Parameters.AddWithValue("$resultJson", (object?)job.ResultJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$errorCode", (object?)job.ErrorCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$errorMessage", (object?)job.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdUtc", job.CreatedUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedUtc", job.UpdatedUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$startedUtc", job.StartedUtc?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$completedUtc", job.CompletedUtc?.ToString("O") ?? (object)DBNull.Value);
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    private static string? ReadNullable(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTimeOffset? ParseNullableDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTimeOffset.Parse(raw);
    }
}
