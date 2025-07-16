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

namespace WorkerService1;

/// <summary>
/// Demonstrates how ExampleLib.Infrastructure.ValidationRunner integrates with existing repositories
/// to provide comprehensive validation including manual rules, summarisation, and sequence validation.
/// </summary>
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
        
        // Get the repository with integrated ValidationRunner and audit database context for seeding
        var sampleRepo = scope.ServiceProvider.GetRequiredService<WorkerService1.Repositories.IRepository<SampleEntity>>();
        var auditDbContext = scope.ServiceProvider.GetRequiredService<TheNannyDbContext>();

        _logger.LogInformation("Starting ValidationDemoWorker - demonstrating ExampleLib.Infrastructure.ValidationRunner integration");

        // Step 1: Seed the SaveAudit database with initial audit records
        await SeedAuditDatabaseAsync(auditDbContext, stoppingToken);

        // Step 2: Create new entities to insert using the repository
        var newEntities = new List<SampleEntity>
        {
            new SampleEntity { Name = "A", Value = 2.0, Validated = true },
            new SampleEntity { Name = "B", Value = 7.0, Validated = true },
            new SampleEntity { Name = "C", Value = 10.0, Validated = true },
            new SampleEntity { Name = "D", Value = 15.0, Validated = true } // This should pass validation against audit (20.0 - 15.0 = 5.0 <= 5.0)
        };

        _logger.LogInformation("Inserting {Count} new entities using repository (ValidationRunner handles all validation automatically)", newEntities.Count);

        // Step 3: Use the repository which automatically handles all validation through ValidationRunner
        int successCount = 0;
        int failureCount = 0;

        foreach (var entity in newEntities)
        {
            try
            {
                // The repository's AddAsync method automatically calls ValidationRunner.ValidateAsync, which includes:
                // 1. Manual validation (rules defined in Program.cs)
                // 2. Summarisation validation (SummarisationPlan)
                // 3. Sequence validation (ValidationPlan against SaveAudit records)
                await sampleRepo.AddAsync(entity);
                successCount++;
                _logger.LogInformation("Successfully inserted entity: Name={Name}, Value={Value} (all validations passed)", entity.Name, entity.Value);
            }
            catch (InvalidOperationException ex)
            {
                failureCount++;
                _logger.LogWarning("Failed to insert entity: Name={Name}, Value={Value}. Reason: {Reason}", entity.Name, entity.Value, ex.Message);
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Unexpected error inserting entity: Name={Name}, Value={Value}", entity.Name, entity.Value);
            }
        }

        _logger.LogInformation("Repository insertion complete. Success: {SuccessCount}, Failures: {FailureCount}", successCount, failureCount);

        // Step 4: Show final SaveAudit database state
        await LogAuditDatabaseStateAsync(auditDbContext, stoppingToken);

        // Wait before next execution (this is just a demo)
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    }

    private async Task SeedAuditDatabaseAsync(TheNannyDbContext auditDbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding SaveAudit database with initial audit records...");

        try
        {
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
                    EntityId = "A", // Name field value (from EntityIdProvider configuration)
                    ApplicationName = "ValidationDemo",
                    MetricValue = 7.0m, // Previous Value
                    BatchSize = 1,
                    Validated = true,
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
                },
                new SaveAudit 
                { 
                    EntityType = nameof(SampleEntity),
                    EntityId = "B", // Name field value (from EntityIdProvider configuration)
                    ApplicationName = "ValidationDemo",
                    MetricValue = 4.0m, // Previous Value
                    BatchSize = 1,
                    Validated = true,
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
                },
                new SaveAudit 
                { 
                    EntityType = nameof(SampleEntity),
                    EntityId = "D", // Name field value (from EntityIdProvider configuration)
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Seeding SaveAudit database was canceled.");
            // Swallow exception to allow graceful shutdown
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
