# Product Requirements Document (PRD): Runesvaner

| Field | Value |
| --- | --- |
| Document status | Accepted (D-01–D-25 resolved) |
| Version | 1.0 |
| Date | 2026-11-25 |
| Product stage | M0 Design |
| Audience | Product, engineering, design, early contributors |
| Related documents | `00-project-charter.md`, `02-problem-and-personas.md`, `05-technical-design.md` |

---

## 1. Product Overview

Runesvaner is a .NET-native AI request profiler. Through Microsoft.Extensions.AI middleware it collects the internal stages, token usage, cost, and streaming behavior of a single AI request, outputs a readable latency waterfall and cost ledger, and can optionally export to OpenTelemetry.

### 1.1 Product Goals

1. Enable developers to answer "where was this request slow, and where did it get expensive" within 5 minutes.
2. Make cost attribution traceable, explainable, and reconcilable.
3. Remain usable under Native AOT / Trimming.
4. Protect content privacy by default and avoid a mandatory cloud dependency.

### 1.2 Success Metrics

| Metric | M2 target | 1.0 target |
| --- | --- | --- |
| Time to first waterfall view | ≤ 5 minutes | ≤ 3 minutes |
| Collection overhead P95 | ≤ 1 ms | ≤ 0.5 ms |
| Cost reconciliation error | ≤ 5% | ≤ 2% |
| External trial projects | 100 | 1,000 |
| AOT smoke pass rate | 100% | 100% |

---

## 2. User Stories and Epics

| Epic | User story |
| --- | --- |
| E1 Onboarding | As a .NET developer, I want to onboard with 3 lines of code, so that I do not change my existing architecture |
| E2 Latency | As a backend engineer, I want to see the internal segments of a request, so that I can locate the slow points |
| E3 Cost | As FinOps, I want to attribute cost by dimension, so that I can explain the bill |
| E4 Streaming | As an AI engineer, I want to see TTFT and token rate, so that I can optimize the experience |
| E5 Local diagnostics | As an independent developer, I want to see results on my own machine, so that I do not need to deploy a platform |
| E6 AOT | As a platform engineer, I want to use it in AOT services, so that I do not compromise the build |
| E7 Interop | As a platform team, I want to export OTLP, so that I can reuse the existing backend |

---

## 3. Functional Requirements List

Priority: Must (M), Should (S), Could (C), Won't (W).

### 3.1 Onboarding and Configuration

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-001 | Provide the `UseRunesvaner()` MEAI middleware | M | Supports `IChatClient` and `IEmbeddingGenerator` |
| FR-002 | Provide the `AddRunesvaner()` DI extension | M | Registers collector, storage, options |
| FR-003 | Support code configuration and `appsettings.json` | M | Configuration section `Runesvaner` |
| FR-004 | Support per-request override of key options | S | Via `Activity` tags or context |
| FR-005 | Support standalone use without DI | S | Console / script scenarios |
| FR-006 | Provide an onboarding diagnostic command | S | `runesvaner doctor` checks version and configuration |

### 3.2 Collection and Latency Breakdown

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-010 | Collect the Chat request root Span | M | Includes provider, model, operation |
| FR-011 | Collect the Embedding request root Span | M | Includes batch size, dimensions |
| FR-012 | Generate latency waterfall segments | M | At least 6 segments: Queue, Connect, TLS, Upload, TTFT, Generation |
| FR-013 | Mark estimated segments | M | `estimated=true` + measurement method |
| FR-014 | Compute unaccounted time | M | `unaccounted` and `overlap` |
| FR-015 | Collect vector retrieval Span | S | Via MEVD or custom instrumentation |
| FR-016 | Collect tool invocation Span | S | Function Calling / MCP |
| FR-017 | Collect serialization time | S | Request / response serialization |
| FR-018 | Collect retry chain | M | attempt, backoff, error type |
| FR-019 | Collect DNS/TCP/TLS events | S | Based on `HttpClient` diagnostic events |

