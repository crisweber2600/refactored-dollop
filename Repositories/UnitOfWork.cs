using System.Threading;
using System.Threading.Tasks;
using refactored_dollop.Data;

namespace refactored_dollop.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly WorkflowContext _context;

    public UnitOfWork(WorkflowContext context, IWorkflowRepository workflows)
    {
        _context = context;
        Workflows = workflows;
    }

    public IWorkflowRepository Workflows { get; }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        return new Repository<TEntity>(_context);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
