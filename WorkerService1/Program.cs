using WorkerService1;
using WorkerService1.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using WorkerService1.Services;
using ExampleLib.Infrastructure;
using ExampleLib.Domain;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// SQL Server Aspire connection
builder.AddSqlServerDbContext<SampleDbContext>("Sql");
builder.AddSqlServerDbContext<TheNannyDbContext>("Sql");

// --- VALIDATION PIPELINE SETUP ---
// 1. Summarisation validation (AddSaveValidation)
builder.Services.AddSaveValidation<WorkerService1.Models.SampleEntity>(
    entity => (decimal)entity.Value, // metric selector for validation
    ThresholdType.RawDifference,    // example threshold type
    0.0m,                           // example threshold value (customize as needed)
    entity => !string.IsNullOrWhiteSpace(entity.Name), // manual rule: Name must not be empty
    entity => entity.Value >= 0 // manual rule: Value must be non-negative
);

// 2. Manual validation service (AddValidatorService + AddValidatorRule)
builder.Services.AddValidatorService();
builder.Services.AddValidatorRule<WorkerService1.Models.SampleEntity>(entity => entity.Validated);

// 3. ValidationRunner (AddValidationRunner)
builder.Services.AddValidationRunner();

// 4. Validation flows from config (AddValidationFlows)
// Example: load flows from a JSON file (uncomment and provide path if needed)
// var flowsJson = File.ReadAllText("flows.json");
// var flowOptions = ExampleLib.Infrastructure.ValidationFlowOptions.Load(flowsJson);
// builder.Services.AddValidationFlows(flowOptions);

// 5. SequenceValidator usage: (not DI, but available for use in code)
// Example usage:
// var valid = ExampleLib.Domain.SequenceValidator.Validate(
//     items,
//     x => x.Server, // key selector
//     x => x.Value,  // value selector
//     (cur, prev) => Math.Abs(cur - prev) < 10 // custom comparison
// );
// --- END VALIDATION PIPELINE SETUP ---

// Register EfSaveAuditRepository for audit persistence (EF)
// builder.Services.AddScoped<ISaveAuditRepository, ExampleLib.Infrastructure.EfSaveAuditRepository>();

builder.Services.AddScoped<ISampleRepository<WorkerService1.Models.SampleEntity>, EfSampleRepository>();

// MongoDB Aspire connection
builder.AddMongoDBClient("mongodb");
builder.Services.AddScoped<MongoSampleRepository>();
builder.Services.AddScoped<ISampleRepository<WorkerService1.Models.SampleEntity>, EfSampleRepository>();
// Register ExampleLib validation pipeline for SampleEntity (Mongo)
builder.Services.AddSaveValidation<WorkerService1.Models.SampleEntity>(
    entity => (decimal)entity.Value, // metric selector for validation
    ThresholdType.RawDifference,
    0.0m
);
// Register MongoSaveAuditRepository for audit persistence (Mongo)
builder.Services.AddScoped<ExampleLib.Domain.ISaveAuditRepository, ExampleLib.Infrastructure.MongoSaveAuditRepository>();

// Register a static application name provider for validation/audit services
builder.Services.AddSingleton<IApplicationNameProvider>(
    new StaticApplicationNameProvider(builder.Environment.ApplicationName));

// Register migration worker FIRST to ensure migrations run before other workers
builder.Services.AddHostedService<MigrationWorker>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<MongoWorker>();

// Register ISampleEntityService
builder.Services.AddScoped<ISampleEntityService, SampleEntityService>();

var host = builder.Build();
host.Run();