### 3.3 Cost

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-020 | Record Input/Output tokens | M | From provider usage |
| FR-021 | Record Cached/Reasoning tokens | M | If provided by the provider |
| FR-022 | Built-in versioned price table | M | Cover at least OpenAI, Azure OpenAI, Anthropic |
| FR-023 | Support user price overrides | M | JSON/YAML file |
| FR-024 | Compute line-item and total cost | M | decimal precision, stored in USD |
| FR-025 | Mark unknown models | M | Do not guess, `cost.status=unknown` |
| FR-026 | Cache discount attribution | S | Cached input unit price |
| FR-027 | Batch / tiered pricing | S | tiered pricing, batch discount |
| FR-028 | Retry duplicate billing detection | M | Mark attempts that may be double-billed |
| FR-029 | Cost reconciliation view | S | Compare against provider billing CSV |
| FR-030 | Budgets and alerts | S | Thresholds by request / tenant / feature |

### 3.4 Streaming

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-031 | Compute TTFT | M | First non-empty delta |
| FR-032 | Compute inter-token latency (ITL) | M | P50/P95 |
| FR-033 | Compute Tokens/s | M | Output tokens / generation duration |
| FR-034 | Record cancellation and interruption | M | Distinguish user cancellation, timeout, provider error |
| FR-035 | Record partial response usage | S | Best effort, marked as estimated |

### 3.5 Storage and CLI

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-040 | SQLite default storage (library / in-app) | M | Query, aggregation, indexing; Studio data source |
| FR-041 | JSONL (CLI AOT default, fallback and interchange) | M | CLI default, AOT fallback, import/export, crash recovery |
| FR-042 | Data retention policy | M | Dual thresholds on time and size |
| FR-043 | CLI `runesvaner run` | M | Start the app and collect |
| FR-044 | CLI `runesvaner report` | M | Terminal summary output |
| FR-045 | CLI `runesvaner top` | S | Slow request / high cost ranking |
| FR-046 | CLI `runesvaner export` | M | JSONL/OTLP |
| FR-047 | CLI `runesvaner open` | M | Start the local Studio |
| FR-048 | Offline import | C | Import from OTLP/JSONL for analysis |

### 3.6 Local Studio

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-050 | Session list | M | Filter by time, status, model |
| FR-051 | Request waterfall | M | Segments, parallel lanes, estimated markers |
| FR-052 | Request detail panel | M | Attributes, tokens, cost, errors |
| FR-053 | Cost ledger page | M | Line items, dimension rollups, anomalies |
| FR-054 | Comparison view | S | Two requests / two versions |
| FR-055 | Keyboard navigation | S | Cmd/Ctrl+K, J/K, Enter |
| FR-056 | Export report | S | Single request HTML/JSON |
| FR-057 | Local token and loopback binding | M | Default `127.0.0.1` |
| FR-058 | Theme | C | Dark default, light optional |

### 3.7 Export and Interop

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-060 | OTLP export (optional package) | M | Trace + Metrics |
| FR-061 | GenAI semantic convention alignment | M | `gen_ai.*` fields |
| FR-062 | Langfuse compatibility validation | C | On demand, not committed |
| FR-063 | Phoenix compatibility validation | C | On demand, not committed |
| FR-064 | Prometheus metrics endpoint | C | Optional |

### 3.8 Sampling and Reliability

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-070 | Head sampling | M | Probability + rules |
| FR-071 | Tail sampling | S | Retain errors / slow / over-budget requests |
| FR-072 | Sampling-weighted statistics | S | Avoid distorting cost statistics |
| FR-073 | Collector circuit breaker | M | Degrade when the queue is full / persistence fails |
| FR-074 | Self-monitoring metrics | M | Queue length, drop count, persistence latency |

