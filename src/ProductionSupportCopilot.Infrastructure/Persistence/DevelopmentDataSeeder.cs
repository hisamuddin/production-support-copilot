using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Domain.Knowledge;

namespace ProductionSupportCopilot.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder(
    SupportCopilotDbContext dbContext,
    IDocumentChunker chunker,
    IEmbeddingProvider embeddings)
{
    public const string DemoTenantKey = "demo-tenant";

    private static readonly SeedDocument[] Documents =
    [
        new("runbook-kafka-consumer-lag", "Kafka Consumer Lag Troubleshooting", "kafka-consumer-lag.md"),
        new("runbook-poison-event-dlq", "Poison Event and DLQ Recovery", "poison-event-dlq.md"),
        new("runbook-booking-status-delay", "Booking Status Update Delay Investigation", "booking-status-delay.md")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(
            item => item.Key == DemoTenantKey,
            cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant(DemoTenantKey, "Demo Transport Operations");
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var seed in Documents)
        {
            if (await dbContext.Documents.AnyAsync(
                document => document.TenantId == tenant.Id && document.ExternalId == seed.ExternalId,
                cancellationToken))
            {
                continue;
            }

            var content = ReadEmbeddedRunbook(seed.FileName);
            var document = new SupportDocument(
                tenant.Id,
                seed.ExternalId,
                seed.Title,
                content,
                "runbook",
                "internal");

            dbContext.Documents.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var draft in chunker.Chunk(content))
            {
                var chunk = new DocumentChunk(
                    document.Id,
                    tenant.Id,
                    draft.Ordinal,
                    draft.Content,
                    draft.StartCharacter,
                    draft.EndCharacter);
                chunk.SetEmbedding(await embeddings.EmbedAsync(draft.Content, cancellationToken));
                dbContext.DocumentChunks.Add(chunk);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ReadEmbeddedRunbook(string fileName)
    {
        var assembly = typeof(DevelopmentDataSeeder).Assembly;
        var suffix = $".SeedData.{fileName}";
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(suffix, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded runbook '{fileName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed record SeedDocument(string ExternalId, string Title, string FileName);
}
