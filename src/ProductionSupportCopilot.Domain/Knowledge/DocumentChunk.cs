using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;
using ProductionSupportCopilot.Domain.Common;

namespace ProductionSupportCopilot.Domain.Knowledge;

public sealed class DocumentChunk : Entity
{
    private DocumentChunk() { }

    public DocumentChunk(Guid documentId, Guid tenantId, int ordinal, string content, int startCharacter, int endCharacter)
    {
        DocumentId = documentId;
        TenantId = tenantId;
        Ordinal = ordinal;
        Content = content;
        StartCharacter = startCharacter;
        EndCharacter = endCharacter;
        TokenEstimate = Math.Max(1, content.Length / 4);
    }

    public Guid DocumentId { get; private set; }
    public SupportDocument Document { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public int Ordinal { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int StartCharacter { get; private set; }
    public int EndCharacter { get; private set; }
    public int TokenEstimate { get; private set; }

    [Column(TypeName = "vector(384)")]
    public Vector? Embedding { get; private set; }

    public void SetEmbedding(float[] values)
    {
        if (values.Length != 384) throw new ArgumentException("Embedding must contain 384 dimensions.", nameof(values));
        Embedding = new Vector(values);
        MarkUpdated();
    }
}
