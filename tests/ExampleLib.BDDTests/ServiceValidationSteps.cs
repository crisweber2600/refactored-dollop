using ExampleData;
using ExampleLib.Infrastructure;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class ServiceValidationSteps
{
    private readonly ValidationService _service;
    private readonly YourDbContext _context;
    private double _summary;

    public ServiceValidationSteps(ValidationService service, YourDbContext context)
    {
        _service = service;
        _context = context;
    }

    [Then("the validation summary should be (\\d+)")]
    public async Task ThenTheValidationSummaryShouldBe(double expected)
    {
        _summary = await _service.ComputeAsync<YourEntity>(e => e.Id, ValidationStrategy.Count);
        if (_summary != expected)
            throw new Exception($"Expected {expected} but was {_summary}");
    }
}
