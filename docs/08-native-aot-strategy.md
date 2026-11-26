# Native AOT and Trimming Strategy

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Engineering, release, platform teams |
| Related documents | `05-technical-design.md`, `09-security-privacy.md`, `12-testing-and-benchmarks.md` |

---

## 1. Why AOT is a first-class constraint

Runesvaner's primary domain is AI workload diagnostics, not AOT diagnostics. However, AOT must be a **hard constraint** for three reasons:

1. **Real user scenarios exist**: an increasing number of .NET AI services use Native AOT to reduce cold start and memory footprint.
2. **Traditional approaches break or get heavier**: some profilers rely on DAC/ClrMD; the OTel SDK carries size and compatibility costs under AOT.
3. **Constraints improve quality**: a core library with no reflection and no dynamic code is usually smaller, faster, and more predictable, benefiting all users.

> Important fact: Native AOT has supported EventPipe since .NET 8 (requires `<EventSourceSupport>true</EventSourceSupport>`), and `dotnet-trace` / `dotnet-counters` are available; however, **managed heap analysis is not supported**. Runesvaner does not rely on EventPipe and does not perform heap analysis, so it is not affected by these limitations.

---

## 2. Capability boundary statement

Runesvaner **provides** under Native AOT:

- In-process AI request latency segmentation.
- Token usage and cost calculation.
- Streaming TTFT / ITL / TPS.
- Local SQLite store as default storage (falls back to JSONL on AOT failure) and CLI reports.
- Optional OTLP export (subject to the AOT status of the OTel SDK).

Runesvaner **does not provide** under Native AOT:

- Managed heap object graphs, allocation sampling, GC generation analysis.
- CPU sampling profiler.
- Dynamic attach to a running process.
- Runtime method JIT analysis (AOT has no JIT).
- Reflection-based automatic discovery.

These boundaries must be explicit in the README and documentation to prevent user misunderstanding.

---

## 3. Core design constraints

### 3.1 Prohibited list

| Prohibited item | Reason | Replacement |
| --- | --- | --- |
| `Type.GetType` / `Assembly.GetType` | Depends on runtime type resolution | Explicit registration |
| `Activator.CreateInstance` | Reflective construction | Factory delegate |
| `Assembly.Load` / `LoadFrom` | AOT does not support dynamic loading | Compile-time references |
| `Reflection.Emit` | No runtime code generation | Source generation |
| `MakeGenericType` / `MakeGenericMethod` | AOT generic instantiation is restricted | Closed generics / explicit implementations |
| `JsonSerializer` reflection mode | Trim removes type information | `JsonSerializerContext` |
| `IConfiguration.Get<T>()` reflection binding | Same as above | Source-generated binder |
| Assembly-scanning DI | Reflection + trimming risk | Explicit `AddRunesvaner()` |
| `dynamic` | Runtime binding | Strong typing |
| `System.Linq.Expressions` compilation | Interpreted under AOT, slow and possibly unsupported | Hand-written code |
| Dynamic proxies (DispatchProxy/Castle) | Runtime code generation | Hand-written decorators |
| `Enum.Parse` on hot paths | Reflection/strings | Explicit mapping |
| `string.Format` on hot paths | Allocations | `StringBuilder` / interpolated string handlers |
| Depending on `AppDomain` | Not supported under AOT | None |

### 3.2 Allowed list

| Capability | Usage |
| --- | --- |
| `System.Text.Json` Source Generator | All JSON serialization |
| `Microsoft.Extensions.Configuration.Binder` Source Generator | Configuration binding |
| `FrozenDictionary` / `FrozenSet` | Price tables, mapping tables |
| `SearchValues<char>` | Text scanning |
| `System.Diagnostics.Metrics` | Metrics |
| `ActivitySource` | Optional tracing |
| `Stopwatch.GetTimestamp` | Monotonic clock |
| `Channel<T>` | Asynchronous queues |
| `ValueTask` | Low-allocation async |
| `ArrayPool<T>` | Only when benchmarks prove it necessary |
| `UnsafeAccessor` | Use with caution; only for unavoidable BCL internals access, requires separate review |

