# AI Context - Read This First

This file is the restart contract for any AI or developer entering without conversation history.

## Mission

Build and teach a production-minded **Production Support Copilot for event-driven .NET services**. The learner is a senior .NET engineer moving toward Applied AI Engineer / AI Architect roles. Explain complex ideas in Hinglish, preserve production tradeoffs, and never claim an unverified feature or metric.

## Current verified implementation

Last local verification: 2026-07-09. `dotnet build ProductionSupportCopilot.sln` completed with 0 warnings/0 errors. `dotnet test ProductionSupportCopilot.sln` passed 15/15 tests.

- .NET 8 modular monolith: API, Application, Domain, Infrastructure.
- JWT demo authentication, tenant claim enforcement, fixed-window rate limiting.
- EF Core + PostgreSQL + pgvector schema and initial migration.
- Deterministic document chunking and idempotent ingestion jobs.
- Optional Kafka/Redpanda ingestion with manual commits and DLQ publication.
- Optional Python sentence-transformer service; deterministic offline embedding fallback.
- Tenant-filtered lexical + semantic retrieval fused by Reciprocal Rank Fusion.
- Grounded answer service with citations and safe abstention.
- Provider abstraction, deterministic extractive provider, optional Ollama structured-output provider, retries/fallback.
- Read-only mock tools plus approval-gated simulated DLQ replay.
- Explicit workflow state machine and trace.
- Prompt-injection phrase guard and untrusted-document boundaries.
- OpenTelemetry ASP.NET/HTTP/RAG tracing hooks, request latency plus token/cost dashboard, correlation IDs and streaming SSE endpoint.
- Golden evaluation dataset and Python evaluation CLI.
- Unit/integration tests, Docker Compose, GitHub Actions.
- Merge-conflict audit is clean; old duplicate document-store model files were removed in favor of the current `Domain.Knowledge` model.

## Truthful limitations

- Default LLM is deterministic/extractive so the demo needs no paid key. Ollama must be installed/configured for generative local inference.
- Default direct `dotnet run` uses deterministic embeddings; Compose enables the real MiniLM embedding service.
- Retrieval lexical scoring is application-side term frequency for the small corpus; production scale should move lexical ranking to PostgreSQL `tsvector`/GIN.
- Kafka path is available only when `Kafka:Enabled=true`; direct mode exists for simpler local learning.
- Tool outputs and consequential execution are simulated. Never connect to a real employer or production environment.
- Request dashboard is in-memory and bounded; production should export OpenTelemetry to a durable backend.

## Start here

1. Read `PROJECT_LEARNING_JOURNAL_HINGLISH.md`.
2. Open `docs/obsidian/Home.md` as an Obsidian vault entry.
3. Run `dotnet test ProductionSupportCopilot.sln`.
4. Run full stack with `docker compose up --build` or `docker-compose -f compose.yaml up --build` depending on the local Docker CLI.
5. Open `http://localhost:5051/swagger` and create a demo token.
6. If only PostgreSQL is needed, run `docker compose up -d postgres` or `docker-compose -f compose.yaml up -d postgres`, then `dotnet run --project src/ProductionSupportCopilot.Api`.

## Rules for future changes

- Preserve tenant authorization before retrieval and before context reaches an LLM.
- Consequential tools must stop at approval; no autonomous production action.
- Keep Python limited to model/evaluation ecosystem needs.
- Add or update tests, journal, class catalog, architecture doc and evaluation cases with every material feature.
- Mark measured numbers with date/environment/dataset; do not turn toy-corpus results into production claims.
- Check `git status`, conflict markers, build, tests and Docker health before declaring completion.
