using ExampleData;
using ExampleLib.Domain;
using DataSaveAudit = ExampleData.SaveAudit;
using DomainSaveAudit = ExampleLib.Domain.SaveAudit;
using Microsoft.EntityFrameworkCore;

namespace ExampleLib.Infrastructure;

/// <summary>
/// Entity Framework implementation of <see cref="ISaveAuditRepository"/>.
/// </summary>
public class EfSaveAuditRepository : ISaveAuditRepository
{
    private readonly YourDbContext _context;

    public EfSaveAuditRepository(YourDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public DomainSaveAudit? GetLastAudit(string entityType, string entityId)
    {
        var entity = _context.SaveAudits
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Id)
            .FirstOrDefault();

        return entity == null
            ? null
            : new DomainSaveAudit
            {
                EntityType = entity.EntityType,
                EntityId = entity.EntityId,
                MetricValue = entity.MetricValue,
                Validated = entity.Validated,
                Timestamp = entity.Timestamp
            };
    }

    /// <inheritdoc />
    public void AddAudit(DomainSaveAudit audit)
    {
        var entity = _context.SaveAudits.FirstOrDefault(a => a.EntityType == audit.EntityType && a.EntityId == audit.EntityId);
        if (entity == null)
        {
            entity = new DataSaveAudit
            {
                EntityType = audit.EntityType,
                EntityId = audit.EntityId,
                MetricValue = audit.MetricValue,
                Timestamp = audit.Timestamp,
                Validated = audit.Validated
            };
            _context.SaveAudits.Add(entity);
        }
        else
        {
            entity.MetricValue = audit.MetricValue;
            entity.Timestamp = audit.Timestamp;
            entity.Validated = audit.Validated;
            _context.SaveAudits.Update(entity);
        }
        _context.SaveChanges();
    }
}
