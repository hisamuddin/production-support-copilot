# Glossary

Related: [[02 RAG From Zero]], [[04 Security Reliability and Evaluation]].

- **LLM:** next-token model capable of language tasks.
- **Embedding:** fixed-size numeric representation of meaning.
- **Vector:** ordered list of numbers.
- **Cosine distance:** angular difference between vectors.
- **RAG:** retrieve external evidence before generation.
- **Chunk:** independently indexed document segment.
- **Hybrid retrieval:** lexical plus semantic search.
- **Reranker:** second-stage candidate ordering model/algorithm.
- **Grounding:** tying claims to provided evidence.
- **Hallucination:** unsupported generated claim.
- **Citation:** source identifier attached to answer claim/evidence.
- **Prompt injection:** untrusted input attempting to override instructions.
- **Tool calling:** model/workflow selecting a controlled application function.
- **Agent:** iterative model-directed workflow; use bounded authority.
- **State machine:** explicit allowed stages/transitions.
- **Idempotency:** retry produces no duplicate effect.
- **DLQ:** dead-letter queue for poison messages.
- **RRF:** reciprocal rank fusion of ranked lists.
- **Recall@K:** fraction where expected result appears in top K.
- **MRR:** mean reciprocal rank; rewards earlier correct result.
- **p95 latency:** 95% requests at or below this duration.
- **Fine-tuning:** changing model weights with task examples.
- **Structured output:** response constrained to schema.
- **SSE:** server-sent events for one-way streaming responses.
