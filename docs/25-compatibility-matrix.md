# Compatibility Matrix

**Verified on 2026-11-27.** "Verified" means the path is exercised by CI or by a local test that
runs in the repository; it does not mean every combination below has been tested.

## Runtime and packages

| Item | Version |
| --- | --- |
| .NET SDK | 10.0.404 |
| Target framework | `net10.0` |
| Microsoft.Extensions.AI | 10.11.1 |
| Microsoft.Extensions.AI.OpenAI | 10.11.1 |
| Microsoft.SemanticKernel.Abstractions | 1.82.0 |
| Microsoft.Extensions.VectorData.Abstractions | 10.11.1 |
| OpenTelemetry / OTLP exporter | 1.21.0 |
| Microsoft.Data.Sqlite | 10.0.15 |
| NuGet package version | `0.3.0-alpha.1` |

## Platform matrix

| Area | Windows 11 x64 | Linux x64 | macOS | Windows arm64 | Notes |
| --- | --- | --- | --- | --- | --- |
| Core / Abstractions / JSONL | Verified (CI) | Verified (CI) | Non-goal | Not supported | D-24 |
| Microsoft.Extensions.AI decorator | Verified (CI) | Verified (CI) | Non-goal | Not supported | OpenAi protocol test uses loopback mock |
| HTTP segments | Verified (tests) | Verified (tests) | Non-goal | Not supported | TLS segment is opt-in and estimated |
| SQLite store | Verified (tests) | Verified (tests) | Non-goal | Not supported | AOT not verified, see below |
| OpenTelemetry exporter | Verified (tests) | Verified (tests) | Non-goal | Not supported | OTLP/HTTP collector test |
| DI integration | Verified (tests) | Verified (tests) | Non-goal | Not supported | |
| Semantic Kernel adapter | Verified (tests) | Verified (tests) | Non-goal | Not supported | SK 1.82 |
| VectorData adapter | Verified (tests) | Verified (tests) | Non-goal | Not supported | `IVectorSearchable<T>` only |
| Native AOT sample | Verified (CI) | Verified (CI) | Non-goal | Not supported | `samples/Runesvaner.Sample.Aot` |

## Native AOT

| Component | AOT status | Evidence / limitation |
| --- | --- | --- |
| `Runesvaner.Abstractions` | Designed for AOT | `IsAotCompatible=true`, no reflection |
| `Runesvaner` (Core) | Verified in sample | Windows/Linux `aot-smoke` jobs |
| `Runesvaner.Storage.Jsonl` | Verified in sample | JSON source generation |
| `Runesvaner.MicrosoftExtensionsAI` | Verified in sample | No dynamic code |
| `Runesvaner.Http` | Not separately verified | `SocketsHttpHandler` and callbacks are AOT-safe by API design |
| `Runesvaner.Storage.Sqlite` | **Not verified; known risk** | `Microsoft.Data.Sqlite` AOT remains open (`dotnet/efcore#36068`); use JSONL for AOT |
| `Runesvaner.Exporters.OpenTelemetry` | Not verified | Optional package; validate before AOT deployment |
| `Runesvaner.SemanticKernel` | **No AOT promise** | SK Abstractions may use reflection; keep SK outside AOT paths |
| `Runesvaner.VectorData` | Not verified | Thin adapter; provider implementations may not support AOT |
| `Runesvaner.Extensions.DependencyInjection` | Not verified | Factory registrations avoid reflection, but references SQLite |

## CI evidence

| Job | What it proves |
| --- | --- |
| `build-test (ubuntu-latest)` / `build-test (windows-latest)` | Restore, build, all tests, RAG sample, price validation, CLI cost ledger |
| `aot-smoke (windows)` / `aot-smoke (linux)` | Native AOT publish and run of the AOT sample |
| `perf-gate (ubuntu)` | Recorder P95 and allocation budgets |
| `pack (ubuntu)` | Ten packages produced, no sample packages, README and XML in every package |
| `studio-ui` | React/RTK Query production build |
| `docs-site` | Documentation site generation |

## Known limitations

1. **SQLite + Native AOT** is not verified; select `StorageMode.Jsonl` for AOT deployments.
2. **TLS segment** is opt-in, is derived from the certificate-validation callback, and is marked
   `Estimated=true`. There is no public TLS-complete hook in .NET.
3. **macOS** is a non-goal; **win-arm64** is not supported.
4. **SK token usage** is not recorded by the SK adapter; use the MEAI layer for usage.
5. **VectorData** compatibility is limited to `IVectorSearchable<TRecord>` in 10.11.1, not the
   store/collection interfaces from other versions.
6. Compatibility is a tested subset, not a certification of every provider or vector database.