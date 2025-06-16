using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MetricsPipeline.Infrastructure;

public class SummaryRecordConfiguration : IEntityTypeConfiguration<SummaryRecord>
{
    public void Configure(EntityTypeBuilder<SummaryRecord> builder)
    {
        builder.Property(p => p.PipelineName).HasMaxLength(50).IsRequired();
    }
}
