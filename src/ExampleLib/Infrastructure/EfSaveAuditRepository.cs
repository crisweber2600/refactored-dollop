using ExampleData;
using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Entity Framework implementation of <see cref="ISaveAuditRepository"/>.
/// </summary>
public class EfSaveAuditRepository : ISaveAuditRepository
{
    private readonly YourDbContext _context;
    private const string BatchKey = "__batch__";

    public EfSaveAuditRepository(YourDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public SaveAudit? GetLastAudit(string entityType, string entityId)
    {
        var entity = _context.SaveAudits
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Id)
            .FirstOrDefault();

        return entity;
    }

    /// <inheritdoc />
    public void AddAudit(SaveAudit audit)
    {
        var entity = _context.SaveAudits.FirstOrDefault(a => a.EntityType == audit.EntityType && a.EntityId == audit.EntityId);
        if (entity == null)
        {
            _context.SaveAudits.Add(audit);
        }
        else
        {
            entity.MetricValue = audit.MetricValue;
            entity.BatchSize = audit.BatchSize;
            entity.Timestamp = audit.Timestamp;
            entity.Validated = audit.Validated;
            _context.SaveAudits.Update(entity);
        }
        _context.SaveChanges();
    }

    public void AddBatchAudit(SaveAudit audit)
    {
        var batch = new SaveAudit
        {
            EntityType = audit.EntityType,
            EntityId = BatchKey,
            MetricValue = audit.MetricValue,
            BatchSize = audit.BatchSize,
            Validated = audit.Validated,
            Timestamp = audit.Timestamp
        };
        AddAudit(batch);
    }

    public SaveAudit? GetLastBatchAudit(string entityType)
        => GetLastAudit(entityType, BatchKey);
}
