"""Small reproducible offline/HTTP evaluation harness for retrieval and grounded answers."""
from __future__ import annotations

import argparse
import json
import statistics
import time
import urllib.error
import urllib.request
from pathlib import Path


def post(url: str, payload: dict, token: str | None = None) -> tuple[int, dict]:
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, json.dumps(payload).encode(), headers, method="POST")
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status, json.loads(response.read())
    except urllib.error.HTTPError as error:
        return error.code, json.loads(error.read() or b"{}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", default="http://localhost:5051")
    parser.add_argument("--dataset", default=str(Path(__file__).with_name("golden_dataset.jsonl")))
    args = parser.parse_args()

    _, auth = post(f"{args.base_url}/api/v1/auth/demo-token", {"userId": "evaluator", "tenantId": "demo-tenant", "requestedRoles": ["support"]})
    token = auth["accessToken"]
    rows = [json.loads(line) for line in Path(args.dataset).read_text(encoding="utf-8").splitlines() if line.strip()]
    ranks, latencies, abstentions, citation_checks = [], [], 0, 0

    for row in rows:
        started = time.perf_counter()
        status, body = post(f"{args.base_url}/api/v1/copilot/query", {"question": row["question"], "tenantId": "demo-tenant"}, token)
        latencies.append((time.perf_counter() - started) * 1000)
        titles = [item["title"] for item in body.get("citations", [])]
        expected = row["expected_document"]
        rank = titles.index(expected) + 1 if expected in titles else 0
        if expected:
            ranks.append(rank)
            citation_checks += int(body.get("isGrounded", False) and rank > 0)
        else:
            abstentions += int(not body.get("isGrounded", False) or status in (400, 403))
        print(f"{row['id']}: http={status} expected={expected!r} rank={rank} status={body.get('status')}")

    answerable = len(ranks)
    report = {
        "recall_at_1": sum(rank == 1 for rank in ranks) / max(1, answerable),
        "recall_at_5": sum(0 < rank <= 5 for rank in ranks) / max(1, answerable),
        "mrr": sum((1 / rank) if rank else 0 for rank in ranks) / max(1, answerable),
        "grounded_citation_rate": citation_checks / max(1, answerable),
        "safe_abstention_rate": abstentions / max(1, len(rows) - answerable),
        "median_latency_ms": statistics.median(latencies),
        "p95_latency_ms": sorted(latencies)[max(0, int(len(latencies) * .95) - 1)],
        "examples": len(rows),
    }
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
