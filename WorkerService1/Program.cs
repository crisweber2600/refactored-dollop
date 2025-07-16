using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using MongoDB.Driver;
using WorkerService1;
using WorkerService1.Models;
using WorkerService1.Repositories;
using WorkerService1.Services;

var builder = Host.CreateApplicationBuilder(args);

// STEP 1: Configure ExampleLib validation services using fluent configuration
// This shows how easy it is to add ExampleLib to an existing application
builder.Services.ConfigureExampleLib(config =>
{
    config.WithApplicationName(builder.Environment.ApplicationName)
          .UseEntityFramework() // Primary data store
          .WithConfigurableEntityIds(provider =>
          {
              // Register custom selectors for different entity types
              provider.RegisterSelector<SampleEntity>(entity => entity.Name);
              provider.RegisterSelector<OtherEntity>(entity => entity.Code);
          })
          .AddSummarisationPlan<SampleEntity>(
              entity => (decimal)entity.Value,
              ThresholdType.RawDifference,
              0.0m)
          .AddSummarisationPlan<OtherEntity>(
              entity => entity.Amount,
              ThresholdType.RawDifference,
              1.0m)
          .AddValidationPlan<SampleEntity>(threshold: 5.0, ValidationStrategy.Count)
          .AddValidationPlan<OtherEntity>(threshold: 1.0, ValidationStrategy.Count)
          .AddValidationRules<SampleEntity>(
              entity => !string.IsNullOrWhiteSpace(entity.Name),
              entity => entity.Value >= 0,
              entity => entity.Validated)
          .AddValidationRules<OtherEntity>(
              entity => !string.IsNullOrWhiteSpace(entity.Code),
              entity => entity.Amount > 0,
              entity => entity.IsActive,
              entity => entity.Validated);
});

// Remove default ISaveAuditRepository registration from ExampleLib
var defaultAuditDescriptor = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(ISaveAuditRepository));
if (defaultAuditDescriptor != null)
{
    builder.Services.Remove(defaultAuditDescriptor);
}

builder.AddServiceDefaults();

// STEP 2: Configure your existing database contexts
builder.AddSqlServerDbContext<SampleDbContext>("Sql");
builder.AddSqlServerDbContext<TheNannyDbContext>("Sql");
builder.AddMongoDBClient("mongodb");

// STEP 3: Register ExampleLib audit repository
// Register EfSaveAuditRepository as the ISaveAuditRepository implementation using TheNannyDbContext
builder.Services.AddScoped<ISaveAuditRepository>(sp =>
    new EfSaveAuditRepository(sp.GetRequiredService<TheNannyDbContext>()));

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
