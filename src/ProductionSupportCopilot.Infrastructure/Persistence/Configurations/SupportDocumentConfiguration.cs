using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionSupportCopilot.Domain.Documents;

namespace ProductionSupportCopilot.Infrastructure.Persistence.Configurations;

internal sealed class SupportDocumentConfiguration : IEntityTypeConfiguration<SupportDocument>
{
    public void Configure(EntityTypeBuilder<SupportDocument> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(document => document.Id);

        builder.Property(document => document.Title).HasMaxLength(300).IsRequired();
        builder.Property(document => document.SourceType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(document => document.SourceReference).HasMaxLength(1_000).IsRequired();

        builder.HasIndex(document => new { document.TenantId, document.SourceReference }).IsUnique();
        builder.HasIndex(document => new { document.TenantId, document.SourceType });
    }
}
