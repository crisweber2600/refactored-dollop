using FluentAssertions;
using MetricsPipeline.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Reqnroll;

namespace MetricsPipeline.Tests.Steps;

[Binding]
[Scope(Feature = "GatherEntities")]
public class GatherEntitiesSteps
{
    private readonly SummaryDbContext _db;
    private IModel? _model;

    public GatherEntitiesSteps(SummaryDbContext db)
    {
        _db = db;
    }

    [When("the database model is built")]
    public void WhenModelBuilt()
    {
        _model = _db.Model;
    }

    [Then("the SummaryRecord entity should be mapped")]
    public void ThenSummaryRecordMapped()
    {
        _model!.FindEntityType(typeof(SummaryRecord)).Should().NotBeNull();
    }
}
