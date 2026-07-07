using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Domain.Documents;
using ProductionSupportCopilot.Domain.Ingestion;
using ProductionSupportCopilot.Domain.Tenants;

namespace ProductionSupportCopilot.Infrastructure.Persistence;

public sealed class SupportCopilotDbContext(DbContextOptions<SupportCopilotDbContext> options)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SupportDocument> Documents => Set<SupportDocument>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupportCopilotDbContext).Assembly);
    }
}
