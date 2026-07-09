using ProductionSupportCopilot.Application.Documents;

namespace ProductionSupportCopilot.Infrastructure.Messaging;

public sealed class DirectIngestionPublisher(IDocumentIngestionService ingestion) : IIngestionEventPublisher
{
    public async Task PublishAsync(IngestDocumentCommand command, CancellationToken cancellationToken = default) =>
        _ = await ingestion.IngestAsync(command, cancellationToken);
}
