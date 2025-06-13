using Microsoft.EntityFrameworkCore;
using refactored_dollop.Data;
using refactored_dollop.Workflow;

namespace refactored_dollop.Repositories;

public class WorkflowRepository : IWorkflowRepository
{ 
    private readonly WorkflowContext _context;

    public WorkflowRepository(WorkflowContext context)
    {
        _context = context;
    }

    public async Task<WorkflowRun?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.WorkflowRuns.Include(r => r.Logs).FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddAsync(WorkflowRun run, CancellationToken ct = default)
    {
        await _context.WorkflowRuns.AddAsync(run, ct);
    }

    public Task UpdateAsync(WorkflowRun run, CancellationToken ct = default)
    {
        _context.WorkflowRuns.Update(run);
        return Task.CompletedTask;
    }

    public async Task AddLogAsync(WorkflowRunLog log, CancellationToken ct = default)
    {
        await _context.WorkflowRunLogs.AddAsync(log, ct);
    }
}
