# Azure deployment mapping

This repository does not contain credentials or perform an external deployment automatically. Recommended portfolio mapping:

- Azure Container Apps or AKS: API and embedding containers.
- Azure Database for PostgreSQL Flexible Server: enable `vector` extension.
- Azure Event Hubs Kafka endpoint or Confluent Cloud: ingestion topics and consumer group.
- Azure Container Registry: versioned images.
- Azure Key Vault + managed identity: PostgreSQL/JWT/provider secrets.
- Azure Monitor/OpenTelemetry collector: traces, logs and metrics.

## Safe deployment sequence

1. Provision resource group, network, managed Postgres and broker.
2. Enable pgvector and run EF migrations as a one-shot release job.
3. Build immutable image tags and scan them.
4. Store secrets in Key Vault; never commit them or place them in ConfigMap.
5. Deploy embedding service, then API with readiness/liveness probes.
6. Run smoke tests, tenant-isolation tests and the stable evaluation subset.
7. Canary traffic, inspect quality/latency/cost, then promote or roll back.

The Kubernetes manifest in `deploy/kubernetes/support-copilot.yaml` is a provider-neutral starting point. Replace image registry, managed Kafka and secret references before applying.
