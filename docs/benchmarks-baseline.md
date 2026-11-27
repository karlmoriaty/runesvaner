# Benchmark Baseline: Recorder

| Field | Value |
| --- | --- |
| Date | 2026-11-26 |
| Project | `test/Runesvaner.Benchmarks` |
| Benchmark | `RecorderBenchmarks.RecordRequest` |
| Tool | BenchmarkDotNet 0.15.8 |
| Runtime | .NET 10.0.15 (SDK 10.0.404), X64 RyuJIT x86-64-v3 |
| OS | Windows 11 25H2 (10.0.26200.9974) |
| CPU | 13th Gen Intel Core i7-13650HX, 20 logical / 14 physical cores |

## Command

    dotnet run --project test/Runesvaner.Benchmarks -c Release -- --filter '*RecorderBenchmarks*'

## Result

| Method | Mean | Error | StdDev | Median | Gen0 | Gen1 | Allocated |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| RecordRequest | 324.9 ns | 8.18 ns | 24.11 ns | 315.0 ns | 0.1032 / 1000 ops | 0.0002 / 1000 ops | 1.27 KB |

## P95 harness

    dotnet run --project test/Runesvaner.Benchmarks -c Release -- --p95

Latest run (same machine, 10,000 iterations):

| Metric | Result |
| --- | ---: |
| P50 | 1.00 us |
| P95 | 3.70 us |
| P99 | 6.10 us |

The M1 gate is P95 ≤ 2 ms; the recorder-only P95 is 3.70 us, roughly three orders of magnitude below the gate. This does not include network, provider, JSONL, or SQLite I/O.

## What is measured

The benchmark measures one recorder lifecycle with an in-memory no-op store:

1. Start a chat request.
2. Record one generation segment.
3. Record provider usage.
4. Complete the request.

It does not measure provider network time, MEAI middleware overhead, JSONL serialization, or SQLite I/O.

## Interpretation

- The recorder-only path is four to five orders of magnitude below the M1 P95 target.
- The 1.27-1.52 KB allocation per request comes mostly from request IDs, record objects, segment arrays, and the usage dictionary/tags path.
- This page records the baseline; CI enforces the regression gate described below.

## CI gate

    dotnet test/Runesvaner.Benchmarks/bin/Release/net10.0/Runesvaner.Benchmarks.dll --gate

The `perf-gate` job runs on every push and pull request (ubuntu-latest, Release). It uses 2,000 warmup iterations and 20,000 measured iterations, then compares:

| Metric | Budget |
| --- | ---: |
| P95 | 500 us (M1 target for B02) |
| Allocated | 2 KB per request |

Latest local gate run: P50 1.00 us, P95 2.80-2.90 us, P99 4.10-5.20 us, 1.52 KB/op — PASS. A run that exceeds either budget prints `Result: FAIL` and exits with code 1, failing the job.

Budgets can be overridden per environment for calibration (`RUNESVANER_PERF_P95_US`, `RUNESVANER_PERF_ALLOC_BYTES`); CI uses the defaults, so changing a threshold requires a reviewed code change.

## Studio query baseline (10k JSONL)

| Metric | Result |
| --- | ---: |
| Dataset | 10,000 JSONL records (10 MB) |
| `/api/costs` | 0.44 s cold, 0.19 s / 0.10 s warm |
| `/api/sessions` | 0.22 s cold, 0.21 s / 0.12 s warm |
| `/api/requests?limit=20` | 0.18 s cold, 0.19 s / 0.13 s warm |
| First-screen sum (serial) | ~0.84 s cold, within the 1 s M2 target |

Measured 2026-11-27 against the JSONL query store. SQLite now pushes session filtering and limits into SQL rather than reading every payload.

## Next benchmarks

- B02: recorder + MEAI decorator, non-streaming.
- B03: recorder + JSONL store.
- B04: recorder + SQLite store.
- B05: streaming chunk recording.
- B06: cost engine calculation.