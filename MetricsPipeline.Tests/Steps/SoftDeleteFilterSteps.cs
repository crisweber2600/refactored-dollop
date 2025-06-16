using FluentAssertions;
using MetricsPipeline.Core;
using MetricsPipeline.Infrastructure;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "SoftDeleteFilter")]
public class SoftDeleteFilterSteps
{
    private readonly SummaryDbContext _db;
    private readonly IGenericRepository<SummaryRecord> _repo;
    private int _count;

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

    [Then("the summary count should be (\\d+)")]
    public void ThenSummaryCount(int expected)
    {
        _count.Should().Be(expected);
    }
}