### 3.3 On `UnsafeAccessor`

Prohibited by default. If it must be used in the future:

- Must have an ADR.
- Must pass the AOT smoke test.
- Must accept the risk that "future runtimes may break it".
- Must not be used for business logic, only for diagnostic infrastructure.

---

## 4. Package-level AOT matrix

| Package | AOT goal | Verification | Notes |
| --- | --- | --- | --- |
| `Runesvaner.Abstractions` | Fully compatible | Build + smoke | Zero dependencies, BCL only |
| `Runesvaner` | Fully compatible | Build + smoke + benchmarks | Core collection and cost |
| `Runesvaner.MicrosoftExtensionsAI` | Fully compatible | Build + smoke | Decorator, no reflection |
| `Runesvaner.Storage.Jsonl` | Fully compatible | Build + smoke | Pure BCL I/O; AOT fallback path |
| `Runesvaner.Storage.Sqlite` | Default in libraries/apps; needs verification | Standalone smoke + fallback tests | Depends on `Microsoft.Data.Sqlite` and native `e_sqlite3`; affected by `dotnet/efcore#36068` |
| `Runesvaner.Exporters.OpenTelemetry` | Conditionally compatible | Standalone smoke + size | Depends on OTel SDK version |
| `Runesvaner.Cli` | Fully compatible | `PublishAot` + run | Default JSONL; `--storage sqlite` optional; fallback on failure |
| `Runesvaner.Studio` | Not required | — | Local development tool, may be framework-dependent |

### 4.1 Special handling for SQLite

Risks:

- `Microsoft.Data.Sqlite` depends on the native `e_sqlite3`, and AOT publishing requires correct native assets.
- Packaging and symbols for different RIDs may have issues.
- Trim may remove certain metadata from the SQLite provider.

Strategy:

1. Default to SQLite in libraries/apps; default to JSONL when the CLI is published with Native AOT.
2. SQLite AOT compatibility is affected by `dotnet/efcore#36068`: `e_sqlite3` may not be found, and the project has not enabled `IsAotCompatible`.
3. With CLI `--storage sqlite`, attempt to initialize SQLite; on failure automatically fall back to JSONL and record the reason.
4. The library also falls back in AOT hosts; business is not blocked.
5. M1 verification workaround: native asset copy, `SQLitePCLRaw` initialization, WAL, queries; results recorded in an ADR.
6. M2 evaluates a framework-dependent CLI (default SQLite) as a supplementary release.
7. Studio can read both SQLite and JSONL (non-AOT).

### 4.2 Special handling for OpenTelemetry

Risks:

- The OTel SDK may produce Trim warnings under AOT.
- Notable size increase (the community once reduced about 734 KB by removing Regex).
- Different exporters (OTLP/gRPC, HTTP) have different AOT support.

Strategy:

1. The core does not depend on OTel.
2. The Exporter package has an independent version line, with AOT status labeled separately.
3. Provide a "JSONL → external Collector" alternative path.
4. AOT users who only need local diagnostics do not reference the Exporter.

---

## 5. Build configuration

### 5.1 Library projects

    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <IsAotCompatible>true</IsAotCompatible>
      <EnableAotAnalyzer>true</EnableAotAnalyzer>
      <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
      <EnableSingleFileAnalyzer>true</EnableSingleFileAnalyzer>
      <VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>
      <TrimmerSingleWarn>false</TrimmerSingleWarn>
      <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
      <WarningsAsErrors>IL2026;IL2067;IL2070;IL2072;IL2075;IL3050;IL3051;IL3053;IL3058</WarningsAsErrors>
    </PropertyGroup>

Notes:

