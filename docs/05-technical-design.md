# Technical Design: Runesvaner

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Engineering, architecture, security |
| Related documents | `03-prd.md`, `06-observability-model.md`, `07-api-design.md`, `08-native-aot-strategy.md` |

---

## 1. Design Goals

1. **Low overhead**: single-request P95 increment ≤ 1 ms, CPU ≤ 1%, allocation ≤ 2 KB.
2. **Non-intrusive to the application**: any failure in the collection pipeline must not fail the request.
3. **Explainable**: every segment is annotated with its measurement method and `estimated`.
4. **Composable**: the core has zero dependencies; OTel, SQLite, and Studio are all optional layers.
5. **AOT-safe**: no reflection, dynamic code, or runtime type loading on the core path.
6. **Local-first**: writes only to the local machine by default; captures no content by default.
7. **Evolvable**: both the data schema and the public API have versioning policies.

## 2. Key Constraints

| Constraint | Impact |
| --- | --- |
| Maintenance bandwidth of a personal project | Clear module boundaries; avoid spreading work across everything at once |
| MEAI version evolution | Integration layer is isolated; the core does not depend on MEAI types |
| AOT / Trim | No reflection-based serialization, dynamic DI, or runtime code generation |
| Streaming API | Wrappers must preserve original semantics, cancellation, and exception propagation |
| Multi-provider differences | Mark unknown when usage fields are missing; do not infer |
| Privacy | No content by default; hashing and redaction are completed before writing |

---

## 3. Architecture Overview

    +-----------------------------+
    |  Application / AI Pipeline  |
    |  IChatClient / IEmbedding   |
    +--------------+--------------+
                   |
                   v
    +-----------------------------+
    | Runesvaner.MicrosoftExtensionsAI
    |  ChatClientDecorator         |
    |  EmbeddingGeneratorDecorator |
    |  StreamingWrapper            |
    +--------------+--------------+
                   |
                   v
    +-----------------------------+
    | Runesvaner (Core)              |
    |  Recorder / Clock / Sampler  |
    |  Segmenter / CostEngine      |
    |  Redactor / RingBuffer       |
    |  SelfMonitor                 |
    +------+-----------+-----------+
           |           |
           v           v
    +-----------+  +------------------+
    | Storage   |  | Exporters        |
    | SQLite def|  | OTLP (optional)  |
    | JSONL fb  |  | Custom Sink      |
    +-----+-----+  +------------------+
          |
          v
    +-----------------------------+
    | Runesvaner.Cli / Studio        |
    |  read-only query + UI        |
    +-----------------------------+

### 3.1 Dependency Direction

| Layer | Allowed dependencies | Forbidden dependencies |
| --- | --- | --- |
| `Runesvaner.Abstractions` | BCL | Any third-party package |
| `Runesvaner` | Abstractions, DI Abstractions, Options | MEAI, OTel, SQLite |
| `Runesvaner.MicrosoftExtensionsAI` | Core, MEAI Abstractions | OTel, SQLite |
| `Runesvaner.Storage.*` | Core | MEAI, OTel |
| `Runesvaner.Exporters.*` | Core, OTel | MEAI, Storage |
| `Runesvaner.Cli` | Core, Storage, Studio host | MEAI (except samples) |

The core does not reference MEAI, which means integrating Semantic Kernel or other SDKs in the future will not require changes to the core.

---

## 4. Solution Structure

    Runesvaner.sln
    src/
      Runesvaner.Abstractions/
      Runesvaner/
      Runesvaner.MicrosoftExtensionsAI/
      Runesvaner.SemanticKernel/            # M3
      Runesvaner.VectorData/                # M3
      Runesvaner.Storage.Jsonl/            # fallback/interchange
      Runesvaner.Storage.Sqlite/            # default
      Runesvaner.Exporters.OpenTelemetry/   # M2
      Runesvaner.Cli/
      Runesvaner.Studio/                    # ASP.NET Core host + React build
    test/
      Runesvaner.UnitTests/
      Runesvaner.IntegrationTests/
      Runesvaner.AotSmokeTests/
      Runesvaner.Benchmarks/
      Runesvaner.ContractTests/             # MEAI version contract
    samples/
      Runesvaner.Sample.Console/
      Runesvaner.Sample.Rag/
      Runesvaner.Sample.Aot/
    docs/
    eng/
      build/
      ci/

