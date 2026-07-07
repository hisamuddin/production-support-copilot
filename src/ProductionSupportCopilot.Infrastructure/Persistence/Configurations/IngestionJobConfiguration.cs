using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionSupportCopilot.Domain.Documents;
using ProductionSupportCopilot.Domain.Ingestion;

namespace ProductionSupportCopilot.Infrastructure.Persistence.Configurations;

internal sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("ingestion_jobs");
        builder.HasKey(job => job.Id);

        builder.Property(job => job.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(job => job.FailureReason).HasMaxLength(2_000);

        builder.HasIndex(job => new { job.TenantId, job.IdempotencyKey }).IsUnique();
        builder.HasOne<SupportDocument>()
            .WithMany()
            .HasForeignKey(job => job.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
