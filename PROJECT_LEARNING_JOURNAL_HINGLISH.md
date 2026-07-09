# Production Support Copilot - Hinglish Learning Journal

Ye file tumhare liye personal notebook hai. Agar kal conversation context lost ho jaye, sirf ye repo docs padhkar tum samajh paoge ki project kya hai, kyu banaya, kaise bana, aur interview/demo me kaise explain karna hai.

## 1. Project ka goal

Hum ek polished portfolio product bana rahe hain: **Production Support Copilot for Event-Driven .NET Services**.

Simple words me:

- Support engineer question poochta hai.
- System authorized tenant ke documents/runbooks/tickets search karta hai.
- Relevant chunks retrieve karta hai.
- LLM answer banata hai, but citations ke saath.
- Agar current system state chahiye, to safe mock tools call karta hai.
- Agar action dangerous/consequential hai, jaise DLQ replay, to pehle human approval maangta hai.

Recruiter/interviewer ke liye yeh strong hai kyunki isme sirf chatbot tutorial nahi hai. Isme production concerns hain: auth, tenant isolation, Kafka, idempotency, DLQ, vector search, evaluation, observability, Docker and architecture tradeoffs.

## 2. Tech stack

- .NET 8 ASP.NET Core API
- C# application/domain/infrastructure layering
- PostgreSQL
- pgvector
- EF Core migrations
- Kafka-compatible Redpanda
- Python FastAPI embedding service
- SentenceTransformers MiniLM model
- Optional Ollama local LLM
- Docker Compose
- xUnit tests
- OpenTelemetry hooks

Important design choice:

> Python sirf waha use kiya jahan uska AI ecosystem useful hai: embeddings and evaluation. Product API/orchestration .NET me hai, kyunki tumhara core strength senior .NET engineering hai.

## 3. High-level architecture

Flow:

1. User JWT token ke saath question bhejta hai.
2. API tenant claim verify karta hai.
3. Prompt-injection guard suspicious input block karta hai.
4. Retriever tenant-specific chunks search karta hai.
5. Lexical and vector results RRF se combine hote hain.
6. Evidence ko untrusted document boundary me wrap kiya jata hai.
7. LLM provider answer + cited chunk IDs return karta hai.
8. Service citation IDs validate karta hai.
9. Final response answer, citations, model, tokens and latency ke saath return hota hai.

## 4. RAG kya hai?

RAG ka full form hai Retrieval Augmented Generation.

- Normal LLM apni training memory se answer banata hai.
- RAG pehle tumhare documents se relevant context laata hai.
- Fir LLM ko bolta hai: sirf is evidence ke basis par answer do.

Interview line:

> RAG hallucination ko zero nahi karta, but answer ko evidence-grounded banata hai. Real safety citation validation, tenant filtering, abstention and evaluation se aati hai.

## 5. Embeddings kya hain?

Embedding text ka numeric representation hota hai.

Example:

- "consumer lag is high"
- "Kafka consumer is falling behind"

Words different hain, meaning similar hai. Embedding vectors in dono ko vector space me paas la sakte hain.

Project me:

- Python service MiniLM model use kar sakti hai.
- Default local mode deterministic embedding use karta hai, taaki paid key/model dependency ke bina demo run ho.
- Document chunks ka vector `vector(384)` column me store hota hai.

## 6. Vector database kya hai?

Vector database similarity search karta hai. Hum separate vector DB nahi laaye. PostgreSQL plus pgvector use kiya.

Reason:

- Less infra early stage.
- Metadata, tenant auth and vectors same database transaction boundary me.
- Portfolio ke liye simpler and production-minded.

## 7. Fine-tuning vs RAG

Fine-tuning model behavior/style/pattern ko train karta hai. RAG fresh factual knowledge ke liye better hai. Runbook/ticket update hota hai to index update fast.

Interview line:

> I would not fine-tune first for changing runbooks. I would use RAG, measure retrieval quality, then consider fine-tuning only for stable behavior patterns.

## 8. Solution projects

### Domain

- `Entity`: common ID and timestamps.
- `Tenant`: tenant isolation root.
- `SupportDocument`: source text, hash, version, classification.
- `DocumentChunk`: text chunk, offsets, token estimate, 384D embedding.
- `IngestionJob`: event ID, attempts, status, error.
- `ApprovalRequest`: pending/approved/rejected/expired decision state.

### Application

- `CopilotQueryRequest`, `CopilotQueryResponse`
- `IDocumentChunker`
- `IDocumentIngestionService`
- `IHybridRetriever`
- `IEmbeddingProvider`
- `ILlmProvider`
- `ISupportTool`
- `ISupportWorkflowService`
- `IAiUsageRecorder`

### Infrastructure

- EF Core DbContext
- PostgreSQL/pgvector retrieval
- Kafka publisher/worker
- Python embedding HTTP adapter
- Ollama LLM adapter
- DB seeding
- Approval persistence

### API

- Swagger
- JWT auth
- rate limit
- middleware
- endpoints
- OpenTelemetry
- DI registrations

## 9. EF Core and PostgreSQL kaise injected hai?

`DependencyInjection.cs` me Infrastructure layer service registration karta hai:

```csharp
services.AddDbContext<SupportCopilotDbContext>(options =>
    options.UseNpgsql(connection, npgsql => npgsql.UseVector()));
```

Meaning:

