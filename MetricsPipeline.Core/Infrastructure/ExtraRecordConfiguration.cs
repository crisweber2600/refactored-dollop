using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsPipeline.Infrastructure;

public class ExtraRecordConfiguration : IEntityTypeConfiguration<ExtraRecord>
{
    public void Configure(EntityTypeBuilder<ExtraRecord> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(40);
    }
}
