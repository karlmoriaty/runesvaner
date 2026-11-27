# Observability for .NET AI workloads under Native AOT

*What survives trimming and AOT, what does not, and how to design an AI profiler that does not lie
about the difference.*

## The appeal and the trap

Native AOT gives .NET services small, fast-starting binaries with no JIT. It also removes the
assumptions many diagnostics libraries rely on: reflection, dynamic proxies, runtime code
generation, and some event sources. An AI workload adds long-lived streams, provider SDKs, and
token accounting on top.

The trap is claiming "AOT compatible" because the app builds. Compatibility must be proven by
publishing and running the AOT binary, then exercising the real code paths.

## Design rules that survived

The Runesvaner core follows four rules:

1. **No reflection in the hot path.** Request records, segments, and usage are plain records.
2. **JSON source generation.** Price tables and request records serialize through
   `JsonSerializerContext`; no runtime serializer discovery.
3. **No dynamic proxies.** Decorators implement the public interfaces directly:
   `IChatClient`, `IEmbeddingGenerator`, Semantic Kernel's `IChatCompletionService`, and
   VectorData's `IVectorSearchable<T>`.
4. **Optional dependencies stay optional.** OpenTelemetry, SQLite, SK, and VectorData are separate
   packages; the core does not drag them into an AOT binary.

## The known exception: SQLite

`Microsoft.Data.Sqlite` still has an open AOT issue (`dotnet/efcore#36068`). The supported AOT path
is JSONL storage. The library default remains SQLite for app servers; the AOT CLI and sample use
JSONL.

This is documented in the decision log and the compatibility matrix instead of being hidden behind
a generic "AOT ready" badge.

## Measurement boundaries

AOT does not remove the need for honesty about measurement:

- **TLS** has no public completion hook. An opt-in probe estimates it from certificate validation
  and marks it `Estimated=true`/`Derived`.
- **Inter-token latency** is derived from text-chunk timestamps and marked `ItlEstimated=true`.
- **Tokens/s** is `null` without provider usage.
- **Unknown prices** stay `Unknown`; they are never zero.

Estimated values are still useful, but they must be labeled. A profiler that presents estimates as
exact numbers is worse than no profiler.

## CI proof, not a checkbox

The repository verifies AOT on every push:

- `aot-smoke (windows)` publishes and runs the AOT sample on `win-x64`.
- `aot-smoke (linux)` publishes and runs it on `linux-x64`.
- `build-test` runs on Ubuntu and Windows.
- `pack` verifies ten packages, XML docs, and READMEs.
- `perf-gate` enforces recorder P95 and allocation budgets.

The compatibility matrix records the tested subset and the untested paths. "Verified" means a CI
job or a repository test exercises it, not that every provider or OS combination is certified.

## A checklist for AOT AI services

1. Publish the actual AOT binary in CI; do not rely on analyzers alone.
2. Prefer source-generated JSON for every persisted record.
3. Keep reflection-based profilers out of the request path.
4. Separate optional integrations into packages.
5. Label estimates and document missing hooks.
6. Test streaming, cancellation, and error paths, not only success.
7. Record the toolchain versions; an updated ILCompiler can change the result.

## Closing

AOT-friendly observability is mostly a discipline: small interfaces, explicit serialization,
optional dependencies, and verified claims. The AI workload does not change that; it only makes the
failure modes more expensive.

- Repository: https://github.com/karlmoriaty/runesvaner
- Documentation: https://karlmoriaty.github.io/runesvaner/
- Compatibility matrix: `docs/25-compatibility-matrix.md`