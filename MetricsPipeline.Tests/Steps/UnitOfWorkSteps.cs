using MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using Reqnroll;
using FluentAssertions;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "UnitOfWorkCommit")]
public class UnitOfWorkSteps
{
    private readonly IRepository<SummaryRecord> _repo;
    private readonly IUnitOfWork _uow;
    private SummaryRecord? _record;
    private SummaryRecord? _result;

    public UnitOfWorkSteps(IRepository<SummaryRecord> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [Given("a new summary record with value (.*)")]
    public void GivenNewRecord(double value)
    {
        _record = new SummaryRecord { PipelineName = "test", Value = value, Source = new("https://test"), Timestamp = DateTime.UtcNow };
    }

    [When("the record is added without saving")]
    public async Task WhenAddedWithoutSaving()
    {
        await _repo.AddAsync(_record!);
    }

    [When("the record is added and saved")]
    public async Task WhenAddedAndSaved()
    {
        await _repo.AddAsync(_record!);
        await _uow.SaveChangesAsync();
    }

    [When("the changes are committed")]
    public async Task WhenChangesCommitted()
    {
        await _uow.SaveChangesAsync();
    }

    [When("the record is deleted and saved")]
    public async Task WhenDeletedAndSaved()
    {
        _repo.Remove(_record!);
        await _uow.SaveChangesAsync();
    }

    [When("the record is retrieved")]
    public async Task WhenRetrieved()
    {
        _result = await _repo.GetByIdAsync(_record!.Id);
    }

    [Then("no record should be found")]
    public void ThenNoRecordFound()
    {
        _result.Should().BeNull();
    }

    [Then("the retrieved value should be (.*)")]
    public void ThenRetrievedValue(double value)
    {
        _result!.Value.Should().Be(value);
    }
}
