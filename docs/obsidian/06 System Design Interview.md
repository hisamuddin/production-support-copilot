# System Design Interview

Related: [[01 Architecture Map]], [[04 Security Reliability and Evaluation]], [[05 Runbook and Demo]].

## Capacity example (assumptions, not measured production facts)

Assume 100 tenants, 10,000 documents each, average 20 chunks = 20 million chunks. A 384D float32 vector is about 1.5 KB raw, roughly 30 GB raw vectors before table/index overhead. If 10 QPS and top-50 candidates, exact scan is no longer acceptable; partition/tenant filters plus HNSW and connection pooling become relevant. Embedding ingestion needs backpressure and autoscaled consumers. LLM concurrency is bounded by provider rate limits and cost budgets.

## Interview tradeoffs

- Consistency: document transaction completes before job complete; Kafka gives at-least-once.
- Availability: LLM failure falls back; DB failure prevents grounded answer rather than hallucinating.
- Latency: parallel lexical/vector candidates can reduce time; reranker adds quality and latency.
- Cost: small embedding model, Postgres consolidation, prompt context cap, cached/deduplicated ingestion.
- Security: tenant filter before context, role-gated tools, approval, secrets outside source.
- Scale: start modular monolith; extract ingestion/model gateway on evidence.

## Failure questions

If embedding service down: ingestion fails/retries/DLQ; existing indexed docs remain searchable. If LLM down: deterministic cited fallback. If duplicate Kafka message: unique EventId returns duplicate result. If malicious document: trust boundary, no tool authority, citation allow-list, tests. If approval service restarts: persisted request survives; current implementation decision endpoint completes simulated action only, while production needs durable continuation/outbox.