- `VerifyReferenceAotCompatibility` is the IL3058 check introduced in .NET 10; it may produce noise for third-party dependencies and can be enabled in a separate project.
- `TrimmerSingleWarn=false` ensures every warning is visible.
- Promote IL warnings to errors to prevent regressions.

### 5.2 CLI project

    <PropertyGroup>
      <OutputType>Exe</OutputType>
      <TargetFramework>net10.0</TargetFramework>
      <PublishAot>true</PublishAot>
      <TrimMode>full</TrimMode>
      <InvariantGlobalization>false</InvariantGlobalization>
      <UseSystemResourceKeys>false</UseSystemResourceKeys>
      <StackTraceSupport>true</StackTraceSupport>
      <EventSourceSupport>false</EventSourceSupport>
      <MetadataUpdaterSupport>false</MetadataUpdaterSupport>
    </PropertyGroup>

Notes:

- `InvariantGlobalization=false`: keeps Chinese and localization; the size increase must be measured.
- `EventSourceSupport=false`: Runesvaner does not depend on EventPipe; if OTel/EventSource is needed, evaluate separately.
- `StackTraceSupport=true`: the diagnostic tool itself needs readable stack traces.
- The CLI defaults to JSONL and does not reference SQLite; `--storage sqlite` requires an optional package or a framework-dependent build.

### 5.3 Sample AOT project

    <PropertyGroup>
      <PublishAot>true</PublishAot>
      <TrimMode>full</TrimMode>
      <IsAotCompatible>true</IsAotCompatible>
    </PropertyGroup>

The sample must cover:

- Non-streaming chat
- Streaming chat
- Embedding
- Error path
- Cancellation path

---

## 6. Verification pipeline

### 6.1 CI matrix

| Dimension | Value |
| --- | --- |
| OS | `ubuntu-latest`, `windows-latest` |
| RID | `linux-x64`, `win-x64`; `linux-arm64` optional; `win-arm64` non-target (D-24) |
| Configuration | Release |
| Projects | Sample.Aot (library, SQLite default), CLI (JSONL default) |
| Checks | Build warnings, run smoke, output schema validation, size recording, SQLite AOT attempt recording |

### 6.2 Smoke content

1. Start the AOT binary.
2. Perform one mock Chat request (local HTTP mock, no real provider).
3. Generate one session file.
4. Read it with `runesvaner report --json`.
5. Validate key fields: operation, duration, segments, usage, cost.
6. Perform one streaming request, validate TTFT and chunk counts.
7. Perform one error request, validate error recording.
8. CLI default JSONL smoke; library SQLite smoke (if AOT is available).
9. Exit code is 0.

### 6.3 Failure criteria

- Any IL2026/IL3050 or similar warning → failure.
- Missing smoke fields → failure.
- Startup time or size exceeding the baseline by 20% → warning; exceeding 50% → failure (thresholds pending benchmark determination).
- `NotSupportedException` / `MissingMethodException` at runtime → failure.

---

## 7. Size and startup budget

No absolute size target is set (it depends on the runtime version); instead, an **incremental budget** is set:

| Item | Budget | Measurement method |
| --- | --- | --- |
| Runesvaner core incremental size on the AOT binary | ≤ 500 KB | Compare enabled vs disabled |
| CLI binary | Baseline + 2 MB | JSONL default, excluding SQLite native assets |
| Startup time increment | ≤ 50 ms | Median of 10 cold starts |
| Runtime memory increment | ≤ 8 MB | Idle + after 100 requests |

> The values are initial budgets, to be calibrated with real measurements in M1. If exceeded, prioritize trimming dependencies and strings rather than sacrificing functional correctness.

---

## 8. Relationship with EventPipe / diagnostic ports

