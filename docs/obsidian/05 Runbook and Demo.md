# Runbook and Demo

Related: [[03 Class and Service Catalog]], [[06 System Design Interview]].

## Quick run

```powershell
docker compose up --build
```

Swagger: `http://localhost:5051/swagger`

Token -> Authorize -> hybrid query -> grounded query -> current-health workflow -> DLQ replay workflow -> approval -> injection/tenant negative tests.

## Database inspection

pgAdmin connection: host `localhost`, port `5432`, database `support_copilot`, user `support`, password `support_dev_password`. Tables live under Schemas -> public -> Tables. View `tenants`, `support_documents`, `document_chunks`, `ingestion_jobs`, `approval_requests`.

## Evaluation

```powershell
python evaluation/evaluate.py --base-url http://localhost:5051
```

## Common failures

- Port 5432 in use: stop old Postgres container or change compose host port.
- First embedding build slow: PyTorch/model download is large; model cache volume makes later starts faster.
- Swagger 401: obtain token and click Authorize.
- 403: token tenant and request tenant differ, or operator role missing.
- Empty retrieval: seed did not complete; inspect API logs and database.
- Locked DLL on Windows: stop running API/testhost, then build again.
- NuGet NU1900 offline warning: vulnerability feed unreachable; build may still compile from cache, but CI should perform online audit.
