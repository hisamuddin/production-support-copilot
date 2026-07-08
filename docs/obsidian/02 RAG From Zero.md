# RAG From Zero

Related: [[01 Architecture Map]], [[07 Glossary]], [[04 Security Reliability and Evaluation]].

## Beginner example

Document says: “status-indexer lag above 500 for five minutes needs investigation.” User asks: “updates queue mein atke kyun hain?” Exact words differ. Embedding model both sentences ko meaning-space mein nearby vectors de sakta hai. Semantic search document retrieve karta hai; LLM evidence ko readable answer banata hai; citation engineer ko original runbook dikhati hai.

## Why 384 numbers?

MiniLM architecture fixed-size representation emits 384 dimensions. Individual dimension human-readable topic nahi hota. Meaning distributed representation mein encoded hota hai. Vector length schema/model agreement hai: model change karoge to re-embedding aur column/index plan required.

## Similarity

Cosine distance angle compare karti hai. Smaller distance means closer direction. Normalization vectors ko unit length banati hai, so magnitude noise reduce hota hai.

## Index choices

- Exact scan: perfect recall, small corpus mein simple.
- HNSW: fast approximate graph index, more memory/build cost, strong query performance.
- IVFFlat: trained cluster lists, tuning required, lower memory possible.

Current code correctness-first exact vector ordering use karta hai. Scale proof ke baad HNSW add karna chahiye.

## Reranking

RRF rank positions combine karta hai. Cross-encoder reranker query+chunk jointly score karke better precision de sakta hai but latency/cost add karta hai. First retrieve 20-50 candidates, then rerank top candidates, then 5-8 context chunks choose karo.

## Fine-tuning decision

RAG dynamic knowledge ke liye; fine-tuning behavior/task pattern ke liye. Fine-tune tab consider karo jab evaluated prompt/RAG baseline ke baad repeatable capability gap ho aur sufficient high-quality training data ho.
