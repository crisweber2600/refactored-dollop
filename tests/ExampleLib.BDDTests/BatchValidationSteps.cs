using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Reqnroll;

namespace ExampleLib.BDDTests;

[Binding]
public class BatchValidationSteps
{
    private readonly IBatchValidationService _service;
    private readonly InMemorySaveAuditRepository _repo;
    private bool _result;

    public BatchValidationSteps()
    {
        _repo = new InMemorySaveAuditRepository();
        _service = new BatchValidationService(_repo);
    }

    [Given("no previous batch audit")]
    public void GivenNoPreviousBatchAudit()
    {
        // nothing needed
    }

    [Given("a previous batch size of (\\d+)")]
    public void GivenPreviousBatch(int size)
    {
        _repo.AddBatchAudit(new SaveAudit
        {
            EntityType = typeof(object).Name,
            BatchSize = size,
            MetricValue = 0,
            Validated = true,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    [When("validating a batch of (\\d+) entities")]
    public void WhenValidatingBatch(int size)
    {
        _result = _service.ValidateAndAudit<object>(size);
    }

    [Then("the batch validation result should be (true|false)")]
    public void ThenResultShouldBe(bool expected)
    {
        if (_result != expected)
            throw new Exception($"Expected {expected} but was {_result}");
    }
}
