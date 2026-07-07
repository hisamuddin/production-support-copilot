namespace ProductionSupportCopilot.Application.Documents;

public interface IDocumentChunker
{
    IReadOnlyList<TextChunk> Chunk(string content, ChunkingOptions? options = null);
}

public sealed record TextChunk(int Sequence, string Content, int StartOffset, int EndOffset);

public sealed record ChunkingOptions(int MaximumCharacters = 1_200, int OverlapCharacters = 150)
{
    public void Validate()
    {
        if (MaximumCharacters < 100)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumCharacters), "Maximum characters must be at least 100.");
        }

        if (OverlapCharacters < 0 || OverlapCharacters >= MaximumCharacters)
        {
            throw new ArgumentOutOfRangeException(nameof(OverlapCharacters), "Overlap must be non-negative and smaller than the chunk size.");
        }
    }
}
