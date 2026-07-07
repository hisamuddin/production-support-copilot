# Kafka Consumer Lag Troubleshooting

## Symptoms

Booking status updates arrive late, the consumer lag metric rises continuously, or notification events are delayed while producers remain healthy.

## Safe investigation

1. Confirm the affected consumer group and topic partition.
2. Compare current lag with the normal baseline and record the observation time.
3. Check consumer pod health, restart count, CPU, memory, and recent error logs.
4. Verify whether one partition is disproportionately behind, which may indicate a hot key or poison event.
5. Check downstream database and API latency before increasing consumer replicas.

## Important caution

Do not reset offsets or replay events during initial diagnosis. Scaling consumers helps only when partition count and downstream capacity allow useful parallelism. Any offset change requires an impact review and authorized approval.
