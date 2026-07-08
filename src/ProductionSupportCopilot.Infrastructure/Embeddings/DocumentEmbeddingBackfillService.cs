using Microsoft.EntityFrameworkCore;
using Pgvector;
using ProductionSupportCopilot.Application.Embeddings;
using ProductionSupportCopilot.Infrastructure.Persistence;

namespace ProductionSupportCopilot.Infrastructure.Embeddings;

public sealed class DocumentEmbeddingBackfillService(
    SupportCopilotDbContext dbContext,
    IEmbeddingProvider embeddingProvider)
{
    public const int ExpectedDimensions = 384;

    public async Task<int> BackfillMissingAsync(CancellationToken cancellationToken = default)
    {
        var chunks = await dbContext.DocumentChunks
            .Where(chunk => EF.Property<Vector?>(chunk, "Embedding") == null)
            .OrderBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.Sequence)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return 0;
        }

        var updated = 0;
        foreach (var batch in chunks.Chunk(32))
        {
            var embeddingBatch = await embeddingProvider.GenerateAsync(
                batch.Select(chunk => chunk.Content).ToArray(),
                cancellationToken);

            if (embeddingBatch.Dimensions != ExpectedDimensions)
            {
                throw new InvalidOperationException(
                    $"Expected {ExpectedDimensions} embedding dimensions but received {embeddingBatch.Dimensions}.");
            }

            for (var index = 0; index < batch.Length; index++)
            {
                dbContext.Entry(batch[index]).Property("Embedding").CurrentValue = new Vector(embeddingBatch.Vectors[index]);
                updated++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return updated;
    }
}
