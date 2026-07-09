using ProductionSupportCopilot.Infrastructure.Documents;

namespace ProductionSupportCopilot.UnitTests.Documents;

public sealed class DeterministicDocumentChunkerTests
{
    private readonly DeterministicDocumentChunker _sut = new();

    [Fact]
    public void Chunk_LongText_ProducesOrderedOverlappingChunks()
    {
        var text = string.Join(" ", Enumerable.Repeat(
            "Booking status consumer lag causes delayed updates.",
            80));

        var result = _sut.Chunk(text, 300, 50);

        Assert.True(result.Count > 2);
        Assert.Equal(Enumerable.Range(0, result.Count), result.Select(x => x.Ordinal));
        Assert.True(result[1].StartCharacter < result[0].EndCharacter);
        Assert.All(result, chunk => Assert.InRange(chunk.Content.Length, 1, 300));
    }

    [Fact]
    public void Chunk_SameInput_IsDeterministic()
    {
        const string content = "First paragraph.\n\nSecond paragraph with useful recovery instructions.";

        var first = _sut.Chunk(content, 300, 50);
        var second = _sut.Chunk(content, 300, 50);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Chunk_WithBlankContent_ReturnsNoChunks()
    {
        var chunks = _sut.Chunk("   ");

        Assert.Empty(chunks);
    }
}
