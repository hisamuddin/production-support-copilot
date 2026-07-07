SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename = 'document_chunks'
ORDER BY indexname;

EXPLAIN (ANALYZE, BUFFERS)
SELECT c."Id", c."DocumentId", ts_rank_cd(c."SearchVector", plainto_tsquery('english', 'consumer lag')) AS score
FROM document_chunks AS c
WHERE c."TenantId" = '10000000-0000-0000-0000-000000000001'
  AND c."SearchVector" @@ plainto_tsquery('english', 'consumer lag')
ORDER BY score DESC
LIMIT 5;
