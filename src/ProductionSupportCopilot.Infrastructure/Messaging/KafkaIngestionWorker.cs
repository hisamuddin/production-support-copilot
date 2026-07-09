using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductionSupportCopilot.Application.Documents;

namespace ProductionSupportCopilot.Infrastructure.Messaging;

public sealed class KafkaIngestionWorker(IServiceScopeFactory scopeFactory, IOptions<KafkaOptions> options, ILogger<KafkaIngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = settings.BootstrapServers, EnableIdempotence = true }).Build();
        consumer.Subscribe(settings.IngestionTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = consumer.Consume(stoppingToken);
                var command = JsonSerializer.Deserialize<IngestDocumentCommand>(message.Message.Value) ?? throw new JsonException("Empty ingestion command.");
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDocumentIngestionService>();
                var result = await service.IngestAsync(command, stoppingToken);
                if (result.Error is not null)
                {
                    await producer.ProduceAsync(settings.DeadLetterTopic, new Message<string, string>
                    {
                        Key = message.Message.Key,
                        Value = JsonSerializer.Serialize(new { original = command, result.Error, sourceTopic = settings.IngestionTopic, message.Offset.Value })
                    }, stoppingToken);
                }
                consumer.Commit(message);
            }
            catch (ConsumeException ex) { logger.LogError(ex, "Kafka consume failed; worker will retry."); await Task.Delay(1000, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Ingestion message failed before commit."); await Task.Delay(500, stoppingToken); }
        }
    }
}
