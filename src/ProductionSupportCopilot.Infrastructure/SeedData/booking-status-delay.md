# Booking Status Update Delay Investigation

## Scope

Use this runbook when a booking was accepted but assignment, tracking, or notification status is not visible within the expected interval.

## Investigation order

1. Locate the booking by its correlation ID and confirm the Booking API accepted it.
2. Verify that the booking-created event was published successfully.
3. Inspect assignment-service consumer lag and recent processing errors.
4. Confirm the status-updated event reached the tracking and notification consumers.
5. Check downstream API and database latency.

## Interpretation

If publication failed, investigate the producer/outbox path. If publication succeeded but lag is high, follow the Kafka consumer lag runbook. If one event repeatedly fails, follow the poison-event and DLQ runbook. Do not claim the live cause from documentation alone; inspect current service and Kafka metrics.
