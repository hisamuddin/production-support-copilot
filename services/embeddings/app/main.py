import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer


MODEL_NAME = os.getenv(
    "MODEL_NAME",
    "sentence-transformers/all-MiniLM-L6-v2",
)
MAX_BATCH_SIZE = 32
MAX_INPUT_CHARACTERS = 8_000

model: SentenceTransformer | None = None


@asynccontextmanager
async def lifespan(_: FastAPI):
    global model
    model = SentenceTransformer(MODEL_NAME)
    yield
    model = None


app = FastAPI(
    title="Support Copilot Embedding Service",
    version="1.0.0",
    lifespan=lifespan,
)


class EmbeddingRequest(BaseModel):
    inputs: list[str] = Field(min_length=1, max_length=MAX_BATCH_SIZE)


class EmbeddingItem(BaseModel):
    index: int
    embedding: list[float]


class EmbeddingResponse(BaseModel):
    model: str
    dimensions: int
    data: list[EmbeddingItem]


@app.get("/health")
def health() -> dict[str, str]:
    if model is None:
        raise HTTPException(status_code=503, detail="Model is not loaded.")
    return {"status": "healthy", "model": MODEL_NAME}


@app.post("/v1/embeddings", response_model=EmbeddingResponse)
def create_embeddings(request: EmbeddingRequest) -> EmbeddingResponse:
    if model is None:
        raise HTTPException(status_code=503, detail="Model is not loaded.")

    cleaned_inputs = [value.strip() for value in request.inputs]
    if any(not value for value in cleaned_inputs):
        raise HTTPException(status_code=400, detail="Inputs cannot be blank.")
    if any(len(value) > MAX_INPUT_CHARACTERS for value in cleaned_inputs):
        raise HTTPException(
            status_code=400,
            detail=f"Each input must be at most {MAX_INPUT_CHARACTERS} characters.",
        )

    vectors = model.encode(
        cleaned_inputs,
        normalize_embeddings=True,
        convert_to_numpy=True,
    )

    items = [
        EmbeddingItem(index=index, embedding=vector.astype(float).tolist())
        for index, vector in enumerate(vectors)
    ]

    return EmbeddingResponse(
        model=MODEL_NAME,
        dimensions=len(items[0].embedding),
        data=items,
    )
