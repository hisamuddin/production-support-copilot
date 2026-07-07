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

Tenant/document/chunk models, deterministic chunking, and the PostgreSQL Compose definition are now present. Next, start Docker Desktop, connect EF Core to PostgreSQL, seed runbooks, and implement lexical retrieval. The API will return ranked passages with citations before an LLM is allowed to generate an answer.
