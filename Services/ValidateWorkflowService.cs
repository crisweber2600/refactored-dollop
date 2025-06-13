using refactored_dollop.Repositories;
using refactored_dollop.Workflow;

namespace refactored_dollop.Services;

public class ValidateWorkflowService
{
    private readonly IUnitOfWork _uow;

    public ValidateWorkflowService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task ValidateAsync(Guid id, CancellationToken ct = default)
    {
        var run = await _uow.Workflows.GetAsync(id, ct) ?? throw new InvalidOperationException("Run not found");
        run.Status = WorkflowStatus.Validated;
        run.UpdatedAt = DateTime.UtcNow;
        await _uow.Workflows.UpdateAsync(run, ct);
        await _uow.Workflows.AddLogAsync(new WorkflowRunLog { WorkflowRunId = run.Id, Message = "Validated" }, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
