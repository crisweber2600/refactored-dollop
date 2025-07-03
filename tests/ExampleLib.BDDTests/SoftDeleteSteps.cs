using ExampleData;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class SoftDeleteSteps
{
    private readonly IGenericRepository<YourEntity> _repository;
    private readonly YourDbContext _context;
    private YourEntity? _entity;

    public SoftDeleteSteps(IGenericRepository<YourEntity> repository, YourDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [Given("an entity to delete")]
    public async Task GivenAnEntityToDelete()
    {
        _entity = new YourEntity { Name = "Delete", Validated = true };
        await _repository.AddAsync(_entity);
        await _context.SaveChangesAsync();
    }

    [When("the entity is deleted")]
    public async Task WhenTheEntityIsDeleted()
    {
        if (_entity != null)
            await _repository.DeleteAsync(_entity);
    }

    [Then("the entity should be marked unvalidated")]
    public void ThenTheEntityShouldBeMarkedUnvalidated()
    {
        if (_entity == null || _entity.Validated)
            throw new Exception("Entity was not soft deleted");
    }
}
