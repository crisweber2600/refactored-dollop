using ExampleLib;
using ExampleLib.Domain;
using MetricsPipeline.Core.Domain;

namespace MetricsPipeline.Core.Infrastructure.Workers;

public class ExampleWorker
{
    private readonly ISaveAuditRepository _auditRepository;
    private readonly IValidationPlanProvider _planStore;
    private readonly ISummarisationValidator<ExampleEntity> _validator;
    private readonly ValueProvider _provider;

    public ExampleWorker(ISaveAuditRepository auditRepository,
                         IValidationPlanProvider planStore,
                         ISummarisationValidator<ExampleEntity> validator,
                         ValueProvider provider)
    {
        _auditRepository = auditRepository;
        _planStore = planStore;
        _validator = validator;
        _provider = provider;
    }

    public Task SaveInitialAsync()
    {
        _provider.SetInitial();
        return SaveAsync();
    }

    public Task SaveWithinAsync()
    {
        _provider.SetWithin();
        return SaveAsync();
    }

    public Task SaveOutsideAsync()
    {
        _provider.SetOutside();
        return SaveAsync();
    }

    private Task SaveAsync()
    {
        var entity = new ExampleEntity { Values = _provider.GetValues() };
        var plan = _planStore.GetPlan<ExampleEntity>();
        var last = _auditRepository.GetLastAudit(nameof(ExampleEntity), entity.Id);
        var valid = _validator.Validate(entity, last!, plan);
        var audit = new SaveAudit
        {
            EntityType = nameof(ExampleEntity),
            EntityId = entity.Id,
            MetricValue = plan.MetricSelector(entity),
            Validated = valid,
            Timestamp = DateTimeOffset.UtcNow
        };
        _auditRepository.AddAudit(audit);
        return Task.CompletedTask;
    }
}