### 4.1 Packages and Publishing

| Package | Contents | Dependencies |
| --- | --- | --- |
| `Runesvaner.Abstractions` | Public models, enums, interfaces | None |
| `Runesvaner` | Core collection and cost engine | Abstractions, Options |
| `Runesvaner.MicrosoftExtensionsAI` | MEAI middleware | Core, MEAI.Abstractions |
| `Runesvaner.Storage.Jsonl` | JSONL fallback and interchange | Core |
| `Runesvaner.Storage.Sqlite` | Default in-library/in-app storage and query | Core, Microsoft.Data.Sqlite |
| `Runesvaner.Exporters.OpenTelemetry` | OTLP export | Core, OTel SDK |
| `Runesvaner.Cli` | `dotnet tool`, AOT | Core, Storage.Sqlite (falls back to Jsonl on failure) |
| `Runesvaner.Studio` | Local UI host | ASP.NET Core, Storage |

---

## 5. Collection Model

### 5.1 Lifecycle of a Request

| Phase | Event | Recorded content |
| --- | --- | --- |
| Enter middleware | `request.start` | trace_id, operation, provider, model, start_ts |
| Establish connection | `segment` | connect, tls (from HttpClient diagnostic events, if available) |
| Upload complete | `segment` | upload |
| First byte | `event` | ttfb |
| First token | `event` | ttft (first non-empty delta) |
| Each chunk | `stream.chunk` | timestamp, delta length (content not stored) |
| Tool call | `segment` | tool name, duration, status |
| Vector search | `segment` | collection, top_k, duration |
| Completion | `request.end` | usage, cost, status, segments, error |
| Persistence | `storage.write` | asynchronous batch |

### 5.2 Segmentation Model

Segmentation is not "guessing"; it is layered:

1. **Measured segments**: start and end times can be obtained directly (local queueing, serialization, tools, vector search).
2. **Event-derived segments**: derived from `HttpClient` diagnostic events (connect, TLS, upload).
3. **Estimated segments**: can only be derived from the time difference between first byte and first token (provider queueing, network wait).
4. **Residual segments**: total elapsed time minus the explained intervals (`unaccounted`, `overlap`).

Each segment must carry:

- `kind`
- `start_offset_us`
- `duration_us`
- `estimated` (bool)
- `measurement` (`stopwatch` / `http_diagnostic` / `derived` / `residual`)
- `lane` (parallel grouping)

### 5.3 Time Sources

- Monotonic clock: `Stopwatch.GetTimestamp()`, converted to `TimeSpan` with `Stopwatch.GetElapsedTime`.
- Wall clock: `TimeProvider.GetUtcNow()`, used only for display and cross-process correlation.
- Testing: `IRunesvanerClock` abstraction allows injecting a fake clock.
- Do not call `DateTime.UtcNow` on the request path.

### 5.4 Streaming Wrapping

Streaming is the highest-risk area. Wrappers must:

1. Not buffer the entire stream; pass through chunk by chunk.
2. Not change the `CancellationToken`.
3. Not swallow exceptions; record them and rethrow unchanged.
4. Flush on `DisposeAsync` and record `cancelled` (if not completed).
5. Use `[EnumeratorCancellation]` semantics for `IAsyncEnumerable` so that cancellation does not become ineffective.
6. Record the time of the first non-empty delta; empty deltas do not count toward TTFT.
7. Record the time of the last chunk; do not rely on `usage` necessarily arriving last.

Pseudocode (design sketch):

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var state = _recorder.BeginStreaming(...);
        try
        {
            await foreach (var update in _inner.GetStreamingResponseAsync(messages, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                state.OnChunk(update);
                yield return update;
            }
            state.OnCompleted();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            state.OnCancelled();
            throw;
        }
        catch (Exception ex)
        {
            state.OnError(ex);
            throw;
        }
        finally
        {
            await state.FlushAsync().ConfigureAwait(false);
        }
    }

### 5.5 Relationship with OTel

