# Why your .NET AI request is slow: a segment-by-segment teardown

*A practical guide to measuring what general APM cannot see in an LLM call — and the numbers from a
small open-source profiler built for it.*

## The 8-second RAG call

A support assistant answers in eight seconds. The HTTP span says `200 OK`. The database span is
fast. The LLM span is "slow". None of that tells you whether the time went into DNS, TLS, uploading
the prompt, provider queueing, the first token, or streaming the answer.

General APM was designed for request/response services. LLM calls have a different shape:

- A long-lived streaming response, where time-to-first-token matters more than total duration.
- A cost dimension that general APM does not model at all.
- Provider-side queueing that looks like "network" unless you separate it.
- Retries that can silently double the bill.

Runesvaner is an in-process profiler for .NET AI workloads. This post walks through the measurement
model and the numbers behind it. It is intentionally honest about what cannot be measured.

## Six segments, measured where possible

The waterfall records these segment kinds:

| Segment | What it means | Source |
| --- | --- | --- |
| Queue | Middleware and serialization before the HTTP call | Stopwatch |
| Connect | DNS + TCP connection | `SocketsHttpHandler.ConnectCallback` |
| Upload | Request body serialization | `HttpContent` wrapper |
| ProviderQueue | Wait for response headers | Stopwatch |
| TTFT | First non-empty text delta | MEAI decorator |
| Generation | Response stream until EOF | Stream wrapper |

Two details matter:

1. **Connect is not TLS.** `ConnectCallback` returns a plain stream; `SocketsHttpHandler` performs
   TLS internally. There is no public TLS-complete hook. Runesvaner offers an opt-in TLS probe that
   derives an estimate from the certificate-validation callback and marks it
   `Estimated=true`/`Derived`. It is off by default because enabling it installs a validation
   callback.
2. **ProviderQueue is not TTFT.** Waiting for response headers is an HTTP-level signal. TTFT is the
   first token surfaced by the AI client. They answer different questions.

The tests use a loopback TCP server and a real `HttpClient`, so the segments are produced by real
sockets, not by mocks of the profiler.

## Streaming changes the question

For a streaming response, total duration is misleading. The profiler records:

- TTFT, from the first non-empty text delta.
- Inter-token latency P50/P95, derived from text-chunk timestamps and marked
  `ItlEstimated=true`, because chunks are not tokens.
- Tokens/s, only when provider usage is present; `null` otherwise.
- Cancellation, interruption, and finish reason.

A local SSE test compares the recorded TTFT with a client-side observer and keeps the error within
10 ms. The exact timings vary by machine; the point is that the measurement is validated, not just
printed.

## Cost is a first-class dimension — and unknown is not zero

Cost needs usage and a price table. Both are uncertain:

- Providers report input/output/cached/reasoning tokens differently.
- Prices change and are not always retrievable.
- A model without a price entry must not silently cost zero.

Runesvaner uses a versioned price table. The built-in snapshot is verified, checksummed, and
limited to entries whose source could be retrieved. At snapshot time, first-party OpenAI pricing
pages returned HTTP 403, so those models resolve to `CostStatus.Unknown` rather than a guessed
number. The sample price table is explicitly illustrative.

The recorder computes input, cached, output, reasoning, batch discount, and tiered pricing, and
rounds to six decimals with banker's rounding.

## What the numbers look like

On the development machine used for the repository:

| Measurement | Result |
| --- | --- |
| Recorder-only P95 | ~3 µs |
| Recorder allocation | ~1.5 KB per request |
| CI performance gate | P95 ≤ 500 µs, allocation ≤ 2 KB |
| Studio first screen, 10k JSONL records | ~0.84 s serial cold sum |
| TTFT accuracy vs client observer | within 10 ms on a local mock |

The recorder gate runs on every push. It measures the no-storage path; JSONL and SQLite writes are
measured separately, which is why storage is isolated behind a bounded background writer.

## Three-line integration

With dependency injection:

    builder.Services.AddRunesvaner(options =>
    {
        options.Provider = "anthropic";
        options.StorageMode = StorageMode.Sqlite;
        options.StoragePath = ".runesvaner";
    });

    var client = new ChatClientBuilder(innerChatClient)
        .UseRunesvaner()
        .Build(serviceProvider);

The default is no content capture. `ContentCapture.Metadata` records lengths and hashes;
`ContentCapture.Full` stores text and is opt-in.

## Native AOT is a constraint, not a slogan

The core packages avoid reflection and dynamic code, use JSON source generation, and are marked
`IsAotCompatible`. The sample publishes and runs under Native AOT on Windows and Linux in CI.

SQLite is the exception: `Microsoft.Data.Sqlite` AOT support is still an open issue, so the AOT
path uses JSONL. TLS is estimated; SK has no AOT promise; the OTLP exporter is optional.

## Try it

- Repository: https://github.com/karlmoriaty/runesvaner
- Documentation: https://karlmoriaty.github.io/runesvaner/
- Examples: five runnable samples, including a loopback OpenAI protocol test that needs no API key.
- Compatibility matrix: `docs/25-compatibility-matrix.md`.

If you are debugging an AI request that "feels slow", start by separating the segments. The answer
is usually not "the model"; it is one specific interval.