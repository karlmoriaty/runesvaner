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

- The recorder-only path is well below the M1 P95 target of 1 ms (M2 target 0.5 ms).
- The 1.27 KB allocation per request comes mostly from request IDs, record objects, segment arrays, and the usage dictionary/tags path.
- This is a baseline, not a regression gate yet. CI will add a benchmark gate after the benchmark suite covers storage and the MEAI decorator.

## Next benchmarks

- B02: recorder + MEAI decorator, non-streaming.
- B03: recorder + JSONL store.
- B04: recorder + SQLite store.
- B05: streaming chunk recording.
- B06: cost engine calculation.