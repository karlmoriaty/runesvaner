# Testing Strategy and Performance Benchmarks

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Engineering, CI, contributors |
| Related documents | `05-technical-design.md`, `08-native-aot-strategy.md`, `11-roadmap.md` |

---

## 1. Testing Goals

1. **Correctness**: segmentation, usage, cost, and streaming semantics match the contract.
2. **Non-interference**: collection failure, storage failure, and export failure do not affect the business.
3. **Low overhead**: performance thresholds are quantifiable and regressible.
4. **AOT availability**: verified before every release.
5. **Privacy**: no content by default; export can be redacted.
6. **Maintainability**: tests are fast, stable, and diagnosable.

---

## 2. Test Pyramid

| Layer | Proportion | Content | Run frequency |
| --- | --- | --- | --- |
| Unit tests | 70% | Pure functions, cost, segmentation, sampling, serialization | Every PR |
| Integration tests | 20% | MEAI pipeline, SQLite, JSONL fallback, CLI, Studio API | Every PR |
| Contract tests | 5% | MEAI 10.x/11.x, OTel mapping | Daily + release |
| AOT/Trim | 3% | Publish + smoke | Every release |
| Benchmarks | 2% | BenchmarkDotNet | Every PR (threshold) |
| Manual/exploratory | — | UI, real provider | Milestones |

---

## 3. Unit Tests

### 3.1 Coverage Scope

| Module | Key cases |
| --- | --- |
| Segmentation | Start/end offsets, parallel lanes, residual, estimated |
| Cost | Cache, reasoning, tiered, batch, unknown, precision, rounding |
| Sampling | Probability boundaries, weighting, rules |
| Clock | Monotonic, fake clock, overflow |
| Serialization | Schema, unknown fields, missing fields, oversized fields |
| Redaction | Hashing, header blacklist, paths |
| Retention | Time/size thresholds, active session protection |
| Circuit breaking | Queue full, IO failure, recovery |
| Streaming state machine | First token, empty delta, cancellation, exception, dispose |

### 3.2 Assertion Style

- Use xUnit v3 (pending confirmation by D-17).
- Assertion library TBD: prefer the built-in `Assert` plus custom extensions to reduce dependencies; if FluentAssertions is used, the license change must be considered.
- Do not use a snapshot library as the sole assertion; assert key fields explicitly.
- Floating point/money: use `decimal` for exact comparison; use tolerance for time.

### 3.3 Test Data

- Price table: fixed JSON, version `test-1`.
- Request: constructor plus builder, avoiding huge anonymous objects.
- Streaming: custom `IAsyncEnumerable` with controllable delay, exceptions, and cancellation.
- Privacy: fixtures containing fake keys and fake PII, verifying that nothing is written to disk.

---

## 4. Integration Tests

### 4.1 MEAI Pipeline

| Case | Verification |
| --- | --- |
| Non-streaming Chat | Return value, record fields, no duplicate Span |
| Streaming Chat | Chunk order, content, TTFT, cancellation |
| Embedding | Batch size, usage, dimensions |
| Middleware order | `UseRunesvaner().UseOpenTelemetry()` and the reverse order |
| Exception | Record when the inner call throws and rethrow unchanged |
| Dispose | Inner is disposed exactly once |
| GetService | Passthrough is correct |

### 4.2 Storage

| Case | Verification |
| --- | --- |
| Library SQLite default write | Transactions, WAL, query consistency |
| Library SQLite initialization failure | Automatic fallback to JSONL, reason recorded |
| CLI default JSONL | Complete lines, readable, ordered |
| JSONL fallback append | Complete lines, readable, ordered |
| Crash simulation | SQLite WAL recovery; JSONL half-line ignored |
| Concurrent writes | Single writer, no interleaving |
| Retention cleanup | Delete oldest, do not delete active |
| Disk full | Circuit breaking, business succeeds |
| JSONL import into SQLite | Consistent with source data, rebuildable |

### 4.3 CLI

| Case | Verification |
| --- | --- |
| `report` | Output fields, exit code |
| `report --json` | Schema compatibility |
| `top` | Sorting, filtering |
| `export` | File format, redaction |
| `doctor` | Check items, error messages |
| No data | Exit code 3, actionable hint |

### 4.4 Studio API

| Case | Verification |
| --- | --- |
| No token | 401 |
| Wrong token | 401 |
| Correct token | 200 |
| Non-localhost Host | 400 |
| Pagination | Limit, cursor |
| Sort injection | Whitelist rejection |
| XSS fixture | Escaped |
| Empty data | Empty state |

---

## 5. Contract Tests

### 5.1 MEAI Version Matrix

| Version line | Status |
| --- | --- |
| 10.x (LTS line) | Required |
| 11.x (latest) | Required |
| Future preview | Best effort |

Test content:

- All `IChatClient` method signatures are available.
- `ChatResponseUpdate` structure.
- `IEmbeddingGenerator` generic constraints.
- `ChatClientBuilder` extension methods.
- `GetService` behavior.

Strategy:

- A separate `Runesvaner.ContractTests` project.
- Multi-version package references (via conditional compilation or separate projects).
- On failure, adapt first; do not require users to upgrade.

### 5.2 OTel Mapping

- Use fixed Runesvaner events, export as OTLP, and validate the fields.
- Bind to the GenAI semconv version; review on upgrade.
- Do not treat Development fields as a stable contract.

---

## 6. AOT and Trim Tests

See `08-native-aot-strategy.md`. Additional:

| Test | Content |
| --- | --- |
| Build | `PublishAot` with 0 warnings |
| Run | Non-streaming, streaming, error, cancellation |
| Fields | Session schema complete |
| Size | Record the delta |
| Startup | Median cold start |
| Dependencies | SQLite/OTel optional matrix |

