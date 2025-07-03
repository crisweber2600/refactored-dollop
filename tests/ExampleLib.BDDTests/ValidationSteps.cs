using ExampleData;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class ValidationSteps
{
    private readonly IGenericRepository<YourEntity> _repository;
    private readonly YourDbContext _context;
    private List<YourEntity> _results = new();

    public ValidationSteps(IGenericRepository<YourEntity> repository, YourDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    [Given("a context with an unvalidated entity")]
    public async Task GivenContextWithAnUnvalidatedEntity()
    {
        await _repository.AddAsync(new YourEntity { Name = "Invalid", Validated = false });
        await _context.SaveChangesAsync();
    }

    [When("querying for all entities")]
    public async Task WhenQueryingForAllEntities()
    {
        _results = await _repository.GetAllAsync();
    }

    [Then("the result list should be empty")]
    public void ThenTheResultListShouldBeEmpty()
    {
        if (_results.Count != 0)
            throw new Exception($"Expected 0 but got {_results.Count}");
    }
}
