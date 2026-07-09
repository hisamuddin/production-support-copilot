using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure.Persistence;

namespace ProductionSupportCopilot.Infrastructure.Retrieval;

public sealed class PostgresHybridRetriever(SupportCopilotDbContext db, IEmbeddingProvider embeddings) : IHybridRetriever
{
    public async Task<IReadOnlyList<RetrievedPassage>> SearchAsync(RetrievalQuery query, CancellationToken cancellationToken = default)
    {
        var topK = Math.Clamp(query.TopK, 1, 20);
        var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(x => x.Key == query.TenantKey, cancellationToken);
        if (tenant is null) return [];

        var baseQuery = db.DocumentChunks.AsNoTracking()
            .Include(x => x.Document)
            .Where(x => x.TenantId == tenant.Id && (query.Classification == null || x.Document.Classification == query.Classification));

        var words = query.Query.ToLowerInvariant().Split([' ', '\r', '\n', '\t', '.', ',', '?', '!'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2).Distinct().Take(16).ToArray();

        var lexicalCandidates = await baseQuery.Take(200).ToListAsync(cancellationToken);
        var lexical = lexicalCandidates
            .Select(x => new { Chunk = x, Score = words.Sum(w => Count(x.Content, w)) / (double)Math.Max(1, words.Length) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).Take(topK * 3).ToList();

        var queryVector = new Vector(await embeddings.EmbedAsync(query.Query, cancellationToken));
        var semantic = await baseQuery.Where(x => x.Embedding != null)
            .Select(x => new { Chunk = x, Distance = x.Embedding!.CosineDistance(queryVector) })
            .OrderBy(x => x.Distance).Take(topK * 3).ToListAsync(cancellationToken);

        var lexicalRanks = lexical.Select((x, i) => (x.Chunk.Id, Rank: i + 1, x.Score)).ToDictionary(x => x.Id);
        var semanticRanks = semantic.Select((x, i) => (x.Chunk.Id, Rank: i + 1, Score: 1d - x.Distance)).ToDictionary(x => x.Id);
        var chunks = lexical.Select(x => x.Chunk).Concat(semantic.Select(x => x.Chunk)).DistinctBy(x => x.Id);

        return chunks.Select(x =>
        {
            var lex = lexicalRanks.GetValueOrDefault(x.Id);
            var sem = semanticRanks.GetValueOrDefault(x.Id);
            var rrf = (lex.Rank > 0 ? 1d / (60 + lex.Rank) : 0) + (sem.Rank > 0 ? 1d / (60 + sem.Rank) : 0);
            return new RetrievedPassage(x.DocumentId, x.Id, x.Document.Title, x.Content, x.Ordinal, lex.Score, sem.Score, rrf, x.Document.SourceType);
        }).OrderByDescending(x => x.CombinedScore).Take(topK).ToList();
    }

    private static int Count(string text, string word)
    {
        var count = 0; var start = 0;
        while ((start = text.IndexOf(word, start, StringComparison.OrdinalIgnoreCase)) >= 0) { count++; start += word.Length; }
        return count;
    }
}