---

## 7. Performance Benchmarks

### 7.1 Tooling

- BenchmarkDotNet (Release, Server GC? depending on scenario).
- Fixed hardware: CI runners may be noisy; use a relative baseline plus tolerance for thresholds.
- Local benchmarks for diagnosis, CI for regression.

### 7.2 Scenarios

| Scenario | Description | Metrics |
| --- | --- | --- |
| B01 Sampling off | `IsEnabled` check only | ns/op, allocations |
| B02 Sampling on, no storage | Record construction + segmentation | µs/op, allocations |
| B03 Sampling on, library SQLite | Full path (async disk write) | µs/op, P95 |
| B03b CLI default JSONL | CLI AOT path | µs/op, P95 |
| B04 Streaming 100 chunks | Record per chunk | µs/chunk |
| B05 Cost calculation | Known/unknown/tiered | µs/op |
| B06 10k request batch | Throughput | req/s, memory |
| B07 Studio query | 10k/100k requests | ms/query |

### 7.3 Initial Thresholds

| Metric | M1 | M2 | 1.0 |
| --- | --- | --- | --- |
| B01 | ≤ 100 ns | ≤ 50 ns | ≤ 50 ns |
| B02 | ≤ 500 µs | ≤ 200 µs | ≤ 100 µs |
| B03 P95 | ≤ 2 ms | ≤ 1 ms | ≤ 0.5 ms |
| B04 | ≤ 2 µs | ≤ 1 µs | ≤ 1 µs |
| CPU overhead | ≤ 2% | ≤ 1% | ≤ 1% |
| Allocations/request | ≤ 8 KB | ≤ 4 KB | ≤ 2 KB |

> Thresholds are engineering targets, not marketing promises; M1 is calibrated with real measurements. If a target cannot be met, reduce allocations and strings first rather than removing features.

### 7.4 Regression Strategy

- Run B01–B05 on every PR.
- Compare against the `main` baseline; exceeding the threshold → fail or require an explanation.
- Noise handling: at least 3 runs, take the median; use relative values for CI runner differences.
- Archive benchmark results and generate trend charts (M3).

### 7.5 Benchmark Ethics

- Do not hide overhead: report P50/P95/P99, allocations, and GC together.
- Do not use only "no-op" scenarios.
- Do not count provider network time as Runesvaner overhead.
- Publish benchmark code and data.

---

## 8. Streaming-Specific Tests

| Case | Expectation |
| --- | --- |
| Normal 100 chunks | Order, content, TTFT, TPS |
| First chunk empty | TTFT not counted; wait for the first non-empty chunk |
| Exception mid-stream | Keep what was received, rethrow unchanged |
| User cancellation | `cancelled=true`, no throw |
| Timeout | `timeout`, keep partial data |
| Slow consumer | No read-ahead, no blocking |
| Early dispose | Flush, record |
| Multiple enumeration | Consistent with inner behavior (usually allowed only once) |

---

## 9. Privacy and Security Tests

| Case | Expectation |
| --- | --- |
| Default configuration | No Prompt/Completion |
| Full configuration | Content present, warning shown, retained 7 days |
| Authorization header | Not written to disk |
| Path traversal | Rejected |
| Symbolic link | Write rejected |
| Malicious JSONL | Safe parsing, depth limited |
| Studio without token | 401 |
| Studio foreign Host | 400 |
| XSS Prompt | Escaped |
| Export redaction | Sensitive fields removed |

---

## 10. Test Environments

| Environment | Purpose |
| --- | --- |
| Local | Fast unit/integration |
| GitHub Actions | Full PR suite + Windows/Linux |
| Nightly | Contracts, long benchmarks, fuzzing |
| Pre-release | AOT, security, manual UI |

RID scope: `win-x64`, `linux-x64`; `linux-arm64` optional; `win-arm64` not a target (D-24).

### 10.1 Dependency Simulation

- Do not call a real LLM provider.
- Use a local `HttpMessageHandler` mock or ASP.NET Core TestServer.
- The streaming mock supports delay, chunking, exceptions, and cancellation.
- The price table uses a fixed fixture.

### 10.2 Datasets

- Synthetic data: 10 / 10k / 100k requests.
- Includes: normal, slow, error, retry, cache, unknown price, over budget.
- No real user data; if real data is required, it must be redacted and authorized.

---

## 11. Coverage

| Scope | Target |
| --- | --- |
| Core (segmentation, cost, sampling) | ≥ 90% lines + branches |
| Integration layer | ≥ 80% |
| CLI | ≥ 70% |
| Studio API | ≥ 80% |
| UI | Critical-path E2E (M3) |

Coverage is a guardrail, not a goal; do not write meaningless tests for the numbers.

---

## 12. Pre-Release Checklist

1. Unit + integration + contract tests all green.
2. AOT Windows/Linux smoke.
3. No benchmark regression.
4. Privacy tests pass.
5. Studio security tests pass.
6. Dependency vulnerability scan shows no high severity.
7. Documentation and changelog updated.
8. Manually verify the "30-second experience".

---

## 13. Decision Status

D-01–D-25 are all decided; see `15-decision-log.md` and `19-open-implementation-decisions.md`. Directly relevant to this document:

- D-07: SQLite by default in the library; JSONL by default in CLI AOT; fallback on failure.
- D-17: xUnit v3; MEAI 10.x/11.x; LTS + latest STS.
- D-15: Windows/Linux runners; macOS not a target.
- D-24: `win-arm64` not a target; `linux-arm64` optional.
- D-14: 100% locally, 10% recommended in production; tail sampling in M3.
- D-06: UI E2E uses Playwright (M3).