using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductionSupportCopilot.Domain.Documents;

namespace ProductionSupportCopilot.Infrastructure.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");
        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Content).HasColumnType("text").IsRequired();
        builder.HasIndex(chunk => new { chunk.TenantId, chunk.DocumentId, chunk.DocumentVersion, chunk.Sequence }).IsUnique();

        builder.HasOne<SupportDocument>()
            .WithMany()
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
