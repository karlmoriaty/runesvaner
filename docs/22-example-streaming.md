# Example 3: Streaming TTFT and ITL

**Goal:** measure time-to-first-token, inter-token latency, chunks, tokens/s, and cost for a
streaming response.

## Code

`samples/Runesvaner.Sample.Streaming/Program.cs`

A fake streaming client emits three text chunks with 25 ms spacing followed by a usage-only
chunk. The MEAI decorator records TTFT, ITL P50/P95, chunk count, TPS, and cost.

## Run

    dotnet run --project samples/Runesvaner.Sample.Streaming -c Release

## Expected output

    Streaming sample: three chunks with 25 ms spacing.
    Time to first token.
    chunks: 4
    ttft: ~30000 us
    itl p95: ~30000 us (estimated: True)
    tps: ~2000
    cost: 0.0032 USD (Known)

Exact timings vary per machine. `chunks` includes the usage-only chunk, which carries no text and
therefore does not affect TTFT.

## What to look for

- `TtftUs` comes from the first non-empty text delta.
- ITL is marked `ItlEstimated=true`: stream chunks are the observable unit, not provider tokens.
- TPS uses provider output tokens when usage is present; it is `null` without usage.
- Cost uses the built-in verified Anthropic snapshot for `claude-sonnet-5.5`.