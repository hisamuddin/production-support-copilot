using System.Data;
using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure.Persistence;

namespace ProductionSupportCopilot.Infrastructure.Retrieval;

public sealed class PostgresDocumentSearchService(SupportCopilotDbContext dbContext)
    : IDocumentSearchService
{
    public async Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        Guid tenantId,
        string query,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Search query is required.", nameof(query));
        if (limit is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 20.");

        const string sql = """
            SELECT
                c."Id" AS chunk_id,
                c."DocumentId" AS document_id,
                d."Title" AS document_title,
                c."Sequence" AS sequence,
                c."Content" AS content,
                ts_rank_cd(
                    c."SearchVector",
                    plainto_tsquery('english', @query)) AS score
            FROM document_chunks AS c
            INNER JOIN documents AS d ON d."Id" = c."DocumentId"
            WHERE c."TenantId" = @tenant_id
              AND c."SearchVector" @@ plainto_tsquery('english', @query)
            ORDER BY score DESC, c."DocumentId", c."Sequence"
            LIMIT @limit;
            """;

        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            AddParameter(command, "query", query.Trim());
            AddParameter(command, "tenant_id", tenantId);
            AddParameter(command, "limit", limit);

            var results = new List<DocumentSearchResult>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new DocumentSearchResult(
                    reader.GetGuid(reader.GetOrdinal("chunk_id")),
                    reader.GetGuid(reader.GetOrdinal("document_id")),
                    reader.GetString(reader.GetOrdinal("document_title")),
                    reader.GetInt32(reader.GetOrdinal("sequence")),
                    reader.GetString(reader.GetOrdinal("content")),
                    reader.GetFloat(reader.GetOrdinal("score"))));
            }

            return results;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
