using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Plan2RepositoryUoW.Domain.Entities;

namespace Plan2RepositoryUoW.Application.Services;

public interface IEntityCrudService
{
    Task<Guid> CreateAsync(string name, double score);
    Task UpdateAsync(Guid id, double newScore);
    Task DeleteAsync(Guid id, bool hardDelete);
    Task<YourEntity?> GetAsync(Guid id, bool includeDeleted = false);
}

public sealed class EntityCrudService : IEntityCrudService
{
    private readonly IUnitOfWork _uow;
    private readonly IGenericRepository<YourEntity> _repo;

    public EntityCrudService(IUnitOfWork uow, IGenericRepository<YourEntity> repo)
    {
        _uow = uow;
        _repo = repo;
    }

    public async Task<Guid> CreateAsync(string name, double score)
    {
        var entity = new YourEntity { Name = name, Score = score };
        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync<YourEntity>(e => e.Score, ValidationStrategy.Average, 50);
        return entity.Id;
    }

    public async Task UpdateAsync(Guid id, double newScore)
    {
        var entity = await _repo.GetByIdAsync(id, includeDeleted: true);
        if (entity == null) return;
        entity.Score = newScore;
        await _uow.SaveChangesAsync<YourEntity>(e => e.Score, ValidationStrategy.Average, 50);
    }

    public async Task DeleteAsync(Guid id, bool hardDelete)
    {
        var entity = await _repo.GetByIdAsync(id, includeDeleted: true);
        if (entity == null) return;
        await _repo.DeleteAsync(entity, hardDelete);
    }

    public Task<YourEntity?> GetAsync(Guid id, bool includeDeleted = false)
        => _repo.GetByIdAsync(id, includeDeleted);
}
