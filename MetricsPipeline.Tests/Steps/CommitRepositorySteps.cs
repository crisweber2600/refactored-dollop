using MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using FluentAssertions;
using Reqnroll;
using System.Linq;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "CommitRepository")]
public class CommitRepositorySteps
{
    private readonly SummaryDbContext _db;
    private IGenericRepository<SummaryRecord> _repo;
    private SummaryRecord? _record;
    private int _resultId;
    private int _createdId;
    private Exception? _ex;
    private SummaryRecord? _retrieved;

    public CommitRepositorySteps(IGenericRepository<SummaryRecord> repo, SummaryDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    [Given("a summary record with value (.*) for commit repo")]
    public void GivenRecord(double val)
    {
        _record = new SummaryRecord { PipelineName = "test", Value = val, Source = new("https://test"), Timestamp = DateTime.UtcNow };
    }

    [Given("a summary record with value (.*) for commit repo with hard delete")]
    public void GivenRecordWithHardDelete(double val)
    {
        _record = new SummaryRecord { PipelineName = "test", Value = val, Source = new("https://test"), Timestamp = DateTime.UtcNow };
        _repo = new EfGenericRepository<SummaryRecord>(_db, true);
    }

    [Given("the record is created via repository")]
    [When("the record is created via repository")]
    public async Task WhenRecordCreated()
    {
        _createdId = await _repo.CreateAsync(_record!);
    }

    [When("the record value is updated to (.*) via repository")]
    public async Task WhenRecordUpdated(double val)
    {
        _record!.Value = val;
        _resultId = await _repo.UpdateAsync(_record);
    }

    [When("the record is softly deleted via repository")]
    public async Task WhenSoftDelete()
    {
        _resultId = await _repo.DeleteAsync(_record!, false);
    }

    [When("a hard delete is attempted via repository")]
    public async Task WhenHardDelete()
    {
        try
        {
            _resultId = await _repo.DeleteAsync(_record!, true);
        }
        catch(Exception ex)
        {
            _ex = ex;
        }
    }

    [Then("the created id should be greater than 0")]
    public void ThenCreatedId()
    {
        _createdId.Should().BeGreaterThan(0);
    }

    [Then("the updated id should equal the created id")]
    public void ThenUpdateId()
    {
        _resultId.Should().Be(_createdId);
    }

    [Then("the record should be marked deleted")]
    public void ThenMarkedDeleted()
    {
        var entity = _db.Summaries.First(e => e.Id == _createdId);
        entity.IsDeleted.Should().BeTrue();
    }

    [Then("a HardDeleteNotPermittedException should be thrown")]
    public void ThenHardDeleteException()
    {
        _ex.Should().BeOfType<HardDeleteNotPermittedException>();
    }

    [Then("the delete result id should equal the created id")]
    public void ThenHardDeleteSuccessId()
    {
        _resultId.Should().Be(_createdId);
    }

    [Then("retrieving the deleted record should return nothing")]
    public async Task ThenRetrievingDeleted()
    {
        _retrieved = await _repo.GetByIdAsync(_createdId);
        _retrieved.Should().BeNull();
    }
}
