using ExampleData;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class ValidationRuleSetSteps
{
    private readonly IUnitOfWork _uow;
    private readonly IGenericRepository<YourEntity> _repository;
    private YourEntity? _entity;

    public ValidationRuleSetSteps(IUnitOfWork uow, IGenericRepository<YourEntity> repository)
    {
        _uow = uow;
        _repository = repository;
    }

    [When("saving a new entity with rule set count (\\d+) and sum (\\d+)")]
    public async Task WhenSavingWithRuleSet(int countThreshold, int sumThreshold)
    {
        _entity = new YourEntity { Name = "Rules" };
        await _repository.AddAsync(_entity);
        var ruleSet = new ValidationRuleSet<YourEntity>(e => e.Id,
            new ValidationRule(ValidationStrategy.Count, countThreshold),
            new ValidationRule(ValidationStrategy.Sum, sumThreshold));
        await ((UnitOfWork<YourDbContext>)_uow).SaveChangesAsync(ruleSet);
    }

    [Then("the rule set entity should be validated")]
    public void ThenEntityShouldBeValidated()
    {
        if (_entity == null || !_entity.Validated)
            throw new Exception("Entity was not validated");
    }
}
