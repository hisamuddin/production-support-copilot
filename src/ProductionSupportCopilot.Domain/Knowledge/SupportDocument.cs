using ProductionSupportCopilot.Domain.Common;

namespace ProductionSupportCopilot.Domain.Knowledge;

public sealed class SupportDocument : Entity
{
    private SupportDocument() { }

    public SupportDocument(Guid tenantId, string externalId, string title, string content, string sourceType, string classification = "internal")
    {
        TenantId = tenantId;
        ExternalId = Require(externalId, nameof(externalId));
        Title = Require(title, nameof(title));
        Content = Require(content, nameof(content));
        SourceType = Require(sourceType, nameof(sourceType));
        Classification = Require(classification, nameof(classification));
        ContentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Content)));
    }

    public Guid TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public string ExternalId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string ContentHash { get; private set; } = string.Empty;
    public string SourceType { get; private set; } = string.Empty;
    public string Classification { get; private set; } = "internal";
    public int Version { get; private set; } = 1;
    public ICollection<DocumentChunk> Chunks { get; private set; } = [];

    public void ReplaceContent(string content)
    {
        Content = Require(content, nameof(content));
        ContentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Content)));
        Version++;
        MarkUpdated();
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
}
