using Microsoft.EntityFrameworkCore;
using refactored_dollop.Data;

namespace refactored_dollop.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly WorkflowContext _context;
    private readonly DbSet<TEntity> _set;

    public Repository(WorkflowContext context)
    {
        _context = context;
        _set = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetAsync(object id, CancellationToken ct = default)
    {
        return await _set.FindAsync(new object?[] { id }, ct);
    }

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        return Task.CompletedTask;
    }
}
