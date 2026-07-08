using System.Text.Json;

namespace ProductionSupportCopilot.Application.Tools;

public sealed class ServiceHealthTool : ISupportTool
{
    public string Name => "get_service_health";
    public string Description => "Returns current mock health, latency and error rate for an allowed service.";
    public bool IsConsequential => false;
    public Task<ToolResult> ExecuteAsync(JsonElement arguments, ToolContext context, CancellationToken cancellationToken = default)
    {
        var service = arguments.TryGetProperty("service", out var value) ? value.GetString() : "booking-api";
        var data = new { service, status = "healthy", p95LatencyMs = 184, errorRatePercent = 0.3, observedAtUtc = DateTimeOffset.UtcNow };
        return Task.FromResult(new ToolResult(true, $"{service} is healthy in the mock environment.", data));
    }
}

public sealed class ConsumerLagTool : ISupportTool
{
    public string Name => "get_consumer_lag";
    public string Description => "Returns mock Kafka consumer lag without modifying the system.";
    public bool IsConsequential => false;
    public Task<ToolResult> ExecuteAsync(JsonElement arguments, ToolContext context, CancellationToken cancellationToken = default)
    {
        var group = arguments.TryGetProperty("consumerGroup", out var value) ? value.GetString() : "status-indexer";
        return Task.FromResult(new ToolResult(true, $"Consumer group {group} has mock lag 42.", new { consumerGroup = group, lag = 42, partitions = 3 }));
    }
}

public sealed class BookingStatusTool : ISupportTool
{
    public string Name => "get_booking_status";
    public string Description => "Returns a synthetic booking status; never calls a real employer system.";
    public bool IsConsequential => false;
    public Task<ToolResult> ExecuteAsync(JsonElement arguments, ToolContext context, CancellationToken cancellationToken = default)
    {
        var id = arguments.TryGetProperty("bookingId", out var value) ? value.GetString() : "demo-1001";
        return Task.FromResult(new ToolResult(true, $"Booking {id} is accepted and waiting for assignment.", new { bookingId = id, status = "accepted", updatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2) }));
    }
}
