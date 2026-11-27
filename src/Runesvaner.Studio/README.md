# Runesvaner Studio

Local, read-only Studio for Runesvaner cost data.

## Run

Build the React cost page first:

    npm ci --prefix src/Runesvaner.Studio.UI
    npm run build --prefix src/Runesvaner.Studio.UI
    dotnet run --project src/Runesvaner.Studio -c Release

Studio binds to `127.0.0.1` on a random port and prints a URL with a one-time local token:

    Runesvaner Studio: http://127.0.0.1:57015/?token=...

Open that URL. The token is stored in `sessionStorage`; it is not written to the URL after bootstrap.

If the UI build is missing, the API still runs but the browser returns a static-file 404.

## Data path

Resolution order:

1. `Studio:DataPath` configuration.
2. `RUNESVANER_DATA_PATH` environment variable.
3. Nearest `.runesvaner` directory, walking up from the current directory.
4. `./.runesvaner` (created lazily by the recording side).

The Studio reads JSONL sessions only at this stage.

## API

All API calls require `Authorization: Bearer <token>`.

| Endpoint | Purpose |
| --- | --- |
| `GET /api/health` | Status, version, resolved data path |
| `GET /api/costs` | Cost aggregation by provider/model |
| `GET /api/requests?limit=20` | Newest request summaries |
| `GET /api/requests/{id}` | Full request record with segments, usage, cost, stream, tags |

Security boundaries:

- Binds to `127.0.0.1` only.
- Rejects non-loopback `Host` headers.
- Random token per process; printed only to the local console.
- Read-only; no delete/update endpoints.
- Does not serve prompt/completion content.

## Scope

M2.1 implements the cost ledger and recent requests view. M2.2 adds request detail, latency waterfall, cost breakdown, stream metrics, and tags. Session browser and compare view remain.