namespace ProductionSupportCopilot.Application.Retrieval;

public interface ISemanticDocumentSearchService
{
    Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        Guid tenantId,
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default);
}