| Mechanism | Used by Runesvaner | Notes |
| --- | --- | --- |
| EventPipe | No | In-process collection, does not depend on the runtime diagnostic pipeline |
| DiagnosticServer / diagnostic port | No | No attach |
| EventSource | No (core) | Optional export is decided by OTel/the user |
| ClrMD / DAC | No | No heap/thread analysis |
| ETW / LTTng | No | Not in v1 |
| `System.Diagnostics.Metrics` | Yes | Standard metrics, available under AOT |
| `ActivitySource` | Optional | Interoperates with OTel |

This means:

- Runesvaner can work in AOT applications where EventPipe is disabled.
- Runesvaner does not compete with `dotnet-trace`, nor does it replace it.
- If users need CPU/GC-level diagnostics, they should still use `dotnet-trace` / PerfView / platform tools.

---

## 9. Runtime capability detection

Runesvaner detects at startup:

| Detection item | Behavior |
| --- | --- |
| Whether AOT | Records `runtime.aot=true`, used in reports |
| Whether `ActivitySource` is supported | Yes (BCL) |
| Whether SQLite native is supported | If SQLite is enabled, attempt to open; on failure fall back to JSONL and warn |
| Whether the OTLP exporter is supported | If enabled, disable with a warning when initialization fails |
| File system permissions | Check that the storage directory is writable; on failure use memory mode |

Detection results are written to `session.start` and `runesvaner doctor`.

---

## 10. Known limitations under AOT

1. **No heap analysis**: no object allocation, generation, or leak information.
2. **No dynamic providers**: custom price-table plugin DLLs cannot be loaded at runtime; JSON configuration only.
3. **No dynamic serialization**: custom event types require a source-generated context; third-party sinks must handle it themselves.
4. **Library defaults to SQLite, CLI defaults to JSONL**: affected by `dotnet/efcore#36068` under AOT; on failure, fall back to JSONL; see 4.1.
5. **OTel optional**: see 4.2.
6. **Reflection-based configuration**: not provided; must be code or a Source Generator.
7. **Symbols and stack traces**: AOT stack traces may lack method names; `StackTraceSupport=true` mitigates this but does not guarantee completeness.

---

## 11. Future: AOT-specific diagnostics (v2 research)

If the main product gains a foothold, we can explore:

| Direction | Value | Risk | Decision |
| --- | --- | --- | --- |
| Startup time breakdown | Medium-high | Requires deep ILC/ETW/perf integration | D-18 |
| Binary size attribution | Medium | Overlaps with Sizoscope/ILC logs | D-18 |
| AOT compatibility Roslyn analyzer | Medium-high | Overlaps with the official analyzer | D-19 |
| Managed heap analysis | High | Requires runtime-level engineering | Keep a research branch (D-18), no delivery commitment |

Principle: these directions must be **independent experimental branches** and must not slow down the main product; D-18/D-19 have decided to retain research/v2 investment, and community maintainers are welcome.

---

## 12. Risk register

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| MEAI dependencies introduce AOT warnings | Medium | High | Contract tests; isolate if necessary |
| SQLite AOT compatibility (#36068) | High | High | CLI defaults to JSONL; library falls back on failure; M1 verification workaround |
| OTel exporter AOT failure | Medium | Medium | Optional package; alternative path |
| Size exceeds budget | Medium | Medium | Incremental budget; trim strings |
| Warning promotion blocks the build | High | Medium | Enable in stages; allowlist + ADR |
| `UnsafeAccessor` abuse | Low | High | Prohibited by default; review |

---

## 13. Decision status

D-01–D-25 are all resolved; see `15-decision-log.md` and `19-open-implementation-decisions.md`. Directly related to this strategy:

- D-05: `net10.0`, M3 evaluates `net11.0`.
- D-07: SQLite default in libraries/apps; JSONL default for CLI AOT; fall back on failure.
- D-13: core has zero dependencies, OTLP optional.
- D-15: Windows/Linux; macOS non-target.
- D-24: `win-arm64` non-target; `linux-arm64` optional.
- D-18: AOT heap analysis kept as a research branch, not on the mainline.
- D-19: Roslyn AOT analyzer investment in v2.