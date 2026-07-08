from __future__ import annotations

import os
from functools import lru_cache
from typing import List

from fastapi import FastAPI
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer

MODEL_NAME = os.getenv("EMBEDDING_MODEL", "sentence-transformers/all-MiniLM-L6-v2")
app = FastAPI(title="Support Copilot AI Service", version="1.0.0")


class EmbedRequest(BaseModel):
    texts: List[str] = Field(min_length=1, max_length=64)


class EmbedResponse(BaseModel):
    embeddings: List[List[float]]
    model: str
    dimensions: int


@lru_cache(maxsize=1)
def model() -> SentenceTransformer:
    return SentenceTransformer(MODEL_NAME)


@app.get("/health")
def health() -> dict:
    return {"status": "healthy", "model": MODEL_NAME}


@app.post("/embed", response_model=EmbedResponse)
def embed(request: EmbedRequest) -> EmbedResponse:
    vectors = model().encode(request.texts, normalize_embeddings=True).tolist()
    return EmbedResponse(embeddings=vectors, model=MODEL_NAME, dimensions=len(vectors[0]))
