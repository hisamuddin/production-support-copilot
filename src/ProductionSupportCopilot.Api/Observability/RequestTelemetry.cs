using System.Collections.Concurrent;
using System.Diagnostics;
using ProductionSupportCopilot.Application.Observability;

namespace ProductionSupportCopilot.Api.Observability;

public sealed record RequestMetric(string Path, int StatusCode, long DurationMs, DateTimeOffset AtUtc);

public sealed class RequestTelemetry : IAiUsageRecorder
{
    private readonly ConcurrentQueue<RequestMetric> _recent = new();
    private readonly ConcurrentQueue<AiUsageEvent> _aiUsage = new();
    public void Record(RequestMetric metric)
    {
        _recent.Enqueue(metric);
        while (_recent.Count > 500 && _recent.TryDequeue(out _)) { }
    }

    public void Record(AiUsageEvent usage)
    {
        _aiUsage.Enqueue(usage);
        while (_aiUsage.Count > 500 && _aiUsage.TryDequeue(out _)) { }
    }

    public object Snapshot()
    {
        var values = _recent.ToArray();
        var durations = values.Select(x => x.DurationMs).Order().ToArray();
        var ai = _aiUsage.ToArray();
        return new
        {
            requestCount = values.Length,
            errorCount = values.Count(x => x.StatusCode >= 500),
            p50LatencyMs = Percentile(durations, 0.50),
            p95LatencyMs = Percentile(durations, 0.95),
            aiUsage = new
            {
                calls = ai.Length,
                inputTokens = ai.Sum(x => x.InputTokens),
                outputTokens = ai.Sum(x => x.OutputTokens),
                estimatedCostUsd = ai.Sum(x => x.EstimatedCostUsd),
                models = ai.GroupBy(x => x.Model).ToDictionary(x => x.Key, x => x.Count())
            },
            latest = values.TakeLast(20)
        };
    }

    private static long Percentile(long[] values, double percentile) => values.Length == 0 ? 0 : values[(int)Math.Ceiling(percentile * values.Length) - 1];
}

public sealed class RequestTelemetryMiddleware(RequestDelegate next, RequestTelemetry telemetry)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try { await next(context); }
        finally { telemetry.Record(new RequestMetric(context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds, DateTimeOffset.UtcNow)); }
    }
}
