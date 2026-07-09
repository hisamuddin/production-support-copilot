# Poison Event and DLQ Recovery

## Definition

A poison event repeatedly fails deterministic processing because its schema, required data, or business state is invalid. Retrying it without a limit can block useful work and create excess load.

## Safe handling

1. Capture the event ID, topic, partition, offset, failure reason, and correlation ID.
2. Confirm that transient dependency failures have been ruled out.
3. Move the event to the dead-letter topic after the configured retry budget is exhausted.
4. Redact secrets or personal information before sharing the payload for investigation.
5. Fix the producer, data, schema compatibility, or consumer logic before replay.

## Replay control

DLQ replay is a consequential action. Validate idempotency, estimate the number of affected events, select a bounded batch, obtain authorized approval, monitor outcomes, and stop if error rate or downstream load crosses the safety threshold.