### 3.9 Privacy and Security

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-080 | Do not collect content by default | M | `ContentCapture.None` |
| FR-081 | Metadata mode | M | Length, hash, token estimate |
| FR-082 | Full content mode | S | Explicitly enabled, prominent warning |
| FR-083 | Sensitive header denylist | M | Authorization, api-key, cookie |
| FR-084 | Export redaction | S | Configurable rules |
| FR-085 | Local data permissions | M | File permissions 0600 / user directory |

### 3.10 AOT and Compatibility

| ID | Requirement | Priority | Notes |
| --- | --- | --- | --- |
| FR-090 | Core library `IsAotCompatible` | M | No AOT/Trim warnings |
| FR-091 | CLI Native AOT publish | M | Windows/Linux |
| FR-092 | JSON Source Generator | M | No reflection-based serialization |
| FR-093 | Configuration binding Source Generator | M | Avoid reflection-based binding |
| FR-094 | AOT smoke test | M | Every CI run |

---

## 4. Acceptance Criteria for Must Features

Uses Given / When / Then. `AC-xxx` can be mapped directly in tests.

### 4.1 FR-001 MEAI Middleware

- **AC-001** Given an `IChatClient` pipeline, When `UseRunesvaner()` is called, Then the existing call semantics and return values are unchanged.
- **AC-002** Given a streaming request, When the `IAsyncEnumerable` is enumerated, Then every chunk is returned in its original order and with its original content.
- **AC-003** Given the collector throws an exception, When the business request continues, Then the request succeeds and one self-monitoring error is recorded.
- **AC-004** Given no storage is configured, When it is called, Then the default local path is used and the request is not blocked.

### 4.2 FR-012 Latency Waterfall

- **AC-010** Given a completed Chat request, When the report is generated, Then it contains at least the six segments Queue, Connect, TLS, Upload, TTFT, Generation.
- **AC-011** Given a segment that cannot be measured precisely, When it is displayed, Then `estimated=true` and `measurement.method` is provided.
- **AC-012** Given segments overlap in parallel, When they are displayed, Then `overlap` is shown and normalization is not forced.
- **AC-013** Given the sum of segments differs from total duration by > 5%, When it is displayed, Then `unaccounted` is shown and the difference is marked.

### 4.3 FR-022 / FR-024 / FR-025 Cost

- **AC-020** Given a known provider, model, and price table version, When the request completes, Then input/output/cached/reasoning line items and the total are output.
- **AC-021** Given an unknown model, When the request completes, Then `cost.status=unknown`, the total cost is null, and no guess is made.
- **AC-022** Given the price table is updated, When a historical session is viewed, Then it is replayed using the `price_table_version` recorded by that session.
- **AC-023** Given amount computation, When stored and displayed, Then decimal is used, at least 6 decimal places are retained, and display is rounded to 4 decimal places.

### 4.4 FR-028 Retries

- **AC-030** Given the first request returns 429 and the second succeeds, When the bill is generated, Then `attempt=2` is recorded and the successful cost is counted once.
- **AC-031** Given the provider bills a failed request, When the response headers or error body indicate it, Then `billing.status=billed` is recorded.
- **AC-032** Given it cannot be determined whether a failed request was billed, When the bill is generated, Then `billing.status=unknown` is marked, excluded from the total, and surfaced in the report.

### 4.5 FR-031 Streaming

- **AC-040** Given a streaming response, When the first non-empty delta arrives, Then TTFT = the monotonic clock difference from request start to that delta.
- **AC-041** Given the streaming response completes, When the report is generated, Then the chunk count, ITL P50/P95, and Tokens/s are output.
- **AC-042** Given a user cancellation, When the stream closes, Then `cancelled=true` and the number of chunks received are recorded.
- **AC-043** Given the stream is interrupted, When an exception is thrown, Then the data already received is retained and the error type is recorded.

### 4.6 FR-040 Storage and FR-042 Retention

