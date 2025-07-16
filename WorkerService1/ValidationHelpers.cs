using ExampleLib.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkerService1.Models;
using ExampleLib.Infrastructure;

namespace WorkerService1;

public static class ValidationHelpers
{
    /// <summary>
    /// Validates a sequence of OtherEntity by comparing the Code property for each entity with the previous entity with the same Id.
    /// Uses SequenceValidator with Id as the key and Code as the value.
    /// </summary>
    public static bool ValidateOtherEntityCodes(IEnumerable<OtherEntity> entities)
    {
        return SequenceValidator.Validate(
            entities,
            e => e.Code, // wheneverSelector: group by Id
            e => e.Amount // valueSelector: compare Code
        );
    }

    /// <summary>
    /// Validates a sequence of SampleEntity by comparing the Name property for each entity with the previous entity with the same Id.
    /// Uses SequenceValidator with Id as the key and Name as the value.
    /// </summary>
    public static bool ValidateSampleEntityNames(IEnumerable<SampleEntity> entities)
    {
        return SequenceValidator.Validate(
            entities,
            e => e.Name, // wheneverSelector: group by Id
            e => e.Value // valueSelector: compare Name
        );
    }

    /// <summary>
    /// Validates a sequence of OtherEntity by comparing the Code property for each entity with the latest SaveAudit from the database for the same Code.
    /// </summary>
    public static async Task<bool> ValidateOtherEntityCodesAsync(IEnumerable<OtherEntity> entities, TheNannyDbContext db)
    {
        foreach (var entity in entities)
        {
            // Use Code as the key for audit lookup
            var latestAudit = await db.SaveAudits
                .Where(a => a.EntityType == nameof(OtherEntity) && a.EntityId == entity.Code)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            if (latestAudit != null)
            {
                // Compare Amount to MetricValue from audit
                if (entity.Amount.ToString() != latestAudit.MetricValue.ToString())
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates a sequence of SampleEntity by comparing the Name property for each entity with the latest SaveAudit from the database for the same Name.
    /// </summary>
    public static async Task<bool> ValidateSampleEntityNamesAsync(IEnumerable<SampleEntity> entities, TheNannyDbContext db)
    {
        foreach (var entity in entities)
        {
            // Use Name as the key for audit lookup
            var latestAudit = await db.SaveAudits
                .Where(a => a.EntityType == nameof(SampleEntity) && a.EntityId == entity.Name)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            if (latestAudit != null)
            {
                // Compare Value to MetricValue from audit
                if (entity.Value.ToString() != latestAudit.MetricValue.ToString())
                    return false;
            }
        }
        return true;
    }
}
