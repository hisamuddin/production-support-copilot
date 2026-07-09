# Production Support Copilot

A production-minded portfolio project for senior .NET engineers moving into Applied AI Engineering / AI Architecture.

The product is a support copilot for event-driven .NET services. It ingests runbooks/tickets, chunks and embeds documents, retrieves tenant-authorized evidence, answers with citations, calls safe mock tools, and pauses consequential actions for human approval.

## What is implemented

- ASP.NET Core .NET 8 modular monolith: API, Application, Domain, Infrastructure.
- PostgreSQL + EF Core + pgvector for tenants, documents, chunks, ingestion jobs and approvals.
- Deterministic chunking, metadata capture, idempotent ingestion and seeded demo runbooks.
- Hybrid retrieval: lexical term matching + pgvector semantic search + Reciprocal Rank Fusion.
- LLM abstraction with a no-key extractive demo provider and optional Ollama provider with fallback.
- Prompt-injection guard, untrusted document boundaries and citation allow-list validation.
- Kafka-compatible Redpanda ingestion path with manual commits and DLQ publication.
- Tool calling through an explicit registry: health, consumer lag, booking status and approval-gated DLQ replay.
- Explicit workflow state machine for auditable agent-style behavior.
- JWT demo auth, tenant enforcement, rate limiting, correlation IDs and SSE streaming.
- Request latency + token/cost dashboard and OpenTelemetry hooks.
- Python FastAPI embedding service and Python evaluation CLI where the AI ecosystem is useful.
- Docker Compose, Kubernetes/Azure deployment notes, architecture docs, Obsidian notes and PDF handbook.

## Quick start: full demo stack

Prerequisite: Docker Desktop. First run can be slow because the Python image/model cache has to build/download.

```powershell
docker compose up --build
# If your Docker CLI does not support the space form:
docker-compose -f compose.yaml up --build
```

Open Swagger:

```text
http://localhost:5051/swagger
```

Demo flow:

1. `POST /api/v1/auth/demo-token`
2. Copy `accessToken`
3. Click Swagger **Authorize**
4. Enter `Bearer <token>`
5. Try:
   - `POST /api/v1/retrieval/hybrid-search`
   - `POST /api/v1/copilot/query`
   - `POST /api/v1/copilot/query/stream`
   - `POST /api/v1/workflows/run`
   - `GET /api/v1/tools`
   - `POST /api/v1/tools/replay-dead-letter/execute`
   - `POST /api/v1/approvals/{approvalId}/decision`
   - `GET /api/v1/observability/dashboard`

Useful sample question:

```json
{
  "question": "Why are booking status updates delayed and what should I check?",
  "tenantId": "demo-tenant"
}
```

## Lightweight local mode

Run only PostgreSQL in Docker, then run the API from your IDE/terminal:

```powershell
docker compose up -d postgres
# Or:
docker-compose -f compose.yaml up -d postgres
dotnet restore ProductionSupportCopilot.sln --configfile NuGet.Config
dotnet run --project src/ProductionSupportCopilot.Api
```

In Development, the API applies EF migrations and seeds demo runbooks when `Database:Initialize=true`.

## Test and evaluate

```powershell
dotnet build ProductionSupportCopilot.sln
dotnet test ProductionSupportCopilot.sln
python evaluation/evaluate.py --base-url http://localhost:5051
```

The evaluation dataset covers answerable questions, insufficient evidence, prompt-injection attempts, tenant boundaries, stale-document behavior, tool use and approvals. Treat local toy-corpus scores as learning signals, not production proof.

## pgAdmin connection

- Host: `localhost`
- Port: `5432`
- Database: `support_copilot`
- User: `support`
- Password: `support_dev_password`

Tables:

- `tenants`
- `support_documents`
- `document_chunks`
- `ingestion_jobs`
- `approval_requests`

## Documentation map

- [AI restart context](AI_CONTEXT.md)
- [Hinglish learning journal](PROJECT_LEARNING_JOURNAL_HINGLISH.md)
- [Obsidian knowledge graph](docs/obsidian/Home.md)
- [Architecture and capacity design](docs/architecture-design.md)
- [Product requirements](docs/product-requirements.md)
- [ADR 0001: Modular monolith](docs/decisions/0001-modular-monolith.md)
- [Beginner-friendly PDF handbook](output/pdf/Production_Support_Copilot_Hinglish_Handbook.pdf)
- [Kubernetes manifest](deploy/kubernetes/support-copilot.yaml)
- [Azure deployment notes](deploy/azure/README.md)

## Honest limitations

- The default answer generator is deterministic/extractive so the demo needs no paid API key.
- Ollama is optional and must be installed separately for local generative inference.
- Tools and operational data are synthetic; this project does not touch a real employer system.
- The dashboard is in-memory; production should export OpenTelemetry to a durable backend.
- Large-scale retrieval still needs measured PostgreSQL full-text indexes, ANN tuning and load tests.
