using ExampleData;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class RepoSteps
{
    private readonly IGenericRepository<YourEntity> _repository;
    private readonly YourDbContext _context;

    public RepoSteps(IGenericRepository<YourEntity> repository, YourDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [Given("a clean db context")]
    public void GivenACleanDbContext()
    {
        foreach (var e in _context.YourEntities.ToList())
        {
            _context.Remove(e);
        }
        _context.SaveChanges();
    }

    [When("a new entity is added")]
    public async Task WhenANewEntityIsAdded()
    {
        var entity = new YourEntity { Name = "Test", Validated = true };
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    [Then("the repository count should be (\\d+)")]
    public async Task ThenTheRepositoryCountShouldBe(int count)
    {
        var actual = await _repository.CountAsync();
        if (actual != count)
        {
            throw new Exception($"Expected {count} but was {actual}");
        }
    }
}
