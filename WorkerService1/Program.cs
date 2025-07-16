using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MongoDB.Driver;
using WorkerService1;
using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;

var builder = Host.CreateApplicationBuilder(args);

// Create and register a single InMemorySummarisationPlanStore instance
var summarisationPlanStore = new InMemorySummarisationPlanStore();
builder.Services.AddSingleton<ISummarisationPlanStore>(summarisationPlanStore);

// Register SummarisationPlans for SampleEntity and OtherEntity directly
summarisationPlanStore.AddPlan(new SummarisationPlan<SampleEntity>(
    entity => (decimal)entity.Value,
    ThresholdType.RawDifference,
    0.0m
));
summarisationPlanStore.AddPlan(new SummarisationPlan<OtherEntity>(
    entity => entity.Amount,
    ThresholdType.RawDifference,
    1.0m
));


// Register manual validation rules
builder.Services.AddValidatorService();
builder.Services.AddValidatorRule<SampleEntity>(entity => !string.IsNullOrWhiteSpace(entity.Name));
builder.Services.AddValidatorRule<SampleEntity>(entity => entity.Value >= 0);
builder.Services.AddValidatorRule<SampleEntity>(entity => entity.Validated);
builder.Services.AddValidatorRule<OtherEntity>(entity => !string.IsNullOrWhiteSpace(entity.Code));
builder.Services.AddValidatorRule<OtherEntity>(entity => entity.Amount > 0);
builder.Services.AddValidatorRule<OtherEntity>(entity => entity.IsActive);
builder.Services.AddValidatorRule<OtherEntity>(entity => entity.Validated);

// Register IValidationService implementation for DI
builder.Services.AddScoped<IValidationService, ValidationService>();
// Register ISummarisationValidator<T> open generic for DI
builder.Services.AddSingleton(typeof(ISummarisationValidator<>), typeof(SummarisationValidator<>));

// Register ValidationRunner
builder.Services.AddValidationRunner();

builder.AddServiceDefaults();

// SQL Server Aspire connection
builder.AddSqlServerDbContext<SampleDbContext>("Sql");
builder.AddSqlServerDbContext<TheNannyDbContext>("Sql");

// Register EfSaveAuditRepository for audit persistence (EF)
// builder.Services.AddScoped<ISaveAuditRepository, ExampleLib.Infrastructure.EfSaveAuditRepository>();

// Register generic EF repositories for DI using SampleDbContext
builder.Services.AddScoped<IRepository<SampleEntity>>(sp =>
    new EfRepository<SampleEntity>(
        sp.GetRequiredService<SampleDbContext>(),
        sp.GetRequiredService<IValidationRunner>()));
builder.Services.AddScoped<IRepository<OtherEntity>>(sp =>
    new EfRepository<OtherEntity>(
        sp.GetRequiredService<SampleDbContext>(),
        sp.GetRequiredService<IValidationRunner>()));

// MongoDB Aspire connection
builder.AddMongoDBClient("mongodb");
// Register generic Mongo repositories for DI
builder.Services.AddScoped<IRepository<SampleEntity>>(sp =>
    new MongoRepository<SampleEntity>(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<IValidationRunner>(),
        "SampleEntities", "SampleEntities"));
builder.Services.AddScoped<IRepository<OtherEntity>>(sp =>
    new MongoRepository<OtherEntity>(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<IValidationRunner>(),
        "OtherEntities", "OtherEntities"));

// Register ExampleLib validation pipeline for SampleEntity (Mongo)
// Removed AddSaveValidation to prevent overriding the singleton ISummarisationPlanStore
// builder.Services.AddSaveValidation<WorkerService1.Models.SampleEntity>(
//     entity => (decimal)entity.Value, // metric selector for validation
//     ThresholdType.RawDifference,
//     0.0m
// );
// Register ExampleLib validation pipeline for OtherEntity (Mongo)
// builder.Services.AddSaveValidation<WorkerService1.Models.OtherEntity>(
//     entity => entity.Amount,
//     ThresholdType.RawDifference,
//     1.0m
// );
// Register MongoSaveAuditRepository for audit persistence (Mongo)
builder.Services.AddScoped<ISaveAuditRepository, MongoSaveAuditRepository>();

// Register a static application name provider for validation/audit services
builder.Services.AddSingleton<IApplicationNameProvider>(
    new StaticApplicationNameProvider(builder.Environment.ApplicationName));

// Register migration worker FIRST to ensure migrations run before other workers
builder.Services.AddHostedService<MigrationWorker>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<MongoWorker>();
builder.Services.AddHostedService<OtherWorker>();
builder.Services.AddHostedService<MongoOtherWorker>();

// Register ISampleEntityService and IOtherEntityService
builder.Services.AddScoped<ISampleEntityService, SampleEntityService>();
builder.Services.AddScoped<IOtherEntityService, OtherEntityService>();

var host = builder.Build();
host.Run();