- Runesvaner uses `ActivitySource` and `Meter` itself, with the name `Runesvaner.AI`.
- If the user also enables `UseOpenTelemetry()`, two sets of spans will be produced. Duplication must be avoided:
  - Default: Runesvaner only emits diagnostic events and does not create additional Activities; output is via `IRunesvanerSink`.
  - Optional: `ActivitySource` mode, for OTel users; documentation must then explain the span nesting relationship.
- The OTLP export package converts Runesvaner events into OTel spans/metrics to avoid double export.

---

## 6. Low-Overhead Strategy

| Strategy | Description |
| --- | --- |
| Sampling first | Unsampled requests perform only one probability check and create no record |
| Struct-first | `Segment`, `Usage`, `Cost` use `readonly struct` |
| Pre-allocation | `Segment[16]` per request, grows only on overflow |
| Object pooling | Introduce `ArrayPool` only when benchmarks prove it necessary |
| Asynchronous persistence | `Channel<Event>` + background batching |
| Source-generated JSON | `JsonSerializerContext` to avoid reflection |
| String reuse | provider/model use constants or pooling |
| Avoid LINQ | LINQ is prohibited on hot paths |
| Avoid logging | Hot paths do not call `ILogger`; count failures |
| Content off | Do not copy Prompt/Completion by default |
| Circuit breaking | Degrade quickly when the queue is full or persistence fails |

### 6.1 Overhead Budget

| Operation | Target |
| --- | ---: |
| Sampling check | < 50 ns |
| Create request record | < 500 ns |
| Per-segment write | < 100 ns |
| Per-chunk record | < 200 ns |
| Cost calculation | < 5 µs |
| Total increment P95 | < 1 ms |

---

## 7. Cost Engine

### 7.1 Input

- `provider`, `request_model`, `response_model`
- `usage`: input, output, cached_input, reasoning, image, audio
- `price_table_version`
- `batch`, `tier`, `cache` flags

### 7.2 Price Table

    {
      "version": "2026-11-01",
      "currency": "USD",
      "source": "https://example.com/pricing",
      "retrieved_at": "2026-11-01T00:00:00Z",
      "entries": [
        {
          "provider": "openai",
          "model": "gpt-5",
          "aliases": ["gpt-5-2026-08"],
          "input_per_million": 1.25,
          "cached_input_per_million": 0.125,
          "output_per_million": 10.00,
          "reasoning_per_million": 10.00,
          "tiers": []
        }
      ]
    }

Rules:

- Use `decimal` for storage and calculation, not `double`.
- Units are unified as "per million tokens".
- Unknown model: `CostStatus.Unknown`; do not guess.
- The price table version is recorded with each request; historical replay uses the version in effect at that time.
- Alias resolution: request model → response model → alias → unknown.
- Tiered pricing: if `tiers` is non-empty, accumulate by interval.
- Batch discount: `discount` field, default 0.
- Multimodal: calculated in the provider's image/audio units; unknown if it cannot be determined.

### 7.3 Calculation Order

1. Resolve the price entry.
2. Tier the input tokens.
3. Split cached / non-cached.
4. Output tokens.
5. Reasoning tokens (if they overlap with output, follow the provider definition; do not double-count).
6. Multimodal add-ons.
7. Batch discount.
8. Round to 6 decimal places (`MidpointRounding.ToEven`, pending confirmation in D-08).
9. Generate `CostBreakdown` and `CostStatus`.

### 7.4 Retries and Billing

| Scenario | Handling |
| --- | --- |
| Success after 429 | Bill only the successful attempt; mark failed attempts `not_billed` or `unknown` |
| Success after 5xx | Same as above; mark `billed` if the response indicates billing |
| Success after timeout | Possible double billing; mark `possible_double_billing` |
| User cancellation | Mark `partial` if the provider has already generated partial output |
| Stream interruption | Best-effort read of usage; unknown if missing |

---

## 8. Sampling

### 8.1 Head Sampling

- 100% locally by default; 10% recommended in production (documentation recommendation, not enforced).
- Rule: probability + always retain errors/slow requests (requires tail sampling).
- Record `sampling.probability` and weight during aggregation.

### 8.2 Tail Sampling

