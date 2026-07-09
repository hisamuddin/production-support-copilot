using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using ProductionSupportCopilot.Application.Documents;

namespace ProductionSupportCopilot.Infrastructure.Messaging;

public sealed class KafkaIngestionPublisher : IIngestionEventPublisher, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly IProducer<string, string> _producer;

    public KafkaIngestionPublisher(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3
        }).Build();
    }

    public async Task PublishAsync(IngestDocumentCommand command, CancellationToken cancellationToken = default)
    {
        await _producer.ProduceAsync(_options.IngestionTopic, new Message<string, string>
        {
            Key = command.TenantKey + ":" + command.ExternalId,
            Value = JsonSerializer.Serialize(command),
            Headers = new Headers { { "event-id", System.Text.Encoding.UTF8.GetBytes(command.EventId) } }
        }, cancellationToken);
    }

    public void Dispose() { _producer.Flush(TimeSpan.FromSeconds(3)); _producer.Dispose(); }
}
