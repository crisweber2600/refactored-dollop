using refactored_dollop.Repositories;
using refactored_dollop.Workflow;

namespace refactored_dollop.Services;

public class StartWorkflowService
{
    private readonly IUnitOfWork _uow;

    public StartWorkflowService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<WorkflowRun> StartAsync(string? message = null, CancellationToken ct = default)
    {
        var run = new WorkflowRun { Status = WorkflowStatus.Started };
        await _uow.Workflows.AddAsync(run, ct);
        if (!string.IsNullOrEmpty(message))
        {
            await _uow.Workflows.AddLogAsync(new WorkflowRunLog { WorkflowRunId = run.Id, Message = message }, ct);
        }
        await _uow.SaveChangesAsync(ct);
        return run;
    }
}
