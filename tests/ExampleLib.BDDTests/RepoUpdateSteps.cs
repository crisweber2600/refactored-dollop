using ExampleData;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class RepoUpdateSteps
{
    private readonly IGenericRepository<YourEntity> _repository;
    private readonly YourDbContext _context;
    private YourEntity? _entity;

    public RepoUpdateSteps(IGenericRepository<YourEntity> repository, YourDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [Given("an entity to update")]
    public async Task GivenEntityToUpdate()
    {
        _entity = new YourEntity { Name = "Old", Validated = true };
        await _repository.AddAsync(_entity);
        await _context.SaveChangesAsync();
    }

    [When("the entity name is changed")]
    public void WhenEntityNameChanged()
    {
        if (_entity != null)
            _entity.Name = "New";
    }

    [When("the entity is updated")]
    public async Task WhenEntityUpdated()
    {
        if (_entity != null)
            await _repository.UpdateAsync(_entity);
    }

    [When("changes are committed")]
    public Task CommitChanges() => _context.SaveChangesAsync();

    [Then("the entity should reflect the new name")]
    public async Task ThenEntityShouldReflectNewName()
    {
        var loaded = await _repository.GetByIdAsync(_entity!.Id);
        if (loaded?.Name != "New")
            throw new Exception("Entity was not updated");
    }
}
