using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MongoDB.Driver;
using WorkerService1;
using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;

var builder = Host.CreateApplicationBuilder(args);

// STEP 1: Simplified Setup - 90% of validation infrastructure in one call
// The application name is automatically retrieved from IApplicationNameProvider
builder.Services.AddExampleLibValidation();

// STEP 2: Configure your existing database contexts
builder.AddServiceDefaults();
builder.AddSqlServerDbContext<SampleDbContext>("Sql");
builder.AddSqlServerDbContext<TheNannyDbContext>("Sql");
builder.AddMongoDBClient("mongodb");

// STEP 3: Entity-specific validation setup using the new fluent API
builder.Services.ConfigureValidation<SampleEntity>(config => config
    .WithMetricValidation(
        metricSelector: entity => (decimal)entity.Value,
        summaryThreshold: 0.0m,
        sequenceThreshold: 5.0)
    .WithRules(
        entity => !string.IsNullOrWhiteSpace(entity.Name),
        entity => entity.Value >= 0,
        entity => entity.Validated)
    .WithEntityIdSelector(entity => entity.Name));

builder.Services.ConfigureValidation<OtherEntity>(config => config
    .WithMetricValidation(
        metricSelector: entity => entity.Amount,
        summaryThreshold: 1.0m,
        sequenceThreshold: 1.0)
    .WithRules(
        entity => !string.IsNullOrWhiteSpace(entity.Code),
        entity => entity.Amount > 0,
        entity => entity.IsActive,
        entity => entity.Validated)
    .WithEntityIdSelector(entity => entity.Code));

// STEP 4: Register EF repositories WITH ValidationRunner integration as primary implementation
// This shows how to modify existing repository registrations to include validation
builder.Services.AddScoped<WorkerService1.Repositories.IRepository<SampleEntity>>(sp =>
    new WorkerService1.Repositories.EfRepository<SampleEntity>(
        sp.GetRequiredService<SampleDbContext>(),
        sp.GetRequiredService<IValidationRunner>())); // <- ADD ValidationRunner to existing repositories

builder.Services.AddScoped<WorkerService1.Repositories.IRepository<OtherEntity>>(sp =>
    new WorkerService1.Repositories.EfRepository<OtherEntity>(
        sp.GetRequiredService<SampleDbContext>(),
        sp.GetRequiredService<IValidationRunner>())); // <- ADD ValidationRunner to existing repositories

// STEP 5: Register MongoDB repositories as additional implementations for specific scenarios
// Note: These are used by specific workers that request MongoDB explicitly
builder.Services.AddScoped<WorkerService1.Repositories.MongoRepository<SampleEntity>>(sp =>
    new WorkerService1.Repositories.MongoRepository<SampleEntity>(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<IValidationRunner>(), // <- ADD ValidationRunner to existing repositories
        "WorkerService", "SampleEntities"));

builder.Services.AddScoped<WorkerService1.Repositories.MongoRepository<OtherEntity>>(sp =>
    new WorkerService1.Repositories.MongoRepository<OtherEntity>(
        sp.GetRequiredService<IMongoClient>(),
        sp.GetRequiredService<IValidationRunner>(), // <- ADD ValidationRunner to existing repositories
        "WorkerService", "OtherEntities"));

// Register existing application services (unchanged)
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
 * COMPARISON: Old vs New Approach
 * 
 * OLD (Fluent Configuration):
 * - Required 25+ lines of configuration
 * - Needed to manually configure stores, providers, and plans
 * - Separate calls for summarisation and validation plans
 * - Complex setup for EntityIdProvider
 * - Manual dependency management
 * 
 * NEW (Simplified Approach):
 * - Only 3 key method calls: AddExampleLibValidation() + ConfigureValidation<T>()
 * - 90% of infrastructure setup in one call
 * - Combined metric validation (handles both summarisation and sequence validation)
 * - Fluent entity-specific configuration
 * - Automatic dependency resolution
 * - Application name automatically retrieved from IApplicationNameProvider
 * 
 * Benefits:
 * 1. Reduced complexity: 25+ lines ? 3 method calls
 * 2. Better discoverability: Fluent API guides users
 * 3. Consistent patterns: Same approach for all entities
 * 4. Less error-prone: Automatic dependency registration
 * 5. Easier testing: Simplified test setup
 * 6. Logical grouping: Related validations configured together
 */
