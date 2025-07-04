using ExampleData;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class UnitOfWorkSteps
{
    private readonly IUnitOfWork _uow;
    private readonly IGenericRepository<YourEntity> _repository;
    private YourEntity? _entity;

    public UnitOfWorkSteps(IUnitOfWork uow, IGenericRepository<YourEntity> repository)
    {
        _uow = uow;
        _repository = repository;
    }

    [When("saving a new entity with count threshold (\\d+)")]
    public async Task WhenSavingANewEntity(int threshold)
    {
        _entity = new YourEntity { Name = "Validate" };
        await _repository.AddAsync(_entity);
        await _uow.SaveChangesAsync<YourEntity>(e => e.Id, ValidationStrategy.Count, threshold);
    }

    [Then("the entity should be validated")]
    public void ThenTheEntityShouldBeValidated()
    {
        if (_entity == null || !_entity.Validated)
            throw new Exception("Entity was not validated");
    }
}
