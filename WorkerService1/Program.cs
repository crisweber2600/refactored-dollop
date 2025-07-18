using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MongoDB.Driver;
using WorkerService1;
using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;

var builder = Host.CreateApplicationBuilder(args);

// STEP 1: Configure ExampleLib with the latest fluent API
builder.Services.ConfigureExampleLib(config =>
{
    config.WithApplicationName("WorkerService1")
          .UseEntityFramework()
          .WithDefaultThresholds(ThresholdType.RawDifference, 5.0m)
          .AddSummarisationPlan<SampleEntity>(
              entity => (decimal)entity.Value,
              ThresholdType.RawDifference,
              0.0m)
          .AddValidationPlan<SampleEntity>(
              threshold: 5.0,
              strategy: ValidationStrategy.Average)
          .AddValidationRules<SampleEntity>(
              entity => !string.IsNullOrWhiteSpace(entity.Name),
              entity => entity.Value >= 0,
              entity => entity.Validated)
          .AddSummarisationPlan<OtherEntity>(
              entity => entity.Amount,
              ThresholdType.RawDifference,
              1.0m)
          .AddValidationPlan<OtherEntity>(
              threshold: 1.0,
              strategy: ValidationStrategy.Average)
          .AddValidationRules<OtherEntity>(
              entity => !string.IsNullOrWhiteSpace(entity.Code),
              entity => entity.Amount > 0,
              entity => entity.IsActive,
              entity => entity.Validated)
          .WithConfigurableEntityIds(provider =>
          {
              provider.RegisterSelector<SampleEntity>(entity => entity.Name);
              provider.RegisterSelector<OtherEntity>(entity => entity.Code);
          });
});

// STEP 2: Configure database contexts
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<SampleDbContext>("Sql");
builder.AddSqlServerDbContext<TheNannyDbContext>("Sql");
builder.AddMongoDBClient("mongodb");

// STEP 3: Register EF repositories with ValidationRunner integration
builder.Services.AddScoped<WorkerService1.Repositories.IRepository<SampleEntity>>(sp =>
    new WorkerService1.Repositories.EfRepository<SampleEntity>(
        sp.GetRequiredService<SampleDbContext>(),
        sp.GetRequiredService<IValidationRunner>()));

builder.Services.AddScoped<WorkerService1.Repositories.IRepository<OtherEntity>>(sp =>
    new WorkerService1.Repositories.EfRepository<OtherEntity>(
        sp.GetRequiredService<SampleDbContext>(),
        sp.GetRequiredService<IValidationRunner>()));

// STEP 4: Register MongoDB repositories as additional implementations
builder.Services.AddScoped<WorkerService1.Repositories.MongoRepository<SampleEntity>>(sp =>
    new WorkerService1.Repositories.MongoRepository<SampleEntity>(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<IValidationRunner>(),
        "WorkerService", "SampleEntities"));

builder.Services.AddScoped<WorkerService1.Repositories.MongoRepository<OtherEntity>>(sp =>
    new WorkerService1.Repositories.MongoRepository<OtherEntity>(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<IValidationRunner>(),
        "WorkerService", "OtherEntities"));

// STEP 5: Register application services and workers
builder.Services.AddHostedService<MigrationWorker>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<MongoWorker>();
builder.Services.AddHostedService<OtherWorker>();
builder.Services.AddHostedService<MongoOtherWorker>();
builder.Services.AddHostedService<ValidationDemoWorker>();

builder.Services.AddScoped<ISampleEntityService, SampleEntityService>();
builder.Services.AddScoped<IOtherEntityService, OtherEntityService>();

var host = builder.Build();
host.Run();

/*
 * EVOLUTION: Configuration Approaches in ExampleLib
 * 
 * 1. LEGACY (Pre-fluent):
 *    - Required 25+ lines of manual service registration
 *    - Separate calls for each store, validator, and provider
 *    - Complex setup with manual dependency management
 *    - Error-prone due to missing service registrations
 * 
 * 2. SIMPLIFIED (AddExampleLibValidation + ConfigureValidation):
 *    - AddExampleLibValidation() for 90% of infrastructure
 *    - ConfigureValidation<T>() for entity-specific rules
 *    - Reduced to 3 key method calls
 *    - Automatic dependency resolution
 * 
 * 3. CURRENT (ConfigureExampleLib Fluent API):
 *    - Single fluent configuration call
 *    - All validation setup in one place
 *    - Type-safe configuration with IntelliSense
 *    - Consistent patterns across all entities
 *    - Built-in validation of configuration
 *    - Automatic service registration and dependency injection
 * 
 * Benefits of Current Approach:
 * ? Single configuration point
 * ? Type-safe fluent API
 * ? Automatic service registration
 * ? Comprehensive validation setup
 * ? Better maintainability
 * ? Reduced boilerplate code
 * ? Consistent entity configuration patterns
 * ? Built-in configuration validation
 */
