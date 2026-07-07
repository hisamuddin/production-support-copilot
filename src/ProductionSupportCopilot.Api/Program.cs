using ProductionSupportCopilot.Api.Middleware;
using ProductionSupportCopilot.Application.Copilot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<ICopilotQueryService, PlaceholderCopilotQueryService>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.MapHealthChecks("/health");

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
});

app.Run();

public partial class Program;
