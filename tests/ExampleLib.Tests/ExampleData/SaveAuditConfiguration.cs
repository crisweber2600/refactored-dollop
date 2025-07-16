using ExampleLib.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExampleData;

public class SaveAuditConfiguration : IEntityTypeConfiguration<SaveAudit>
{
    public void Configure(EntityTypeBuilder<SaveAudit> builder)
    {
        builder.HasKey(e => e.Id);
        
        // Configure MetricValue decimal with explicit precision and scale
        // Using precision 18 and scale 6 to accommodate a wide range of values
        builder.Property(e => e.MetricValue)
            .HasPrecision(18, 6);
        
        // Configure string properties with max lengths
        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(e => e.EntityId)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(e => e.ApplicationName)
            .IsRequired()
            .HasMaxLength(256);
    }
}