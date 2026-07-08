using System.Diagnostics;

namespace ProductionSupportCopilot.Application.Observability;

public sealed record AiUsageEvent(string Model, int InputTokens, int OutputTokens, decimal EstimatedCostUsd, long LatencyMs, DateTimeOffset AtUtc);

public interface IAiUsageRecorder { void Record(AiUsageEvent usage); }

public sealed class NullAiUsageRecorder : IAiUsageRecorder { public void Record(AiUsageEvent usage) { } }

public static class CopilotTelemetry
{
    public const string SourceName = "ProductionSupportCopilot";
    public static readonly ActivitySource Source = new(SourceName);
}
