using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using ExampleData;
using Plan2RepositoryUoW.Domain.Entities;
using YourEntityPlan2 = Plan2RepositoryUoW.Domain.Entities.YourEntity;

namespace Plan2RepositoryUoW.Application.Services;

public interface IEntityCrudService
{
    Task<int> CreateAsync(string name, double score);
    Task UpdateAsync(int id, double newScore);
    Task DeleteAsync(int id, bool hardDelete);
    Task<YourEntityPlan2?> GetAsync(int id, bool includeDeleted = false);
}

public sealed class EntityCrudService : IEntityCrudService
{
    private readonly IUnitOfWork _uow;
    private readonly IGenericRepository<YourEntityPlan2> _repo;

    public EntityCrudService(IUnitOfWork uow, IGenericRepository<YourEntityPlan2> repo)
    {
        _uow = uow;
        _repo = repo;
    }

    public async Task<int> CreateAsync(string name, double score)
    {
        var entity = new YourEntityPlan2 { Name = name, Score = score };
        await _repo.AddAsync(entity);
        await _uow.SaveChangesAsync<YourEntityPlan2>(e => e.Score, ValidationStrategy.Average, 50);
        return entity.Id;
    }

    public async Task UpdateAsync(int id, double newScore)
    {
        var entity = await _repo.GetByIdAsync(id, includeDeleted: true);
        if (entity == null) return;
        entity.Score = newScore;
        await _uow.SaveChangesAsync<YourEntityPlan2>(e => e.Score, ValidationStrategy.Average, 50);
    }

    public async Task DeleteAsync(int id, bool hardDelete)
    {
        var entity = await _repo.GetByIdAsync(id, includeDeleted: true);
        if (entity == null) return;
        await _repo.DeleteAsync(entity, hardDelete);
    }

    public Task<YourEntityPlan2?> GetAsync(int id, bool includeDeleted = false)
        => _repo.GetByIdAsync(id, includeDeleted);
}