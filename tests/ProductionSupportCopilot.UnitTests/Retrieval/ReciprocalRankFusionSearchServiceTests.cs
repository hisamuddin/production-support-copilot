using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure.Retrieval;

namespace ProductionSupportCopilot.UnitTests.Retrieval;

public sealed class ReciprocalRankFusionSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_RewardsCandidatePresentInBothRankings()
    {
        var shared = Result(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Shared result");
        var lexicalOnly = Result(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Lexical only");
        var semanticOnly = Result(Guid.Parse("30000000-0000-0000-0000-000000000003"), "Semantic only");

        var sut = new ReciprocalRankFusionSearchService(
            new LexicalStub([lexicalOnly, shared]),
            new SemanticStub([semanticOnly, shared]));

        var results = await sut.SearchAsync(Guid.NewGuid(), "booking missing", 3);

        Assert.Equal(shared.ChunkId, results[0].ChunkId);
        Assert.Equal(3, results.Count);
    }

    private static DocumentSearchResult Result(Guid id, string title) =>
        new(id, id, title, 0, title, 0.5);

    private sealed class LexicalStub(IReadOnlyList<DocumentSearchResult> results)
        : IDocumentSearchService
    {
        public Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
            Guid tenantId,
            string query,
            int limit = 5,
            CancellationToken cancellationToken = default) => Task.FromResult(results);
    }

    private sealed class SemanticStub(IReadOnlyList<DocumentSearchResult> results)
        : ISemanticDocumentSearchService
    {
        public Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
            Guid tenantId,
            string query,
            int limit = 5,
            CancellationToken cancellationToken = default) => Task.FromResult(results);
    }
}
