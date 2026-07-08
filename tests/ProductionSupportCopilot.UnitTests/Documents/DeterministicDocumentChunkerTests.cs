using ProductionSupportCopilot.Application.Documents;

namespace ProductionSupportCopilot.UnitTests.Documents;

public sealed class DeterministicDocumentChunkerTests
{
    [Fact]
    public void Chunk_LongText_ProducesOrderedOverlappingChunks()
    {
        var sut = new DeterministicDocumentChunker();
        var text = string.Join(" ", Enumerable.Repeat("Booking status consumer lag causes delayed updates.", 80));
        var result = sut.Chunk(text, 300, 50);
        Assert.True(result.Count > 2);
        Assert.Equal(Enumerable.Range(0, result.Count), result.Select(x => x.Ordinal));
        Assert.True(result[1].StartCharacter < result[0].EndCharacter);
    }

    [Fact]
    public void Chunk_SameInput_IsDeterministic()
    {
        var sut = new DeterministicDocumentChunker();
        var first = sut.Chunk("First paragraph.\n\nSecond paragraph with useful recovery instructions.", 200, 20);
        var second = sut.Chunk("First paragraph.\n\nSecond paragraph with useful recovery instructions.", 200, 20);
        Assert.Equal(first, second);
    }
}
