# Production Support Copilot

A production-minded AI support assistant for event-driven .NET services. It will ingest runbooks, technical documents, tickets, and incidents; answer questions with citations; inspect a mock service through controlled tools; draft incident summaries; and require approval for consequential actions.

## Current milestone

Phase 0 vertical skeleton:

- ASP.NET Core API targeting .NET 8
- modular Domain/Application/Infrastructure/API boundaries
- health endpoint
- versioned copilot query endpoint
- correlation ID and structured logging scope
- safe abstention while retrieval is unavailable
- unit and HTTP integration tests
- CI build/test workflow

No LLM is connected yet. This is intentional: retrieval and grounding will be measured before generated answers are introduced.

## Run locally

```powershell
docker-compose -f compose.yaml up -d postgres
dotnet restore ProductionSupportCopilot.sln --configfile NuGet.Config
dotnet run --project src/ProductionSupportCopilot.Api
```

Then call:

```http
GET /health

POST /api/v1/copilot/query
Content-Type: application/json

{
  "question": "Why are booking updates delayed?",
  "tenantId": "demo-tenant"
}
```

The current response intentionally has `isGrounded: false`, no citations, and status `retrieval_not_configured`.

Lexical retrieval is available at:

```http
GET /api/v1/retrieval/search?tenantId=10000000-0000-0000-0000-000000000001&query=consumer%20lag&limit=5
```

In Development, database migrations and three demo runbooks are initialized idempotently. Search results include document/chunk citation metadata and PostgreSQL full-text ranking scores. No LLM is used yet.

In the Development environment, Swagger UI is available at:

```text
http://localhost:5051/swagger
```

## Test

```powershell
dotnet test ProductionSupportCopilot.sln
```

## Design

- [Product requirements](docs/product-requirements.md)
- [ADR 0001: Modular monolith](docs/decisions/0001-modular-monolith.md)
- [Hinglish learning and decision journal](PROJECT_LEARNING_JOURNAL_HINGLISH.md)

## Next milestone

PostgreSQL/pgvector, EF Core migrations, deterministic development seeding, and tenant-filtered lexical retrieval are working. Next, build a golden retrieval dataset, measure Recall@K, add a generated full-text index for scale, and then introduce embeddings for semantic/hybrid retrieval.
