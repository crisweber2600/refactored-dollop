using System.Threading;
using System.Threading.Tasks;

namespace refactored_dollop.Repositories;

public interface IUnitOfWork
{
    IWorkflowRepository Workflows { get; }
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
