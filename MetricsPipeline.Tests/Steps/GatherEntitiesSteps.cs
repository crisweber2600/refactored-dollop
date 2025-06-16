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

    [Then("the ExtraRecord entity should be mapped")]
    public void ThenExtraRecordMapped()
    {
        _model!.FindEntityType(typeof(ExtraRecord)).Should().NotBeNull();
    }

    [Then("the SummaryRecord PipelineName max length should be 50")]
    public void ThenSummaryRecordMaxLength()
    {
        var entity = _model!.FindEntityType(typeof(SummaryRecord))!;
        var prop = entity.FindProperty(nameof(SummaryRecord.PipelineName))!;
        prop.GetMaxLength().Should().Be(50);
    }

    [Then("Set<ExtraRecord> should be available")]
    public void ThenExtraRecordSet()
    {
        _db.Set<ExtraRecord>().Should().NotBeNull();
    }

    [Then("Set<SimpleRecord> should be available")]
    public void ThenSimpleRecordSet()
    {
        _db.Set<SimpleRecord>().Should().NotBeNull();
    }
}
