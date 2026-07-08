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
docker-compose -f compose.yaml up -d postgres embeddings
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

Semantic and hybrid retrieval are available at:

```http
GET /api/v1/retrieval/semantic-search?tenantId=10000000-0000-0000-0000-000000000001&query=booking%20accepted%20status%20missing&limit=5

GET /api/v1/retrieval/hybrid-search?tenantId=10000000-0000-0000-0000-000000000001&query=booking%20accepted%20status%20missing&limit=5
```

The local Python service uses `sentence-transformers/all-MiniLM-L6-v2` to produce normalized 384-dimensional vectors. PostgreSQL stores them in `vector(384)`. Hybrid retrieval combines lexical and semantic rankings through Reciprocal Rank Fusion (RRF).

In the Development environment, Swagger UI is available at:

```text
http://localhost:5051/swagger
```

## Test

```powershell
dotnet test ProductionSupportCopilot.sln
```

## Evaluate lexical retrieval

With PostgreSQL running and development data seeded:

```powershell
dotnet run --project tools/ProductionSupportCopilot.Evaluation -- lexical
dotnet run --project tools/ProductionSupportCopilot.Evaluation -- semantic
```

The evaluator runs the versioned dataset at `evaluation/golden-retrieval.json` and reports Recall@1, Recall@5, and mean reciprocal rank (MRR). Current lexical baseline on 12 questions:

```text
Recall@1: 91.7%
Recall@5: 91.7%
MRR:      0.917
```

One semantic-style query intentionally fails lexical retrieval. It will be used to measure whether embeddings improve recall instead of adding semantic search without evidence.

Current comparison on the small 12-question dataset:

| Mode | Recall@1 | Recall@5 | MRR |
|---|---:|---:|---:|
| Lexical | 91.7% | 91.7% | 0.917 |
| Semantic | 83.3% | 100% | 0.903 |
| Hybrid RRF | 100% | 100% | 1.000 |

These figures are a development baseline, not a production-quality claim; the corpus and evaluation set must grow.

To inspect the full-text indexes and query plan:

```powershell
Get-Content -Raw scripts/sql/verify-full-text-index.sql |
  docker exec -i support-copilot-postgres psql -U support_copilot -d support_copilot
```

## Design

- [Product requirements](docs/product-requirements.md)
- [ADR 0001: Modular monolith](docs/decisions/0001-modular-monolith.md)
- [Hinglish learning and decision journal](PROJECT_LEARNING_JOURNAL_HINGLISH.md)

## Next milestone

PostgreSQL/pgvector, lexical retrieval, local MiniLM embeddings, semantic search, hybrid Reciprocal Rank Fusion, and a measurable comparison baseline are working. Next, expand the corpus/evaluation set, add retrieval telemetry, and connect grounded answer generation to hybrid evidence.
