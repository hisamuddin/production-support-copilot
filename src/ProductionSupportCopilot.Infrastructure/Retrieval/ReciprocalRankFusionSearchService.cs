using ProductionSupportCopilot.Application.Retrieval;

namespace ProductionSupportCopilot.Infrastructure.Retrieval;

public sealed class ReciprocalRankFusionSearchService(
    IDocumentSearchService lexicalSearch,
    ISemanticDocumentSearchService semanticSearch)
    : IHybridDocumentSearchService
{
    private const int RankConstant = 60;

    public async Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        Guid tenantId,
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(limit));

        var candidateLimit = Math.Min(20, Math.Max(limit * 4, 10));

        // These are sequential because both services share one scoped EF DbContext,
        // which does not support concurrent operations.
        var lexicalResults = await lexicalSearch.SearchAsync(
            tenantId,
            query,
            candidateLimit,
            cancellationToken);
        var semanticResults = await semanticSearch.SearchAsync(
            tenantId,
            query,
            candidateLimit,
            cancellationToken);

        var candidates = new Dictionary<Guid, FusedCandidate>();
        AddRankedResults(candidates, lexicalResults);
        AddRankedResults(candidates, semanticResults);

        return candidates.Values
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Result.DocumentId)
            .ThenBy(candidate => candidate.Result.Sequence)
            .Take(limit)
            .Select(candidate => candidate.Result with { Score = candidate.Score })
            .ToArray();
    }

    private static void AddRankedResults(
        IDictionary<Guid, FusedCandidate> candidates,
        IReadOnlyList<DocumentSearchResult> results)
    {
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            var contribution = 1d / (RankConstant + index + 1);

            if (candidates.TryGetValue(result.ChunkId, out var existing))
            {
                candidates[result.ChunkId] = existing with { Score = existing.Score + contribution };
            }
            else
            {
                candidates[result.ChunkId] = new FusedCandidate(result, contribution);
            }
        }
    }

    private sealed record FusedCandidate(DocumentSearchResult Result, double Score);
}
