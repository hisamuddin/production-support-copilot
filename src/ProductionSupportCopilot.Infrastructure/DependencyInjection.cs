using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductionSupportCopilot.Application.Documents;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure.Documents;
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

        services.AddDbContext<SupportCopilotDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IDocumentChunker, DeterministicDocumentChunker>();
        services.AddScoped<DevelopmentDataSeeder>();
        services.AddScoped<IDocumentSearchService, PostgresDocumentSearchService>();

        return services;
    }
}
