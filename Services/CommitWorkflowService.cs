using refactored_dollop.Repositories;
using refactored_dollop.Workflow;

namespace refactored_dollop.Services;

public class CommitWorkflowService
{
    private readonly IUnitOfWork _uow;

    public CommitWorkflowService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task CommitAsync(Guid id, CancellationToken ct = default)
    {
        var run = await _uow.Workflows.GetAsync(id, ct) ?? throw new InvalidOperationException("Run not found");
        run.Status = WorkflowStatus.Committed;
        run.UpdatedAt = DateTime.UtcNow;
        await _uow.Workflows.UpdateAsync(run, ct);
        await _uow.Workflows.AddLogAsync(new WorkflowRunLog { WorkflowRunId = run.Id, Message = "Committed" }, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
