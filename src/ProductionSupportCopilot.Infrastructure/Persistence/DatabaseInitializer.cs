using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductionSupportCopilot.Application.Documents;

namespace ProductionSupportCopilot.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportCopilotDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        if (await db.Documents.AnyAsync(cancellationToken)) return;

        var ingestion = scope.ServiceProvider.GetRequiredService<IDocumentIngestionService>();
        var seed = new[]
        {
            new IngestDocumentCommand("seed-consumer-lag-v1", "demo-tenant", "runbook-consumer-lag", "Booking Status Consumer Lag Runbook", "Symptoms: booking status remains accepted while downstream assignment is complete. Check status-indexer consumer lag and partition skew. If lag is above 500 for five minutes, inspect slow handlers and downstream timeouts. Scaling consumers cannot exceed partition count. Do not reset offsets without incident commander approval. Recovery: stop the faulty deployment, verify checkpoint, deploy the fix, and monitor lag until it reaches zero.", "runbook"),
            new IngestDocumentCommand("seed-dlq-v1", "demo-tenant", "runbook-dlq", "Poison Event and DLQ Recovery", "A poison event repeatedly fails schema or business validation. The consumer retries three times with exponential backoff and then publishes the original event plus diagnostic context to the dead-letter topic. Investigate payload and schema version first. Replaying the DLQ is consequential: create a change record, obtain operator approval, replay a bounded batch, and verify idempotency before continuing.", "runbook"),
            new IngestDocumentCommand("seed-health-v1", "demo-tenant", "runbook-health", "Booking API Health Investigation", "For current health use the service-health tool rather than stale documentation. Healthy targets: error rate below one percent, p95 latency below 500 milliseconds, and no sustained dependency timeout. Correlate request IDs across API, assignment service, Kafka producer, and consumer logs. Never claim current production health using only this runbook.", "runbook")
        };
        foreach (var command in seed) await ingestion.IngestAsync(command, cancellationToken);
    }
}