- **AC-050** Given a process crash, When JSONL is read after restart, Then fully written lines are readable and partial lines are safely ignored.
- **AC-051** Given the retention time or size is exceeded, When cleanup runs, Then the oldest sessions are deleted and the currently active session is not deleted.
- **AC-052** Given the disk is full, When a write fails, Then the collector circuit breaks and business requests are unaffected.

### 4.7 FR-050 / FR-051 Studio

- **AC-060** Given local session data, When Studio is opened, Then the first screen shows recent sessions and total cost.
- **AC-061** Given a request is selected, When the waterfall is opened, Then each segment shows its name, duration, share, and estimated marker.
- **AC-062** Given no data, When Studio is opened, Then an empty state is shown with the command for "how to generate the first session".
- **AC-063** Given the default configuration, When Studio is accessed, Then it binds only to `127.0.0.1` and requires a local token.

### 4.8 FR-080 Privacy

- **AC-070** Given the default configuration, When a request completes, Then storage contains no prompt/completion text.
- **AC-071** Given `ContentCapture.Full`, When it is enabled, Then the UI and logs show a prominent warning, and data is retained for 7 days by default.
- **AC-072** Given the request headers contain `Authorization`, When collection occurs, Then that header is dropped or redacted.

### 4.9 FR-090 / FR-091 AOT

- **AC-080** Given `dotnet publish -r linux-x64 /p:PublishAot=true`, When the sample is built, Then there are 0 AOT/Trim warnings.
- **AC-081** Given the AOT binary, When a smoke request runs, Then one complete session is generated with all fields present.
- **AC-082** Given the CLI AOT publish, When `runesvaner report` is executed, Then output is normal and there are no reflection exceptions.

---

## 5. Non-Functional Requirements

### 5.1 Performance

| ID | Requirement | Threshold |
| --- | --- | --- |
| NFR-001 | Incremental collection latency | P95 ≤ 1 ms (excluding provider) |
| NFR-002 | CPU overhead | ≤ 1% (sampling on, no export) |
| NFR-003 | Allocation | ≤ 2 KB/request (excluding provider and user objects) |
| NFR-004 | Persistence | Asynchronous, does not block the request thread |
| NFR-005 | Memory | Ring buffer upper bound configurable, default ≤ 16 MB |

### 5.2 Reliability

| ID | Requirement |
| --- | --- |
| NFR-010 | Collection failure must not cause business failure |
| NFR-011 | When the queue is full, drop the oldest events and count them |
| NFR-012 | When storage is corrupted, start up and prompt for repair |
| NFR-013 | Support graceful shutdown and flush |

### 5.3 Privacy and Security

| ID | Requirement |
| --- | --- |
| NFR-020 | Do not collect content by default |
| NFR-021 | Local UI binds to loopback only |
| NFR-022 | Local token is randomly generated and invalidated on process exit |
| NFR-023 | Redaction is available before export |
| NFR-024 | Do not record keys, cookies, or Authorization |

### 5.4 Compatibility

| ID | Requirement |
| --- | --- |
| NFR-030 | TFM: `net10.0`; evaluate `net11.0` |
| NFR-031 | MEAI: 10.x and 11.x |
| NFR-032 | OS: Windows 10/11 x64/arm64, Linux x64/arm64; macOS is a non-goal (D-15) |
| NFR-033 | Native AOT / Trimming |
| NFR-034 | No mandatory dependency on the OpenTelemetry SDK |

### 5.5 Usability and Accessibility

| ID | Requirement |
| --- | --- |
| NFR-040 | First experience ≤ 5 minutes |
| NFR-041 | Error messages include cause and next step |
| NFR-042 | WCAG 2.2 AA |
| NFR-043 | Key operations can be completed by keyboard |
| NFR-044 | Dark default, light optional; UI supports en-US / zh-Hans / de-DE |

### 5.6 Maintainability

