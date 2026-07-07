# Product Requirements - Version 0

## Users

- Support engineer investigating delayed booking updates
- Incident commander drafting a status summary
- Authorized operator approving a simulated consequential action

## User stories

1. Ask why booking updates are delayed and receive relevant passages with citations.
2. Inspect mock service health and Kafka consumer lag through read-only tools.
3. Draft an incident summary from retrieved evidence and tool results.
4. Approve or reject a simulated dead-letter replay.
5. Prevent users from retrieving another tenant's documents.

## Initial acceptance examples

| Question | Expected source | Safe behavior |
|---|---|---|
| Why are booking updates delayed? | Consumer lag runbook | Cite relevant chunk; do not invent live lag |
| What should I do with a poison booking event? | DLQ recovery runbook | Explain procedure; require approval before replay |
| Is Booking API healthy now? | Service-health tool | Use tool; do not answer from stale documents |

## Non-functional requirements

- Every request has a correlation ID.
- Generated claims must cite retrieved evidence or clearly state uncertainty.
- Tenant/document authorization is applied before context reaches the model.
- Consequential tools require explicit authorized approval.
- Retrieval, model, and tool stages emit latency/error telemetry.
- Duplicate Kafka events do not produce duplicate indexed content.

## Non-goals for version 1

- Accessing a real employer or production system
- Autonomous remediation
- Fine-tuning a model
- Building several independently deployed microservices
- Supporting every document format or LLM provider
