using MetricsPipeline.Infrastructure;
using MetricsPipeline.Core;
using MassTransit;
using Reqnroll;
using FluentAssertions;

[Binding]
public class RepositorySteps
{
    private readonly IRepository<SummaryRecord> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IBus _bus;
    private SummaryRecord? _record;
    private SummaryRecord? _result;

    public RepositorySteps(IRepository<SummaryRecord> repo, IUnitOfWork uow, IBus bus)
    {
        _repo = repo;
        _uow = uow;
        _bus = bus;
    }

    [Then("a repository should be provided")]
    public void ThenRepositoryProvided()
    {
        _repo.Should().NotBeNull();
    }

    [Then("a unit of work should be provided")]
    public void ThenUnitOfWorkProvided()
    {
        _uow.Should().NotBeNull();
    }

    [Then("the bus should be provided")]
    public void ThenBusProvided()
    {
        _bus.Should().NotBeNull();
    }

    [Given("a new summary record with value (.*)")]
    public void GivenNewRecord(double value)
    {
        _record = new SummaryRecord { PipelineName = "test", Value = value, Source = new("https://test") , Timestamp = DateTime.UtcNow };
    }

    [When("the record is added and saved")]
    public async Task WhenRecordAddedAndSaved()
    {
        await _repo.AddAsync(_record!);
        await _uow.SaveChangesAsync();
    }

    [When("the record is retrieved")]
    public async Task WhenRecordRetrieved()
    {
        _result = await _repo.GetByIdAsync(_record!.Id);
    }

    [Then("the retrieved value should be (.*)")]
    public void ThenRetrievedValue(double value)
    {
        _result!.Value.Should().Be(value);
    }
}
