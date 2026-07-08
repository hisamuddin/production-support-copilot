# Production Support Copilot

A production-minded, multi-tenant support assistant for event-driven .NET services. It retrieves runbooks with citations, inspects a safe mock service through controlled tools, drafts grounded answers, and pauses consequential actions for human approval.

## Implemented architecture

- ASP.NET Core .NET 8 modular monolith: API, Application, Domain, Infrastructure
- EF Core + PostgreSQL + pgvector, initial migration and synthetic seed runbooks
- deterministic chunking; MiniLM embeddings through a small Python FastAPI service
- lexical + vector retrieval combined with Reciprocal Rank Fusion
- Kafka-compatible Redpanda ingestion, event idempotency, manual commits and DLQ
- LLM provider abstraction: no-key extractive demo and optional Ollama structured output with retry/fallback
- SSE streaming, explicit workflow state machine, read-only tools and approval-gated DLQ replay
- JWT tenant enforcement, rate limits, prompt-injection boundaries and citation allow-list
- correlation IDs, latency dashboard, golden evaluation dataset, unit/integration tests
- Docker Compose and GitHub Actions

## Run the full stack

Prerequisite: Docker Desktop with enough disk/RAM for the first Python/model image build.

```powershell
docker compose up --build
```

Open [Swagger](http://localhost:5051/swagger). Call `POST /api/v1/auth/demo-token`, copy `accessToken`, click **Authorize**, and enter `Bearer <token>`.

Suggested demo order:

1. `POST /api/v1/retrieval/hybrid-search`
2. `POST /api/v1/copilot/query`
3. `POST /api/v1/copilot/query/stream`
4. `POST /api/v1/workflows/run` with a current-health question
5. Run the workflow with “Replay the DLQ”, then decide the returned approval ID
6. Try a prompt-injection phrase and a mismatched tenant
7. Inspect `/api/v1/observability/dashboard`

## Lightweight developer mode

This uses local PostgreSQL, deterministic offline embeddings and direct ingestion; Kafka and the Python model are disabled by default.

```powershell
dotnet restore ProductionSupportCopilot.sln --configfile NuGet.Config
dotnet run --project src/ProductionSupportCopilot.Api
```

## Test and evaluate

```powershell
dotnet test ProductionSupportCopilot.sln
python evaluation/evaluate.py --base-url http://localhost:5051
```

The evaluation dataset covers answerable, unanswerable, injection, authorization, stale-document, tool and approval cases. Toy-corpus scores must not be presented as production evidence.

## Documentation

- [AI restart context](AI_CONTEXT.md)
- [Complete Hinglish learning journal](PROJECT_LEARNING_JOURNAL_HINGLISH.md)
- [Obsidian knowledge graph](docs/obsidian/Home.md)
- [Architecture and capacity design](docs/architecture-design.md)
- [Product requirements](docs/product-requirements.md)
- [Modular monolith decision](docs/decisions/0001-modular-monolith.md)
- [Beginner-friendly PDF handbook](output/pdf/Production_Support_Copilot_Hinglish_Handbook.pdf)

## Safe limitations

The tools and data are synthetic. No real employer/production system is accessed. Default answer generation is deterministic so the project runs without a paid key. Ollama is optional. The in-memory dashboard demonstrates metrics; a production deployment should export OpenTelemetry. Application-side lexical scoring is suitable for the seed corpus; larger deployments should use PostgreSQL full-text indexes and measured ANN tuning.
