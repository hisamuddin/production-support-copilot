using Microsoft.EntityFrameworkCore;
using ProductionSupportCopilot.Api.Middleware;
using ProductionSupportCopilot.Application.Copilot;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure;
using ProductionSupportCopilot.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Production Support Copilot API",
        Version = "v1",
        Description = "Citation-grounded support copilot for event-driven .NET services."
    });
});
builder.Services.AddSingleton<ICopilotQueryService, PlaceholderCopilotQueryService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("Database:InitializeDevelopmentData"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SupportCopilotDbContext>();
    await dbContext.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Production Support Copilot API v1");
        options.DocumentTitle = "Production Support Copilot API";
    });
}

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapHealthChecks("/health");

app.MapGet("/api/v1/retrieval/search", async (
    string tenantId,
    string query,
    int? limit,
    IDocumentSearchService searchService,
    CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(tenantId, out var parsedTenantId) || parsedTenantId == Guid.Empty)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(tenantId)] = ["A valid tenant ID is required."]
        });
    }

    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(query)] = ["Search query is required."]
        });
    }

    var requestedLimit = Math.Clamp(limit ?? 5, 1, 20);
    var results = await searchService.SearchAsync(parsedTenantId, query, requestedLimit, cancellationToken);
    return Results.Ok(new { query, results });
})
.WithName("SearchDocuments")
.WithTags("Retrieval")
.Produces(StatusCodes.Status200OK)
.ProducesValidationProblem();

app.MapPost("/api/v1/copilot/query", async (
    CopilotQueryRequest request,
    ICopilotQueryService service,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.Question)] = ["Question is required."]
        });
    }

    var response = await service.QueryAsync(request, cancellationToken);
    return Results.Ok(response);
})
.WithName("QueryCopilot")
.WithTags("Copilot")
.Produces<CopilotQueryResponse>(StatusCodes.Status200OK)
.ProducesValidationProblem();

app.Run();

public partial class Program;
