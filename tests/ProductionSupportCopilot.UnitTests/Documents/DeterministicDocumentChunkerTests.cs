using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Infrastructure.Documents;

namespace ProductionSupportCopilot.UnitTests.Documents;

public sealed class DeterministicDocumentChunkerTests
{
    private readonly DeterministicDocumentChunker _sut = new();

    [Fact]
    public void Chunk_WithSameInput_ReturnsIdenticalChunks()
    {
        const string content = "Booking API troubleshooting. Check the health endpoint first.\n\nThen inspect Kafka consumer lag and recent errors.";
        var options = new ChunkingOptions(100, 20);

        var first = _sut.Chunk(content, options);
        var second = _sut.Chunk(content, options);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Chunk_WithLongContent_RespectsMaximumSizeAndPreservesOrder()
    {
        var content = string.Join(" ", Enumerable.Repeat("booking status update delayed because consumer processing is slow.", 20));
        var options = new ChunkingOptions(180, 30);

        var chunks = _sut.Chunk(content, options);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Content.Length, 1, options.MaximumCharacters));
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(chunk => chunk.Sequence));
    }

    [Fact]
    public void Chunk_WithBlankContent_ReturnsNoChunks()
    {
        var chunks = _sut.Chunk("   ");

        Assert.Empty(chunks);
    }
}