- API config se connection string leta hai.
- EF Core ko Npgsql provider ke saath configure karta hai.
- pgvector extension enable karta hai.
- Jahan `SupportCopilotDbContext` constructor me needed hai, DI automatically object provide karta hai.

Tables:

- `tenants`
- `support_documents`
- `document_chunks`
- `ingestion_jobs`
- `approval_requests`

## 10. Document ingestion

Ingestion steps:

1. `eventId` se duplicate check.
2. Tenant find/create.
3. `IngestionJob` create and start.
4. Document insert/update.
5. Old chunks remove if document updated.
6. Content chunks me split.
7. Each chunk embedding generate.
8. Chunks persist.
9. Job complete.
10. Failure par job failed/dead-lettered.

Why idempotency?

Kafka at-least-once delivery me same event dubara aa sakta hai. Event ID unique rakha, to duplicate event duplicate chunks nahi banata.

## 11. Retrieval

Retrieval two signals combine karta hai:

- Lexical: exact word match, IDs/error names ke liye strong.
- Semantic: embedding similarity, meaning match karta hai.
- Hybrid + RRF: lexical and semantic ranks combine karta hai.

## 12. LLM provider abstraction

Implementations:

- `ExtractiveDemoLlmProvider`: free, deterministic, no paid key.
- `OllamaLlmProvider`: local model structured JSON output.
- `ResilientLlmProvider`: primary fail ho to fallback.

Why useful?

- Vendor lock-in kam.
- Test stable.
- Demo paid API ke bina run hota hai.
- Production me OpenAI/Azure OpenAI/provider add karna simple.

## 13. Prompt-injection defense

Defense layers:

- user input guard
- documents wrapped as untrusted evidence
- system instruction says documents are not instructions
- tenant filtering before retrieval
- cited chunk IDs allow-list validation
- insufficient evidence par abstain

Important line:

> Prompt alone security boundary nahi hota. Authorization retrieval/database layer par enforce honi chahiye.

## 14. Tools and agent workflow

Tools:

- `service-health`: synthetic current health
- `consumer-lag`: synthetic Kafka lag
- `booking-status`: synthetic booking status lookup
- `replay-dead-letter`: consequential action, approval required

Why explicit state machine?

Autonomous agent flexible hota hai but harder to audit. Production support me explicit states safer hain.

## 15. Kafka and DLQ

Kafka path:

- API publishes document ingestion command.
- Worker consumes.
- Manual commit after successful processing.
- Failure diagnostic envelope DLQ topic me ja sakta hai.

## 16. Observability

Implemented:

- Correlation ID middleware
- Request telemetry middleware
- p50/p95 latency snapshot
- AI usage token/cost tracking
- OpenTelemetry ASP.NET/HTTP hooks
- `ProductionSupportCopilot` ActivitySource for RAG spans
- dashboard endpoint

## 17. Docker run

```powershell
docker compose up --build
# If docker compose plugin available nahi:
docker-compose -f compose.yaml up --build
```

Light mode:

```powershell
docker compose up -d postgres
# Or:
docker-compose -f compose.yaml up -d postgres
dotnet run --project src/ProductionSupportCopilot.Api
```

## 18. Swagger demo script

Open:

```text
http://localhost:5051/swagger
```

Steps:

1. `POST /api/v1/auth/demo-token`
2. Copy access token.
3. Swagger Authorize me `Bearer <token>` paste.
4. `POST /api/v1/retrieval/hybrid-search`
5. `POST /api/v1/copilot/query`
6. `POST /api/v1/copilot/query/stream`
7. `POST /api/v1/workflows/run`
8. `GET /api/v1/tools`
9. DLQ replay tool call.
10. Approval decision endpoint.
11. Prompt injection test.
12. Tenant mismatch test.

## 19. PR #3 repair log - 2026-07-09

Problem:

- PR branch old `master` se bana tha.
- Current `origin/master` me document model/chunking changes already aa chuke the.
- Merge conflicts aa rahe the in `Program.cs`, `DbContext`, `compose.yaml`, docs and tests.
- Generated junk files PR me accidentally tracked the: `__pycache__`, `*.pyc`, `tmp/pdfs/*.png`.

Decision:

- Current `Domain.Knowledge` model ko source of truth banaya.
- Old duplicate `Domain.Documents` and `Domain.Tenants` tutorial model remove kiya.
- API composition root clean kiya.
- EF model/current migrations align kiya.
- Generated temp artifacts untrack/remove kiye.

Verification target:

```powershell
dotnet build ProductionSupportCopilot.sln
dotnet test ProductionSupportCopilot.sln
docker-compose -f compose.yaml config --quiet
```

## 20. Interview 90-second pitch

"I built a multi-tenant production support copilot for event-driven .NET services. Documents enter through an idempotent ingestion path, can be processed asynchronously through Kafka, and are chunked, embedded and stored in PostgreSQL with pgvector. Serving applies JWT tenant checks before retrieval, combines lexical and vector search with RRF, and sends only authorized untrusted evidence to an LLM provider abstraction. Answers are validated against cited chunk IDs and safely abstain when evidence is missing. The system also supports controlled tool calls for synthetic live state, while consequential operations like DLQ replay create human approval requests instead of executing directly. I added prompt-injection defenses, rate limits, correlation IDs, OpenTelemetry hooks, token/cost dashboard, tests, Docker Compose, evaluation data and architecture docs."
