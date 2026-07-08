using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Domain.Ingestion;
using ProductionSupportCopilot.Domain.Knowledge;
using ProductionSupportCopilot.Domain.Workflow;

namespace ProductionSupportCopilot.Infrastructure.Persistence;

public sealed class SupportCopilotDbContext(DbContextOptions<SupportCopilotDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SupportDocument> Documents => Set<SupportDocument>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<SupportDocument>(entity =>
        {
            entity.ToTable("support_documents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
            entity.Property(x => x.ExternalId).HasMaxLength(200);
            entity.Property(x => x.Title).HasMaxLength(300);
            entity.Property(x => x.SourceType).HasMaxLength(50);
            entity.Property(x => x.Classification).HasMaxLength(50);
            entity.HasOne(x => x.Tenant).WithMany(x => x.Documents).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DocumentId, x.Ordinal }).IsUnique();
            entity.Property(x => x.Embedding).HasColumnType("vector(384)");
            entity.HasOne(x => x.Document).WithMany(x => x.Chunks).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IngestionJob>(entity =>
        {
            entity.ToTable("ingestion_jobs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.EventId).IsUnique();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.ToTable("approval_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });
    }
}
