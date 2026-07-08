namespace ProductionSupportCopilot.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string IngestionTopic { get; set; } = "support.document.ingestion.v1";
    public string DeadLetterTopic { get; set; } = "support.document.ingestion.dlq.v1";
    public string ConsumerGroup { get; set; } = "support-copilot-indexer-v1";
}