| ID | Requirement |
| --- | --- |
| NFR-050 | Public APIs go through API Review |
| NFR-051 | SemVer 2.0 |
| NFR-052 | Experimental APIs use `[Experimental]` |
| NFR-053 | Every public package has XML documentation |
| NFR-054 | Benchmarks and AOT regression run in CI |

---

## 6. Data and Privacy Requirements

| Data category | Default | Optional | Retention |
| --- | --- | --- | --- |
| Time and duration | Collected | — | 7 days / 1 GB |
| Token usage | Collected | — | 7 days / 1 GB |
| Cost | Collected | — | 7 days / 1 GB |
| Provider / Model | Collected | — | 7 days / 1 GB |
| Tenant / User | Hashed by default | Plaintext (explicit) | 7 days / 1 GB |
| Prompt / Completion | Not collected | Metadata / full | Full mode: 7 days |
| Request headers | Denylist filtered | — | Not stored |
| Exception stack traces | Collected (optional) | — | 7 days / 1 GB |

---

## 7. Error Handling and Degradation

| Scenario | Behavior | User visibility |
| --- | --- | --- |
| Collector exception | Catch, count, continue | Studio self-monitoring page |
| Queue full | Drop oldest, count | Top banner warning |
| Persistence failure | Circuit break, retry after 30 seconds | Top banner warning |
| Unknown model price | Mark unknown | Cost column shows `—` + tooltip |
| Missing price table | Use built-in snapshot | Show version |
| Studio port in use | Automatically pick a free port | CLI prints URL |
| AOT-incompatible dependency | Build warning | Documentation lists alternatives |
| SQLite initialization failure | Fall back to JSONL and count | Health page warning |

---

## 8. Dependencies and Assumptions

| Dependency | Purpose | Risk |
| --- | --- | --- |
| Microsoft.Extensions.AI | Integration point | Fast version evolution; multi-version testing required |
| Microsoft.Extensions.VectorData | Vector retrieval | Still evolving |
| System.Text.Json | Serialization | Requires Source Generator |
| Microsoft.Data.Sqlite | SQLite | Native dependency; AOT must be validated |
| OpenTelemetry SDK | Optional export | Size and AOT cost |
| Price data | Cost computation | Source legality and freshness |

---

## 9. Release Criteria (Definition of Done)

A Must feature is complete only when:

1. Code + unit tests + integration tests.
2. Public APIs have XML documentation and examples.
3. No new AOT/Trim warnings.
4. No performance benchmark regression > 10%.
5. Privacy impact assessment completed.
6. Documentation updated (README / docs).
7. Changelog entry.

Additional requirements for 1.0:

1. Public API freeze and compatibility tests.
2. Security audit (dependencies, secrets, path traversal, DNS rebinding).
3. Windows/Linux dual-platform AOT smoke.
4. CLA process available.
5. Migration guide and troubleshooting handbook.

---

## 10. Decision Status

D-01–D-25 are all resolved; see `15-decision-log.md`. The implementation basis for this PRD:

- Main track: AI workload diagnostics first (D-01).
- Project name: Runesvaner (D-02).
- License / CLA: Apache-2.0 / ICLA+CCLA (D-03 / D-04).
- Target framework: `net10.0`, M3 evaluates `net11.0` (D-05).
- UI: React 19 + Redux Toolkit + RTK Query (D-06).
- Storage: library / in-app SQLite by default; CLI AOT defaults to JSONL; fallback on failure (D-07).
- Price table: built-in snapshot + user overrides, remote disabled (D-08).
- Content capture: `None` by default (D-09).
- Commercialization: none for now (D-10).
- OTel: zero dependency in the core, optional export (D-13).
- Sampling: 100% locally, 10% recommended in production (D-14).
- Platform: Windows/Linux (D-15).
- Languages: English / Simplified Chinese / Standard German (D-16).
| Whether to do AOT heap analysis | D-18 |