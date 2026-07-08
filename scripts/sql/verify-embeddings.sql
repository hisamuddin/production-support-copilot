SELECT
    COUNT(*) AS total_chunks,
    COUNT("Embedding") AS embedded_chunks,
    COUNT(*) - COUNT("Embedding") AS missing_embeddings
FROM document_chunks;

SELECT
    d."Title",
    c."Sequence",
    vector_dims(c."Embedding") AS dimensions,
    ROUND(vector_norm(c."Embedding")::numeric, 4) AS vector_norm
FROM document_chunks AS c
INNER JOIN documents AS d ON d."Id" = c."DocumentId"
ORDER BY d."Title", c."Sequence";
