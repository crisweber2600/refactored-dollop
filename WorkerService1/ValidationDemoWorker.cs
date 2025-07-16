using ExampleLib.Domain;
using ExampleLib.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkerService1.Models;
using WorkerService1.Repositories;

namespace WorkerService1;

public class ValidationDemoWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationDemoWorker> _logger;

    public ValidationDemoWorker(IServiceProvider serviceProvider, ILogger<ValidationDemoWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        // Get TheNannyDbContext (for SaveAudit records) and the repository
        var sampleRepo = scope.ServiceProvider.GetRequiredService<IRepository<SampleEntity>>();
        var auditDbContext = scope.ServiceProvider.GetRequiredService<TheNannyDbContext>();
        var plan = scope.ServiceProvider.GetRequiredService<ValidationPlan>();

        _logger.LogInformation("Starting ValidationDemoWorker with SaveAudit validation, threshold: {Threshold}", plan.Threshold);

        // Step 1: Seed the SaveAudit database with initial audit records
        await SeedAuditDatabaseAsync(auditDbContext, stoppingToken);

        // Step 2: Create new entities to validate against SaveAudit records
        var newEntities = new List<SampleEntity>
        {
            new SampleEntity { Name = "A", Value = 2.0, Validated = true },
            new SampleEntity { Name = "B", Value = 7.0, Validated = true },
            new SampleEntity { Name = "C", Value = 10.0, Validated = true },
            new SampleEntity { Name = "D", Value = 15.0, Validated = true } // This should pass validation against audit (20.0 - 15.0 = 5.0 <= 5.0)
        };

        _logger.LogInformation("Validating {Count} new entities against existing SaveAudit records", newEntities.Count);

        // Step 3: Use SequenceValidator extensions with IEntityIdProvider for automatic key mapping
        var entityIdProvider = scope.ServiceProvider.GetRequiredService<IEntityIdProvider>();
        bool valid = await SequenceValidatorExtensions.ValidateWithPlanAndProviderAsync(
            newEntities,
            auditDbContext.SaveAudits,
            entityIdProvider,
            plan,
            e => (decimal)e.Value, // valueSelector: convert entity Value to decimal
            stoppingToken
        );

        _logger.LogInformation("SequenceValidator result using SaveAudit validation (threshold: {Threshold}): {Result}", 
            plan.Threshold, valid);

        if (valid)
        {
            _logger.LogInformation("All new entities are valid against SaveAudit records. Proceeding with insertion.");
            
            // Step 4: Insert the valid entities (SaveAudit records will be created automatically by ValidationRunner)
            foreach (var entity in newEntities)
            {
                await sampleRepo.AddAsync(entity);
                _logger.LogInformation("Inserted entity: Name={Name}, Value={Value} (SaveAudit created automatically)", entity.Name, entity.Value);
            }
        }
        else
        {
            _logger.LogWarning("Some new entities failed validation against SaveAudit records. Insertion aborted.");
            
            // Show detailed validation results for each entity
            await LogDetailedAuditValidationResultsAsync(newEntities, auditDbContext, plan, stoppingToken);
        }

        // Step 5: Show final SaveAudit database state
        await LogAuditDatabaseStateAsync(auditDbContext, stoppingToken);

        // Wait before next execution (this is just a demo)
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }

    private async Task SeedAuditDatabaseAsync(TheNannyDbContext auditDbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding SaveAudit database with initial audit records...");

        // Check if we already have SaveAudit data
        var existingCount = await auditDbContext.SaveAudits
            .Where(a => a.EntityType == nameof(SampleEntity))
            .CountAsync(cancellationToken);
        
        if (existingCount > 0)
        {
            _logger.LogInformation("SaveAudit database already contains {Count} SampleEntity records, skipping seed", existingCount);
            return;
        }

        // Seed with initial SaveAudit records representing previous entity saves
        var seedAudits = new List<SaveAudit>
        {
            new SaveAudit 
            { 
                EntityType = nameof(SampleEntity),
                EntityId = "A", // Name field value
                ApplicationName = "ValidationDemo",
                MetricValue = 7.0m, // Previous Value
                BatchSize = 1,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
            new SaveAudit 
            { 
                EntityType = nameof(SampleEntity),
                EntityId = "B", // Name field value
                ApplicationName = "ValidationDemo",
                MetricValue = 4.0m, // Previous Value
                BatchSize = 1,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
            },
            new SaveAudit 
            { 
                EntityType = nameof(SampleEntity),
                EntityId = "D", // Name field value
                ApplicationName = "ValidationDemo",
                MetricValue = 20.0m, // Previous Value
                BatchSize = 1,
                Validated = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2)
            }
        };

        foreach (var audit in seedAudits)
        {
            auditDbContext.SaveAudits.Add(audit);
            _logger.LogInformation("Seeded SaveAudit: EntityId={EntityId}, MetricValue={MetricValue}", 
                audit.EntityId, audit.MetricValue);
        }

        await auditDbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SaveAudit database seeding completed with {Count} records", seedAudits.Count);
    }

    private async Task LogDetailedAuditValidationResultsAsync(List<SampleEntity> newEntities, TheNannyDbContext auditDbContext, ValidationPlan plan, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Detailed SaveAudit validation results:");
        
        foreach (var newEntity in newEntities)
        {
            var latestAudit = await auditDbContext.SaveAudits
                .Where(a => a.EntityType == nameof(SampleEntity) && a.EntityId == newEntity.Name)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (latestAudit != null)
            {
                var newValue = (decimal)newEntity.Value;
                var auditValue = latestAudit.MetricValue;
                var difference = Math.Abs(newValue - auditValue);
                var isValid = difference <= (decimal)plan.Threshold;
                
                _logger.LogInformation("Entity Name={Name}: New Value={NewValue}, SaveAudit MetricValue={AuditValue}, Difference={Difference}, Valid={IsValid} (threshold={Threshold})",
                    newEntity.Name, newValue, auditValue, difference, isValid, plan.Threshold);
            }
            else
            {
                _logger.LogInformation("Entity Name={Name}: No SaveAudit record found, validation passes by default", newEntity.Name);
            }
        }
    }

    private async Task LogAuditDatabaseStateAsync(TheNannyDbContext auditDbContext, CancellationToken cancellationToken)
    {
        var allAudits = await auditDbContext.SaveAudits
            .Where(a => a.EntityType == nameof(SampleEntity))
            .OrderBy(a => a.EntityId)
            .ThenBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("Final SaveAudit database state ({Count} SampleEntity records):", allAudits.Count);
        foreach (var audit in allAudits)
        {
            _logger.LogInformation("  Id={Id}, EntityId={EntityId}, MetricValue={MetricValue}, Timestamp={Timestamp}", 
                audit.Id, audit.EntityId, audit.MetricValue, audit.Timestamp);
        }
    }
}
