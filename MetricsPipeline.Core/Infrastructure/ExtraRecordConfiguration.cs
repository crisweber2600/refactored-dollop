using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsPipeline.Infrastructure;

/// <summary>
/// EF Core configuration for <see cref="ExtraRecord"/>.
/// </summary>
public class ExtraRecordConfiguration : IEntityTypeConfiguration<ExtraRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExtraRecord> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(40);
    }
}
