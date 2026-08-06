# ITL Automation Gateway (.NET 8)

## What Is Implemented

- REST job API with async job model.
- SQLite persistence for jobs and webhook subscriptions.
- Legacy TCP bridge server (GB2312 + CRLF frame split) for existing Interleaver client.
- Background orchestrator that translates operations to legacy commands.
- Webhook event publishing for job lifecycle.

## Run

```powershell
dotnet run --project project/ITL.AutomationGateway.Api/ITL.AutomationGateway.Api.csproj
```

Default endpoints:

- HTTP API: `http://localhost:5000` / `https://localhost:5001` (Kestrel defaults)
- Legacy TCP listener: `127.0.0.1:9100` in Development profile
- Swagger UI: `http://localhost:5000/docs` (or HTTPS endpoint)
- OpenAPI JSON: `http://localhost:5000/openapi/v1.json`

## API Documentation For Third-Party

Third-party integrators can use either of these:

- Interactive API docs: `/docs`
- Machine-readable OpenAPI spec: `/openapi/v1.json`

Recommended delivery package to third-party:

- Running base URL (test/prod)
- OpenAPI JSON URL
- This README for integration notes and operation semantics

## Current Operations

- `open_template` -> `SNNO;{sn}`
- `scan_nopdl` -> `TEST;NOPDL`
- `scan_pdl` -> `TEST;PDL`
- `uv_set` -> `TEST;UV;{sn};{0|1}`
- `stop` -> `TEST;STOP` (placeholder, confirm station support)

## API Quick Start

### 1) Create job

```http
POST /api/v1/stations/{stationId}/jobs
Idempotency-Key: req-001
Content-Type: application/json

{
  "operation": "open_template",
  "sn": "1831760166",
  "clientReqId": "req-001"
}
```

### 2) Query job

```http
GET /api/v1/jobs/{jobId}
```

### 3) Register webhook

```http
POST /api/v1/stations/{stationId}/subscriptions/webhooks
Content-Type: application/json

{
  "url": "http://thirdparty.local/webhook/itl",
  "secret": "demo-secret"
}
```

## Integration Notes

- Existing Interleaver software should connect to the gateway TCP listener as its automation server target.
- The gateway expects legacy ACK/result frames with CRLF terminator.
- For `scan_nopdl` / `scan_pdl`, success/failure is inferred from frames starting with `TEST;PASS;` or `TEST;FAIL`.

## Known Gaps (Next Iteration)

- No webhook delivery persistence/outbox yet (best-effort with retry in-memory only).
- No auth/API key yet.
- No strict station-level command concurrency guard beyond single in-flight TCP send lock.
- `stop` command compatibility should be validated on the real station.
