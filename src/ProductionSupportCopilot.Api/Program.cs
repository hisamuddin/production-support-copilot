using ProductionSupportCopilot.Api.Middleware;
using ProductionSupportCopilot.Application.Copilot;

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

var app = builder.Build();

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
