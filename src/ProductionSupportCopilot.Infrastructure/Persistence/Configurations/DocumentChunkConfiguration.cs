using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;
using ProductionSupportCopilot.Domain.Documents;

namespace ProductionSupportCopilot.Infrastructure.Persistence.Configurations;

internal sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");
        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Content).HasColumnType("text").IsRequired();
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnType("tsvector")
            .HasComputedColumnSql(
                "to_tsvector('english'::regconfig, COALESCE(\"Content\", ''::text))",
                stored: true);

        builder.HasIndex("SearchVector").HasMethod("GIN");
        builder.HasIndex(chunk => new { chunk.TenantId, chunk.DocumentId, chunk.DocumentVersion, chunk.Sequence }).IsUnique();

        builder.HasOne<SupportDocument>()
            .WithMany()
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
