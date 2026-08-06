using ITL.AutomationGateway.Api.Domain;

namespace ITL.AutomationGateway.Api.Abstractions;

public interface IJobOrchestrator
{
    Task<JobSubmitResult> SubmitAsync(JobSubmitModel submit, CancellationToken ct);

    Task<bool> CancelAsync(Guid jobId, CancellationToken ct);
}
