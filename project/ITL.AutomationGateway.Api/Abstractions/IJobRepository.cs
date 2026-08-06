using ITL.AutomationGateway.Api.Domain;

namespace ITL.AutomationGateway.Api.Abstractions;

public interface IJobRepository
{
    Task<GatewayJob?> GetByIdAsync(Guid jobId, CancellationToken ct);

    Task<GatewayJob?> GetByIdempotencyAsync(string stationId, string idempotencyKey, CancellationToken ct);

    Task CreateQueuedAsync(GatewayJob job, CancellationToken ct);

    Task UpdateStateAsync(
        Guid jobId,
        JobStatus state,
        string? resultJson,
        string? errorCode,
        string? errorMessage,
        CancellationToken ct);

    Task<bool> CancelQueuedAsync(Guid jobId, CancellationToken ct);

    Task<int> CountQueuedByStationAsync(string stationId, CancellationToken ct);

    Task<int> CountRunningByStationAsync(string stationId, CancellationToken ct);

    Task<IReadOnlyList<Guid>> ListQueuedJobIdsAsync(CancellationToken ct);
}
