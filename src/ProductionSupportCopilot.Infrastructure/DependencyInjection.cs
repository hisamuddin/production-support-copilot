using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Application.Embeddings;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure.Documents;
using ProductionSupportCopilot.Infrastructure.Embeddings;
using ProductionSupportCopilot.Infrastructure.Persistence;
using ProductionSupportCopilot.Infrastructure.Retrieval;

namespace ProductionSupportCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SupportCopilot")
            ?? throw new InvalidOperationException("Connection string 'SupportCopilot' is required.");

        services.AddDbContext<SupportCopilotDbContext>(options =>
            options.UseNpgsql(connectionString, postgres => postgres.UseVector()));
        services.AddSingleton<IDocumentChunker, DeterministicDocumentChunker>();
        services.AddScoped<DevelopmentDataSeeder>();
        services.AddScoped<DocumentEmbeddingBackfillService>();
        services.AddScoped<IDocumentSearchService, PostgresDocumentSearchService>();
        services.AddScoped<ISemanticDocumentSearchService, PostgresSemanticDocumentSearchService>();
        services.AddScoped<IHybridDocumentSearchService, ReciprocalRankFusionSearchService>();

        var embeddingServiceUrl = configuration["EmbeddingService:BaseUrl"]
            ?? throw new InvalidOperationException("Embedding service base URL is required.");

        services.AddHttpClient<IEmbeddingProvider, HttpEmbeddingProvider>(client =>
        {
            client.BaseAddress = new Uri(embeddingServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
