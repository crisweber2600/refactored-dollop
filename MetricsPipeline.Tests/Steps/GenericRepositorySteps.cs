using System.Linq.Expressions;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using FluentAssertions;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "GenericRepository")]
public class GenericRepositorySteps
{
    private readonly IGenericRepository<SummaryRecord> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ScenarioContext _ctx;
    private SummaryRecord? _record;
    private IReadOnlyList<SummaryRecord>? _results;

    public GenericRepositorySteps(IGenericRepository<SummaryRecord> repo, IUnitOfWork uow, ScenarioContext ctx)
    {
        _repo = repo;
        _uow = uow;
        _ctx = ctx;
    }

    [Given("a summary record with value (.*) for generic repo")]
    public void GivenRecord(double value)
    {
        _record = new SummaryRecord { Value = value, Source = new("https://test"), Timestamp = DateTime.UtcNow };
    }

    [When("the generic record is added and saved")]
    public async Task WhenAddedAndSaved()
    {
        await _repo.AddAsync(_record!);
        await _uow.SaveChangesAsync();
    }

    [When("the generic record is deleted and saved")]
    public async Task WhenDeletedAndSaved()
    {
        _repo.Delete(_record!);
        await _uow.SaveChangesAsync();
    }

    [Then("the generic repository should return (\\d+) active record(?:s)?")]
    public async Task ThenActiveCount(int expected)
    {
        var count = await _repo.GetCountAsync();
        count.Should().Be(expected);
    }

    [Given("two summary records with values (.*) and (.*)")]
    public void GivenTwoRecords(double v1, double v2)
    {
        _ctx["records"] = new List<SummaryRecord>
        {
            new SummaryRecord { Value = v1, Source = new("https://test"), Timestamp = DateTime.UtcNow },
            new SummaryRecord { Value = v2, Source = new("https://test"), Timestamp = DateTime.UtcNow }
        };
    }

    [When("the records are added and saved to generic repo")]
    public async Task WhenRecordsAddedAndSaved()
    {
        var records = (List<SummaryRecord>)_ctx["records"];
        await _repo.AddRangeAsync(records);
        await _uow.SaveChangesAsync();
    }

    [When("searching for records with value greater than (.*)")]
    public async Task WhenSearching(double value)
    {
        var spec = new ValueGreaterThanSpec(value);
        _results = await _repo.SearchAsync(spec);
    }

    [Then("the search result count should be (\\d+)")]
    public void ThenSearchCount(int count)
    {
        _results!.Count.Should().Be(count);
    }

    private record ValueGreaterThanSpec(double Threshold) : ISpecification<SummaryRecord>
    {
        public Expression<Func<SummaryRecord, bool>> Criteria => r => r.Value > Threshold;
    }
}
