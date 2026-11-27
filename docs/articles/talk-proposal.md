# Talk proposal: From TTFT to Dollars — Profiling .NET AI Workloads

## Abstract (150 words)

Your RAG endpoint takes eight seconds and the LLM span is "slow". Which part? DNS, TLS, upload,
provider queueing, the first token, or streaming? And why did the bill triple while latency stayed
flat?

This talk shows how to measure AI workloads in .NET as a sequence of explainable segments, not a
single opaque span. We will break a streaming call into queue, connect, upload, provider wait,
TTFT, and generation; separate token usage from price tables; and show why unknown must never be
rendered as zero. We will also cover the constraints of Native AOT, where reflection-based
instrumentation stops working, and what a source-generated, optional-package design looks like.
Demos use a local mock, so no API key is required. You will leave with a measurement model you can
apply to any provider, and a checklist for profiling AI services in production.

## Format

- 30 minutes (25 minutes talk + 5 minutes Q&A)
- Intermediate .NET developers, platform engineers, SRE/observability engineers
- No AI/ML math required

## Outline

| Time | Section | Key point |
| --- | --- | --- |
| 0–3 min | The 8-second call | General APM says "slow span"; that is not actionable |
| 3–10 min | Six segments | Queue, connect, upload, provider wait, TTFT, generation; what is measured vs estimated |
| 10–15 min | Streaming metrics | TTFT vs ITL vs tokens/s; why ITL is estimated; cancellation/interruption |
| 15–21 min | Cost ledger | Usage, price table versions, cached/reasoning tokens, unknown ≠ zero |
| 21–26 min | Native AOT | What breaks, what survives, SQLite exception, CI proof |
| 26–30 min | Demo + checklist | RAG sample → CLI report → Studio waterfall → compare |

## Demo plan

1. Run the RAG sample (no network): embedding, vector search, generation.
2. Show `runesvaner report` and `runesvaner costs`.
3. Open Studio: waterfall, session browser, compare view.
4. Show an unknown-price model staying `Unknown`.
5. Optionally show the AOT sample binary running the same recording path.

## Target conferences

Submit through each conference's official CFP page; do not guess a CFP URL.

- .NET Conf — https://dotnetconf.net/
- NDC Conferences — https://ndcconferences.com/
- dotnetos — https://dotnetos.org/
- DevConf.cz — https://devconf.cz/
- Techorama Belgium — https://techorama.be/
- Techorama Netherlands — https://techorama.nl/
- For Chinese events, search for the current .NET Conf China / local .NET user group CFP.

## Submission checklist

- Title and abstract (above).
- 30-minute outline with timings.
- Demo requirements: .NET 10 SDK; no network or API key; laptop with a browser.
- Speaker bio: independent .NET developer; maintainer of Runesvaner; focus on AI workload
  diagnostics, Native AOT, and local-first tooling.
- Links: repository, docs site, compatibility matrix.
- Accessibility: all demos are terminal + browser; no reliance on color alone.

## Short version (for a 10-minute lightning talk)

Title: **Unknown is not zero: measuring AI cost without guessing**

Abstract: A tour of token accounting, price-table versions, and the failure modes that make LLM
bills unexplainable — plus the three-line integration that records usage without capturing prompt
content.