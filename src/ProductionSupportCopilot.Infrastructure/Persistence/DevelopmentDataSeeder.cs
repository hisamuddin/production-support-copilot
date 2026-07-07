using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Domain.Documents;
using ProductionSupportCopilot.Domain.Tenants;

namespace ProductionSupportCopilot.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder(
    SupportCopilotDbContext dbContext,
    IDocumentChunker chunker)
{
    public static readonly Guid DemoTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly SeedDocument[] Documents =
    [
        new(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Kafka Consumer Lag Troubleshooting", "kafka-consumer-lag.md"),
        new(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Poison Event and DLQ Recovery", "poison-event-dlq.md"),
        new(Guid.Parse("20000000-0000-0000-0000-000000000003"), "Booking Status Update Delay Investigation", "booking-status-delay.md")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Tenants.AnyAsync(tenant => tenant.Id == DemoTenantId, cancellationToken))
        {
            dbContext.Tenants.Add(new Tenant(DemoTenantId, "Demo Transport Operations"));
        }

        foreach (var seed in Documents)
        {
            if (await dbContext.Documents.AnyAsync(document => document.Id == seed.Id, cancellationToken))
            {
                continue;
            }

            var content = ReadEmbeddedRunbook(seed.FileName);
            var now = DateTimeOffset.UtcNow;
            var document = new SupportDocument(
                seed.Id,
                DemoTenantId,
                seed.Title,
                DocumentSourceType.Runbook,
                $"embedded://runbooks/{seed.FileName}",
                now);

            dbContext.Documents.Add(document);

            foreach (var textChunk in chunker.Chunk(content))
            {
                dbContext.DocumentChunks.Add(new DocumentChunk(
                    CreateDeterministicGuid($"{seed.Id:N}:{textChunk.Sequence}"),
                    DemoTenantId,
                    seed.Id,
                    document.Version,
                    textChunk.Sequence,
                    textChunk.Content,
                    textChunk.StartOffset,
                    textChunk.EndOffset));
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

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private sealed record SeedDocument(Guid Id, string Title, string FileName);
}
