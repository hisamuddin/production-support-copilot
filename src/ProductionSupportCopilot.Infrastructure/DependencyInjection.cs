using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Application.Workflow;
using ProductionSupportCopilot.Infrastructure.Documents;
using ProductionSupportCopilot.Infrastructure.Embeddings;
using ProductionSupportCopilot.Infrastructure.Messaging;
using ProductionSupportCopilot.Infrastructure.Persistence;
using ProductionSupportCopilot.Infrastructure.Retrieval;
using ProductionSupportCopilot.Infrastructure.Workflow;

namespace ProductionSupportCopilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Postgres") ?? "Host=localhost;Port=5432;Database=support_copilot;Username=support;Password=support_dev_password";
        services.AddDbContext<SupportCopilotDbContext>(options => options.UseNpgsql(connection, npgsql => npgsql.UseVector()));
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IHybridRetriever, PostgresHybridRetriever>();
        services.AddScoped<IApprovalService, ApprovalService>();

        var useHttpEmbedding = configuration.GetValue("Embedding:UseHttp", false);
        if (useHttpEmbedding)
        {
            services.AddHttpClient<IEmbeddingProvider, HttpEmbeddingProvider>(client =>
            {
                client.BaseAddress = new Uri(configuration["Embedding:BaseUrl"] ?? "http://localhost:8001");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else services.AddSingleton<IEmbeddingProvider, DeterministicEmbeddingProvider>();

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        var kafkaEnabled = configuration.GetValue("Kafka:Enabled", false);
        if (kafkaEnabled)
        {
            services.AddSingleton<IIngestionEventPublisher, KafkaIngestionPublisher>();
            services.AddHostedService<KafkaIngestionWorker>();
        }
        else services.AddScoped<IIngestionEventPublisher, DirectIngestionPublisher>();

        return services;
    }
}
