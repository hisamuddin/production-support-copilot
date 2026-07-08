# Architecture Map

Related: [[02 RAG From Zero]], [[03 Class and Service Catalog]], [[04 Security Reliability and Evaluation]].

## Dependency rule

```text
API -> Application <- Infrastructure
          |
        Domain
Infrastructure -> Domain
```

Domain ko Infrastructure ya API reference nahi karna. Application interfaces define karta hai; Infrastructure unko implement karta hai; API composition root registrations wire karta hai. Isse database/model vendor replace karne par use-case contract stable rehta hai.

## Runtime dependency graph

```mermaid
flowchart TB
    Program --> GroundedCopilotQueryService
    Program --> SupportWorkflowService
    Program --> DocumentIngestionService
    GroundedCopilotQueryService --> PromptInjectionGuard
    GroundedCopilotQueryService --> PostgresHybridRetriever
    GroundedCopilotQueryService --> ILlmProvider
    PostgresHybridRetriever --> SupportCopilotDbContext
    PostgresHybridRetriever --> IEmbeddingProvider
    DocumentIngestionService --> DeterministicDocumentChunker
    DocumentIngestionService --> IEmbeddingProvider
    DocumentIngestionService --> SupportCopilotDbContext
    IEmbeddingProvider --> HttpEmbeddingProvider
    IEmbeddingProvider --> DeterministicEmbeddingProvider
    ILlmProvider --> OllamaLlmProvider
    ILlmProvider --> ExtractiveDemoLlmProvider
    SupportWorkflowService --> ToolRegistry
    ToolRegistry --> ApprovalService
    KafkaIngestionWorker --> DocumentIngestionService
```

## Deployment graph

```mermaid
flowchart LR
    Browser --> API[.NET API :5051]
    API --> PG[(Postgres/pgvector :5432)]
    API --> AI[FastAPI MiniLM :8001]
    API --> RP[Redpanda/Kafka :9092]
    RP --> API
    API -. optional .-> OL[Ollama :11434]
```

## Tradeoffs

Postgres + pgvector simplifies transactions and tenant filters. Redpanda gives a Kafka-compatible local developer experience. MiniLM is small enough for local embeddings. Ollama is optional because model download/RAM varies by laptop. Modular monolith keeps operational cost low while interfaces preserve extraction boundaries.
