using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Domain.Ingestion;
using ProductionSupportCopilot.Domain.Knowledge;
using ProductionSupportCopilot.Infrastructure.Persistence;

namespace ProductionSupportCopilot.Infrastructure.Documents;

public sealed class DocumentIngestionService(SupportCopilotDbContext db, IDocumentChunker chunker, IEmbeddingProvider embeddings) : IDocumentIngestionService
{
    public async Task<IngestionResult> IngestAsync(IngestDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var existingJob = await db.IngestionJobs.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == command.EventId, cancellationToken);
        if (existingJob is not null) return new IngestionResult(existingJob.Id, existingJob.Status.ToString(), 0, true, existingJob.LastError);

        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Key == command.TenantKey, cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant(command.TenantKey, command.TenantKey.Replace('-', ' '));
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
        }

        var job = new IngestionJob(tenant.Id, command.EventId, command.ExternalId);
        db.IngestionJobs.Add(job);
        job.Start();
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var document = await db.Documents.Include(x => x.Chunks).SingleOrDefaultAsync(x => x.TenantId == tenant.Id && x.ExternalId == command.ExternalId, cancellationToken);
            if (document is null)
            {
                document = new SupportDocument(tenant.Id, command.ExternalId, command.Title, command.Content, command.SourceType, command.Classification);
                db.Documents.Add(document);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                document.ReplaceContent(command.Content);
                db.DocumentChunks.RemoveRange(document.Chunks);
                await db.SaveChangesAsync(cancellationToken);
            }

            var drafts = chunker.Chunk(command.Content);
            foreach (var draft in drafts)
            {
                var chunk = new DocumentChunk(document.Id, tenant.Id, draft.Ordinal, draft.Content, draft.StartCharacter, draft.EndCharacter);
                chunk.SetEmbedding(await embeddings.EmbedAsync(draft.Content, cancellationToken));
                db.DocumentChunks.Add(chunk);
            }
            job.Complete();
            await db.SaveChangesAsync(cancellationToken);
            return new IngestionResult(job.Id, job.Status.ToString(), drafts.Count, false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            job.Fail(ex.Message, job.Attempts >= 3);
            await db.SaveChangesAsync(cancellationToken);
            return new IngestionResult(job.Id, job.Status.ToString(), 0, false, ex.Message);
        }
    }
}
