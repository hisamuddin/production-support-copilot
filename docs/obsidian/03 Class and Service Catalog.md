# Class and Service Catalog

Related: [[01 Architecture Map]], [[02 RAG From Zero]], [[05 Runbook and Demo]].

## Domain classes

| Class | Kya rakhta hai | Important behavior/invariant |
|---|---|---|
| `Entity` | ID and timestamps | `MarkUpdated` state change time refresh karta hai |
| `Tenant` | stable key and display name | empty values reject; documents ownership root |
| `SupportDocument` | source text, hash, version, classification | replacement hash/version update; tenant+external ID unique |
| `DocumentChunk` | ordinal, offsets, text, token estimate, vector | exactly 384 dimensions enforce |
| `IngestionJob` | event ID, attempts, status, error | duplicate event tracked; state transitions explicit |
| `ApprovalRequest` | action, arguments, requester, expiry/decision | one decision only; expiry check |

## Application contracts and services

| Class/interface | Role | Why separate? |
|---|---|---|
| `ChunkDraft` | chunker output value | EF entity create hone se pehle pure data |
| `IngestDocumentCommand` | ingestion message contract | HTTP/Kafka same contract use karte |
| `IDocumentChunker` | chunk algorithm boundary | alternative token-aware chunker testable |
| `DeterministicDocumentChunker` | paragraph/sentence boundary chunks | repeatable indexing/evaluation |
| `IDocumentIngestionService` | end-to-end indexing use case | API/consumer implementation details se free |
| `IIngestionEventPublisher` | async boundary | direct learning mode vs Kafka mode |
| `RetrievalQuery` | tenant/query/topK/filter | authorization-aware search input |
| `RetrievedPassage` | ranked evidence | raw component + combined scores observable |
| `IEmbeddingProvider` | text -> float vector | model/service swappable |
| `IHybridRetriever` | query -> passages | Postgres/search implementation swappable |
| `IPromptInjectionGuard` | inspect and mark trust | safety policy centralized |
| `PromptInjectionGuard` | basic phrase checks + boundary tags | defense-in-depth first layer |
| `LlmRequest` | question + evidence + system policy | clear model contract |
| `LlmAnswer` | answer + cited IDs + tokens | structured validation and metrics |
| `ILlmProvider` | model abstraction | vendor/local fallback |
| `ExtractiveDemoLlmProvider` | deterministic evidence summary | free, stable tests/demo |
| `ResilientLlmProvider` | 3 retries then fallback | graceful degradation |
| `ICopilotQueryService` | public answer use case | endpoint thin rakhta hai |
| `GroundedCopilotQueryService` | guard/retrieve/generate/validate citations | main RAG orchestration |
| `CopilotTelemetry` | standard ActivitySource | RAG spans vendor-neutral banata hai |
| `IAiUsageRecorder` | token/cost usage sink | Application ko dashboard vendor se decouple karta hai |
| `ISupportTool` | named controlled capability | allow-list and policy metadata |
| `ToolRegistry` | tool lookup | model/user arbitrary code execute nahi kar sakta |
| `ServiceHealthTool` | mock live health | docs vs live-state distinction |
| `ConsumerLagTool` | mock Kafka lag | read-only operational evidence |
| `BookingStatusTool` | synthetic booking lookup | safe domain-relevant demo |
| `ReplayDeadLetterTool` | approval request only | consequential action boundary |
| `IApprovalService` | request/decision persistence contract | workflow database decoupling |
| `SupportWorkflowService` | explicit routing/state trace | agent decisions auditable/deterministic |

## Infrastructure classes

| Class | External responsibility | Failure behavior |
|---|---|---|
| `SupportCopilotDbContext` | EF tables, constraints, vector extension | DB exceptions surface to API/logs |
| `DatabaseInitializer` | local migrate + seed runbooks | startup fails fast if configured DB unavailable |
| `DocumentIngestionService` | idempotency, upsert, chunk, embed, persist | job error captured; DLQ path can publish |
| `DeterministicEmbeddingProvider` | offline hashed vectors | always local; not true semantic model |
| `HttpEmbeddingProvider` | call Python `/embed` | timeout/status/dimension validation |
| `PostgresHybridRetriever` | tenant-safe lexical+semantic+RRF | unknown tenant returns empty evidence |
| `OllamaLlmProvider` | local model structured JSON | invalid HTTP/JSON triggers resilient fallback |
| `KafkaOptions` | broker/topic/group config | environment override friendly |
| `KafkaIngestionPublisher` | keyed idempotent publication | acks-all/retries; caller sees delivery error |
| `DirectIngestionPublisher` | no-broker learning path | executes ingestion in request path |
| `KafkaIngestionWorker` | consume, process, DLQ, manual commit | transient loop retry; poison diagnostic envelope |
| `ApprovalService` | approval DB operations | missing/expired/already decided rejected |
| `DependencyInjection` | Infrastructure registrations | one composition method avoids scattered wiring |

## API classes

| Class | Role | Detail |
|---|---|---|
| `Program` | composition root | Swagger, JWT, rate limits, endpoints, startup seed |
| `CorrelationIdMiddleware` | trace header/scope | accepts supplied ID or creates one |
| `RequestTelemetry` | bounded request + AI metrics | 500 item queues, p50/p95, tokens/model/cost snapshot |
| `RequestTelemetryMiddleware` | measures every request | finally block records failures too |
| `DemoTokenRequest` | local auth request | default support/operator roles |
| `ToolExecutionRequest` | JSON tool arguments | tool validates expected properties |
| `ApprovalDecisionRequest` | approve/reject input | operator role enforced endpoint-side |

## Python classes/functions

| Symbol | Role |
|---|---|
| `EmbedRequest` | 1-64 input texts validation |
| `EmbedResponse` | vectors, model and dimensions |
| `model()` | cached SentenceTransformer load |
| `embed()` | normalized batch embedding endpoint |
| `evaluate.py post()` | dependency-free HTTP helper |
| `evaluate.py main()` | golden dataset metrics/report |
