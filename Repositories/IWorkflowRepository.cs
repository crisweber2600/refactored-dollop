using refactored_dollop.Workflow;

namespace refactored_dollop.Repositories;

public interface IWorkflowRepository
{
    Task<WorkflowRun?> GetAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(WorkflowRun run, CancellationToken ct = default);
    Task UpdateAsync(WorkflowRun run, CancellationToken ct = default);
    Task AddLogAsync(WorkflowRunLog log, CancellationToken ct = default);
}