- Buffer lightweight records for one trace until it ends.
- Retention conditions: errors, P95 above threshold, cost above budget, explicit marking.
- Risks: memory and latency; disabled by default, introduced in M3.

### 8.3 Statistical Bias

- Cost aggregation must use weighting: `sum(cost / probability)`.
- The UI displays "sampling mode" and "confidence".
- When not extrapolating cost for unsampled requests, explicitly label "sampled requests only".

---

## 9. Storage

SQLite is the default for libraries/in-app; JSONL is the default for CLI AOT; fall back when SQLite fails; the two are interchangeable.

### 9.1 JSONL (CLI AOT default, fallback, and interchange)

Directory:

    .runesvaner/
      sessions/
        20261125T143012Z-7f3a.jsonl
      pricing/
        builtin-2026-11-01.json
        user.json
      config.json
      studio.token

Line format:

    {"schema":"runesvaner.event.v1","session_id":"...","event_id":"...","type":"request.end","timestamp":"...","monotonic_ns":123,"payload":{...}}

Writing:

- Append mode, UTF-8, no BOM.
- One complete JSON per line; flush after write (configurable).
- On read, ignore the last incomplete line.
- File rolling: single file ≤ 64 MB or 100k events.

### 9.2 SQLite (default for libraries/in-app)

File: `.runesvaner/runesvaner.db`, WAL mode.

Tables:

| Table | Main columns |
| --- | --- |
| `sessions` | id, started_at, app, environment, version |
| `requests` | id, session_id, trace_id, operation, provider, model, started_at, duration_us, ttft_us, status |
| `segments` | request_id, kind, start_offset_us, duration_us, estimated, lane |
| `usage` | request_id, input, output, cached, reasoning, image, audio |
| `costs` | request_id, currency, input_cost, output_cost, cached_cost, reasoning_cost, total, status, price_version |
| `tags` | request_id, key, value |
| `errors` | request_id, type, message, stack_hash |
| `budgets` | scope, window, limit, spent |

Indexes: `started_at`, `model`, `feature`, `tenant_hash`, `status`, `cost_total`.

### 9.3 Consistency

- SQLite for libraries/in-app is the default source of truth; JSONL is the CLI AOT default; the two are interchangeable.
- Import: `runesvaner index --path .runesvaner/sessions` builds a SQLite index from JSONL.
- CLI: `runesvaner run` uses JSONL by default; `--storage sqlite` explicitly selects SQLite.
- Fallback: if SQLite initialization fails under any host, write JSONL and record `storage.fallback=jsonl`.
- Crash recovery: SQLite uses WAL; JSONL ignores the last incomplete line on read.

---

## 10. Studio Technical Design

- Host: ASP.NET Core Minimal API (framework reference, no additional dependencies).
- Frontend: React 19 + TypeScript + Vite + Redux Toolkit + RTK Query (resolved by D-06).
- Static assets: embedded into the CLI after build, or placed under `wwwroot`.
- Binding: `127.0.0.1`, random port; `Host` header validation; CORS disabled.
- Authentication: generate a 32-byte token at startup, URL `?token=`; API uses `Authorization: Bearer`.
- Read-only: does not write session files; Settings are written to a separate configuration file.
- API:
  - `GET /api/sessions`
  - `GET /api/requests?session=&model=&status=&minCost=`
  - `GET /api/requests/{id}`
  - `GET /api/costs/summary`
  - `GET /api/health`
  - `POST /api/export` (optional)
- Large result sets: pagination + cursor; default 100 rows.
- Charts: uPlot (time series) + custom waterfall (SVG/Canvas).
- Performance: single-page load ≤ 1 s with 100k requests (pagination and indexes).

---

## 11. CLI Technical Design

| Command | Purpose | Exit code |
| --- | --- | --- |
| `runesvaner run -- <cmd>` | Start the application and collect | Child process exit code |
| `runesvaner report` | Terminal summary | 0/3 |
| `runesvaner top` | Slow/expensive ranking | 0/3 |
| `runesvaner export` | JSONL/OTLP | 0 |
| `runesvaner open` | Launch Studio | 0 |
| `runesvaner doctor` | Environment and configuration check | 0/2 |
| `runesvaner pricing update` | Update the price table (requires explicit action) | 0/2 |

