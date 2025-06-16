using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// EF Core configuration for <see cref="SummaryRecord"/>.
/// </summary>
public class SummaryRecordConfiguration : IEntityTypeConfiguration<SummaryRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SummaryRecord> builder)
    {
        builder.Property(p => p.PipelineName).HasMaxLength(50).IsRequired();
    }
}
