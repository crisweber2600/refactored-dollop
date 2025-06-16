using FluentAssertions;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "SoftDeleteFilter")]
public class SoftDeleteFilterSteps
{
    private readonly SummaryDbContext _db;
    private readonly IGenericRepository<SummaryRecord> _repo;
    private int _count;
    private int _simpleCount;
    private SummaryRecord? _found;
    private SimpleRecord? _simple;

    public SoftDeleteFilterSteps(SummaryDbContext db, IGenericRepository<SummaryRecord> repo)
    {
        _db = db;
        _repo = repo;
    }

    [Given("a soft deleted summary record exists")]
    public void GivenSoftDeletedRecord()
    {
        var rec = new SummaryRecord
        {
            PipelineName = "demo",
            Source = new("https://example.com"),
            Timestamp = DateTime.UtcNow,
            Value = 1.0,
            IsDeleted = true
        };
        _db.Summaries.Add(rec);
        _db.SaveChanges();
    }

    [Given("a simple record exists")]
    public void GivenSimpleRecord()
    {
        _simple = new SimpleRecord { Info = "simple" };
        _db.Set<SimpleRecord>().Add(_simple);
        _db.SaveChanges();
    }

    [Given("the repository ignores the soft delete filter")]
    public void GivenIgnoreFilter()
    {
        if (_repo is EfGenericRepository<SummaryRecord> ef)
            ef.IgnoreSoftDeleteFilter = true;
    }

    [When("counting all summaries")]
    public async Task WhenCountingAll()
    {
        _count = await _repo.GetCountAsync();
    }

    [When("counting simple records")]
    public void WhenCountingSimple()
    {
        _simpleCount = _db.Set<SimpleRecord>().Count();
    }

    [When("finding deleted summary by id")]
    public async Task WhenFindingDeleted()
    {
        var id = _db.Summaries.IgnoreQueryFilters().First().Id;
        _found = await _repo.GetByIdAsync(id);
    }

    [Then("the summary count should be (\\d+)")]
    public void ThenSummaryCount(int expected)
    {
        _count.Should().Be(expected);
    }

    [Then("the simple record count should be (\\d+)")]
    public void ThenSimpleCount(int expected)
    {
        _simpleCount.Should().Be(expected);
    }

    [Then("the result should be null")]
    public void ThenResultNull()
    {
        _found.Should().BeNull();
    }
}
