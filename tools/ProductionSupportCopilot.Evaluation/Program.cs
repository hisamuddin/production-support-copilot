using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using ProductionSupportCopilot.Application.Retrieval;
using ProductionSupportCopilot.Infrastructure.Embeddings;
using ProductionSupportCopilot.Infrastructure.Persistence;
using ProductionSupportCopilot.Infrastructure.Retrieval;

const string defaultConnectionString =
    "Host=localhost;Port=5432;Database=support_copilot;Username=support_copilot;Password=local_development_only";

var connectionString = Environment.GetEnvironmentVariable("SUPPORT_COPILOT_DB")
    ?? defaultConnectionString;
var embeddingServiceUrl = Environment.GetEnvironmentVariable("SUPPORT_COPILOT_EMBEDDINGS")
    ?? "http://localhost:8001/";
var mode = args.FirstOrDefault()?.ToLowerInvariant() ?? "lexical";

if (mode is not ("lexical" or "semantic"))
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/ProductionSupportCopilot.Evaluation -- [lexical|semantic]");
    return 2;
}

var datasetPath = Path.Combine(AppContext.BaseDirectory, "golden-retrieval.json");

var json = await File.ReadAllTextAsync(datasetPath);
var dataset = JsonSerializer.Deserialize<List<GoldenRetrievalCase>>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? throw new InvalidOperationException("Golden retrieval dataset is empty or invalid.");

var options = new DbContextOptionsBuilder<SupportCopilotDbContext>()
    .UseNpgsql(connectionString, postgres => postgres.UseVector())
    .Options;

await using var dbContext = new SupportCopilotDbContext(options);
var lexicalSearch = new PostgresDocumentSearchService(dbContext);
using var embeddingHttpClient = new HttpClient
{
    BaseAddress = new Uri(embeddingServiceUrl),
    Timeout = TimeSpan.FromSeconds(30)
};
var embeddingProvider = new HttpEmbeddingProvider(embeddingHttpClient);
var semanticSearch = new PostgresSemanticDocumentSearchService(dbContext, embeddingProvider);

Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(GoldenRetrievalCase testCase) =>
    mode == "semantic"
        ? semanticSearch.SearchAsync(testCase.TenantId, testCase.Query, 5)
        : lexicalSearch.SearchAsync(testCase.TenantId, testCase.Query, 5);

var top1Hits = 0;
var top5Hits = 0;
var reciprocalRankTotal = 0d;

Console.WriteLine("Production Support Copilot - {0} Retrieval Evaluation", char.ToUpperInvariant(mode[0]) + mode[1..]);
Console.WriteLine(new string('-', 94));
Console.WriteLine("{0,-3} {1,-42} {2,-7} {3,-7} {4}", "#", "Query", "Top@1", "Top@5", "First relevant rank");
Console.WriteLine(new string('-', 94));

for (var index = 0; index < dataset.Count; index++)
{
    var testCase = dataset[index];
    var results = await SearchAsync(testCase);
    var rank = results
        .Select((result, resultIndex) => new { result.DocumentId, Rank = resultIndex + 1 })
        .Where(item => testCase.ExpectedDocumentIds.Contains(item.DocumentId))
        .Select(item => (int?)item.Rank)
        .FirstOrDefault();

    var hitAt1 = rank == 1;
    var hitAt5 = rank is >= 1 and <= 5;

    if (hitAt1) top1Hits++;
    if (hitAt5) top5Hits++;
    if (rank.HasValue) reciprocalRankTotal += 1d / rank.Value;

    Console.WriteLine(
        "{0,-3} {1,-42} {2,-7} {3,-7} {4}",
        index + 1,
        Truncate(testCase.Query, 42),
        hitAt1 ? "PASS" : "FAIL",
        hitAt5 ? "PASS" : "FAIL",
        rank?.ToString() ?? "not found");
}

var recallAt1 = (double)top1Hits / dataset.Count;
var recallAt5 = (double)top5Hits / dataset.Count;
var meanReciprocalRank = reciprocalRankTotal / dataset.Count;

Console.WriteLine(new string('-', 94));
Console.WriteLine("Cases:      {0}", dataset.Count);
Console.WriteLine("Recall@1:   {0:P1}", recallAt1);
Console.WriteLine("Recall@5:   {0:P1}", recallAt5);
Console.WriteLine("MRR:        {0:F3}", meanReciprocalRank);

const double minimumRecallAt5 = 0.70;
if (recallAt5 < minimumRecallAt5)
{
    Console.Error.WriteLine("FAIL: Recall@5 {0:P1} is below the {1:P0} quality gate.", recallAt5, minimumRecallAt5);
    return 1;
}

Console.WriteLine("PASS: Recall@5 meets the {0:P0} baseline quality gate.", minimumRecallAt5);
return 0;

static string Truncate(string value, int maximumLength) =>
    value.Length <= maximumLength ? value : value[..(maximumLength - 3)] + "...";

internal sealed record GoldenRetrievalCase(
    string Id,
    Guid TenantId,
    string Query,
    IReadOnlyCollection<Guid> ExpectedDocumentIds,
    string Category);