- Argument parsing: a self-written lightweight parser to avoid AOT risks in `System.CommandLine`.
- Output: human-readable tables + `--json`.
- Color: enabled only on TTY; `NO_COLOR` supported.
- Global configuration: `.runesvaner/config.json` + environment variable `RUNESVANER_*`.

---

## 12. Error Handling and Circuit Breaking

| Failure | Detection | Action |
| --- | --- | --- |
| Collector exception | try/catch | Count, continue |
| Queue full | `Channel.Writer.TryWrite` fails | Drop the oldest, count |
| Persistence failure | IO exception | Circuit break for 30 s, degrade to memory |
| Serialization failure | Exception | Drop event, count |
| Corrupted price table | Validation failure | Use built-in snapshot, warn |
| Studio port in use | bind fails | Try the next port |
| Clock anomaly | impossible (monotonic) | Record `invalid_timing` |

Principle: **A collector error must never become a business error.**

---

## 13. Configuration

### 13.1 Priority

1. Code configuration (highest)
2. Environment variables `RUNESVANER_*`
3. `.runesvaner/config.json`
4. Defaults

### 13.2 AOT Configuration Binding

- Code configuration is preferred.
- JSON configuration uses the Source Generator of `Microsoft.Extensions.Configuration.Binder` (`EnableConfigurationBindingGenerator`).
- Avoid the reflection path of `IConfiguration.Get<T>()`.
- All options use simple types, arrays, and dictionaries; avoid polymorphism.

---

## 14. AOT Strategy Summary

See `08-native-aot-strategy.md` for the full version.

- All libraries `<IsAotCompatible>true</IsAotCompatible>`.
- Prohibited: `Type.GetType`, `Activator.CreateInstance`, `Assembly.Load`, `Reflection.Emit`, `MakeGenericType`.
- JSON: `JsonSerializerContext`.
- Configuration: Source Generator.
- DI: explicit registration; no assembly scanning.
- CLI: `PublishAot=true`, `TrimMode=full`.
- SQLite: default storage for libraries/in-app; JSONL default for CLI AOT; AOT must validate native `e_sqlite3`; automatic fallback on failure.
- OTel: optional package; if AOT warns, the documentation provides an alternative export path (JSONL → external collector).

---

## 15. Testability

| Concern | Design |
| --- | --- |
| Time | `IRunesvanerClock`, fake clock |
| Random sampling | `ISampler`, injectable |
| Storage failure | `FaultInjectingStore` |
| Streaming semantics | Custom `IAsyncEnumerable` test source |
| Pricing | Fixed price table + golden tests |
| AOT | Post-publish smoke |
| Performance | BenchmarkDotNet + CI gate |

---

## 16. Technical Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| MEAI API changes | Integration layer breakage | Multi-version contract tests; adapter isolation |
| Streaming wrapper breaks cancellation | Business bug | Dedicated tests; minimal wrapper |
| Segment estimates not trusted | Users distrust results | Mark estimated; document explanation |
| Price table errors | Incorrect cost | Versioning + user overrides + verification view |
| SQLite failure under AOT | Default library storage unavailable | Automatic fallback to JSONL; JSONL default for CLI; AOT smoke |
| Duplicate OTel spans | Duplicate data | Do not create Activities by default; explicit mode |
| Overhead exceeds budget | Users remove it | Benchmark gates; sampling; circuit breaking |
| Scope creep | Cannot deliver | Non-goals list; milestone exit criteria |

---

## 17. Decision Status

D-01–D-25 are all resolved; see `15-decision-log.md`. Directly related to this design:

- D-05: `net10.0`, with `net11.0` evaluated in M3.
- D-06: React 19 + Redux Toolkit + RTK Query.
- D-07: SQLite default for libraries/in-app; JSONL default for CLI AOT; fallback on failure.
- D-08: built-in price table + user overrides; decimal + ToEven; USD.
- D-13: core zero dependencies, OTLP optional.
- D-14: 100% locally, 10% recommended in production; tail sampling in M3.
- D-15: Windows/Linux; macOS is a non-goal.