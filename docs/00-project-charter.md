# Project Charter: Runesvaner

| Field | Value |
| --- | --- |
| Document status | Accepted (D-01–D-25 resolved) |
| Version | 1.0 |
| Date | 2026-11-25 |
| Author | Project sponsor + CodeArts design agent |
| Audience | Project sponsor, prospective maintainers, early contributors |
| Related documents | `01-market-research.md`, `03-prd.md`, `15-decision-log.md` |

---

## 1. One-Line Definition

Runesvaner is an **in-process performance and cost profiler** for .NET AI applications: it decomposes a single LLM / Embedding / retrieval request into an explainable latency waterfall and cost ledger, and remains usable under Native AOT, Trimming, and local-first constraints.

## 2. Vision

When .NET developers face AI workloads, they should no longer see only "a very slow Span" or "a month-end bill"; instead, they should be able to answer three questions at the granularity of a single request:

1. **Which segment is slow?**
2. **Which item is expensive?**
3. **What should change next time?**

## 3. Problem Statement

### 3.1 Capability Boundaries of Existing Tools

The general .NET diagnostics toolchain addresses process-level and runtime-level problems:

- `dotnet-trace` / `dotnet-counters` / `dotnet-dump` target CPU, GC, thread pool, locks, exceptions, and memory dumps.
- OpenTelemetry targets distributed tracing and metrics, and can provide Span start/end times and standard GenAI attributes.
- Langfuse, Arize Phoenix, and OpenLLMetry target LLM observability platforms, and are strong in trace aggregation, evaluation, and Prompt management.

Together they leave three gaps:

| Gap | Manifestation |
| --- | --- |
| Latency decomposition | Only one total-duration Span; unable to distinguish the shares of network, queueing, first token, generation, tools, vector retrieval, and serialization |
| Cost attribution | Only token counts; lacking price table versions, cache discounts, duplicate billing from retries, and attribution by feature/tenant/Prompt version |
| Runtime form | .NET AI services increasingly use Native AOT / Trimming, where general profilers and some OTel components are limited or costly in size |

### 3.2 Why Now

- `Microsoft.Extensions.AI` has established a unified abstraction for .NET AI and provides `UseOpenTelemetry` middleware on the 10.x / 11.x lines; this offers a stable foothold for "deep profiling at the middleware layer".
- `Microsoft.Extensions.VectorData` has entered 10.x / 11.x, so the RAG pipeline can be uniformly observed.
- Native AOT has supported EventPipe since .NET 8, but **managed heap analysis is still not supported**, and EventPipe is an optional component; AOT diagnostics remains a clear engineering gap.
- Mainstream LLM observability platforms are mainly Python / TypeScript; the .NET side has only scattered, low-download attempts, with no dominant player.

## 4. Goals and Non-Goals

### 4.1 Goals (v1)

1. Provide low-overhead, composable profiling middleware for `IChatClient` and `IEmbeddingGenerator`.
2. Output a request-level latency waterfall, explicitly marking the measurement method and estimation error of each segment.
3. Output a traceable cost ledger, with the price table carrying version, source, and effective time.
4. Support streaming response diagnostics: TTFT, inter-token latency, cancellation, and interruption.
5. Do not collect content by default; store locally; make OTLP export optional.
6. Keep the core package AOT / Trim safe; the CLI can be published with Native AOT.
7. Make "install → run → see the waterfall for one request" complete within 5 minutes.

### 4.2 Non-Goals (v1)

- General CPU / memory profiler.
- LLM evaluation (Eval), dataset management, and Prompt experimentation platform.
- Proxy gateway or request forwarding.
- Hosted SaaS.
- Automatically modifying business code or automatic tuning.
- Capturing and uploading Prompt / Completion bodies by default.
- Replacing OpenTelemetry, Langfuse, Phoenix, or Grafana.

### 4.3 Explicitly Deferred

| Direction | Reason | Disposition |
| --- | --- | --- |
| Native AOT managed heap analysis | Requires deep runtime / GC work; high risk for an individual project | As a v2 research item, not a launch selling point |
| General AOT startup/size attribution | Deeply coupled with the ILC toolchain | As an independent experimental branch |
| Distributed cross-service trace merging | Already covered by OTel | Via OTLP interoperability |
| AI-assisted root cause analysis | Privacy and credibility risks | Optional in v2, local-first |

## 5. Target Users

| Priority | User | Core Need |
| --- | --- | --- |
| P0 | .NET backend engineers | Locate where a RAG/Agent request is slow and where it is expensive |
| P0 | AI platform/infrastructure teams | Unified integration, low overhead, exportable, AOT-usable |
| P1 | Independent developers / startup teams | Zero barrier, local, no SaaS bill |
| P1 | SRE / FinOps | Cost attribution, budget alerts, duplicate billing from retries |
| P2 | Tech leads | Comparison and decision basis by model/version/feature |

## 6. Success Metrics

### 6.1 North Star Metric

**WADP (Weekly Active Diagnosed Projects)**: the number of independent projects that produce at least one valid diagnostic session per week.

GitHub Stars is not used as the sole success criterion, but the following reference targets are set:

| Stage | Reference Target |
| --- | --- |
| M1 | 10 external trial projects |
| M2 | 100 external trial projects; 1,000 NuGet monthly downloads |
| M3 | 1,000 active projects; external contributors ≥ 5 |
| 12 months after 1.0 | GitHub Stars ≥ 5,000; WADP ≥ 500 |

> Stars are heavily influenced by propagation and serve only as a lagging indicator; WADP and the proportion of "action taken after diagnosis" are the leading indicators.

### 6.2 Guardrail Metrics

| Metric | Threshold |
| --- | --- |
| Collection overhead (no I/O, sampling enabled) | Single-request P95 increment ≤ 1 ms, CPU ≤ 1% |
| Business impact of collection failure | 0 (silent degradation after circuit breaking) |
| Cost calculation traceability rate | 100% of bills carry a price table version |
| AOT Smoke | All green on both Windows/Linux |
| Content leakage incidents | 0 |
| Average fix time for P0 defects | Within one release cycle |

## 7. Scope and Versions

### 7.1 v1 Scope

- Microsoft.Extensions.AI: Chat + Embedding.
- Semantic Kernel: as Should.
- Microsoft.Extensions.VectorData: as Should.
- Library/in-app SQLite by default; CLI AOT defaults to JSONL; failure fallback and swap.
- CLI: `run`, `report`, `top`, `export`, `open`.
- Local Studio.
- OTLP export (optional package).
- Tail sampling, budget alerts.

### 7.2 v2 Candidates

- Agent / MCP tool call tree.
- Multimodal token estimation.
- Distributed diagnostic session merging.
- Native AOT startup/size attribution research branch (community-maintained, no delivery commitment).
- Roslyn AOT analyzer (`Runesvaner.Analyzers`).
- Integration with process-level metrics from `dotnet-counters`.

## 8. Key Assumptions and Validation

| Assumption | Validation Method | Failure Signal |
| --- | --- | --- |
| .NET teams need request-level latency decomposition, not just OTel Spans | 10 in-depth interviews + M1 trial feedback | Users believe OTel + Grafana is sufficient |
| Cost attribution is a must-have, and users are willing to maintain price tables | Observe the number of price table update Issues / PRs | No one submits price corrections |
| AOT compatibility is a differentiator, not a pseudo-need | AOT user interviews + TFM distribution of downloaded packages | AOT users account for < 5% |
| Local-first can win over users | Trial conversion rate | Users insist that cloud collaboration is mandatory |
| MEAI middleware is the correct integration point | Integration LOC and retention | Users require direct integration with each vendor SDK |

## 9. Constraints

1. **Individual project**: not affiliated with an organization; limited maintenance bandwidth, so scope must be controlled.
2. **CLA**: allows more maintainers, but contributors must sign; the CLA text requires legal review.
3. **AOT**: core paths must not introduce reflection or dynamic code.
4. **Zero-dependency core**: avoid making the OTel SDK a mandatory dependency.
5. **Privacy by default**: do not collect content by default, and do not upload implicitly.
6. **Cost trustworthiness**: rather show nothing than guess the price of an unknown model.
7. **Documentation first**: all design is fixed in Markdown before development starts; key decisions require the sponsor's approval.
8. **Platform scope**: Windows 10/11 and Linux; macOS is a non-goal (D-15).

## 10. Governance Principles

- **BDFL + maintainers**: the sponsor holds final decision authority; day-to-day merges are handled by maintainers under lazy consensus.
- **RFC process**: changes affecting the public API, data model, license, or governance must go through an RFC.
- **Public roadmap**: milestones and exit criteria are public to avoid "black-box prioritization".
- **Contributor experience**: one-command build, reproducible benchmarks, and clear good first issues.
- **Security first**: vulnerabilities are disclosed privately; details are made public after a fix.

## 11. Relationship to the Two Initial Wedges

The sponsor proposed two candidate wedges:

1. Native AOT compatible diagnostics.
2. A diagnostics tool for AI workloads.

The sponsor decided on 2026-11-25 (D-01): **take AI workload diagnostics as the primary track, and treat Native AOT as a first-class citizen constraint and second-phase differentiator**.

Reason summary (full argument in `01-market-research.md`):

- Native AOT EventPipe has been available since .NET 8, so traditional tools are not entirely ineffective; the real gap is managed heap analysis, whose technical risk does not match an individual project.
- AI workload diagnostics has clear, verifiable, and monetizable (or sponsor-able) user value, and .NET-side competition is weak.
- AOT compatibility is the "moat" of an AI diagnostics product rather than "the main battlefield": it keeps Runesvaner usable in AI services deployed with AOT, where most OTel solutions struggle with size and compatibility.

## 12. Kill Criteria

If any of the following occurs, pause or pivot rather than continuing to pile on features:

1. After M2 ends, external trial projects < 10, and the "willing to recommend" proportion in interviews < 30%.
2. Collection overhead fails to meet targets for two consecutive milestones (P95 > 2 ms or CPU > 2%).
3. Cost calculation is proven untrustworthy (difference in repeated calculation of the same request > 5% and unexplainable).
4. Microsoft officially provides equivalent latency decomposition and cost attribution directly in MEAI, open source and free.
5. The number of maintainers remains 0 for a long time, and the sponsor cannot contribute for two consecutive quarters.

Pivot options:

- Shrink to a single-function library for the "cost ledger".
- Shrink to a "Native AOT startup/size attribution" tool.
- Become a pure OTLP Processor, parasitizing the existing OTel ecosystem.

## 13. Decision Status

D-01–D-25 are all resolved; see `15-decision-log.md`. Directly related to this charter:

- D-01 Primary track: AI workload diagnostics launched first, AOT as a constraint and v2 branch.
- D-02 Project name: Runesvaner.
- D-03 License: Apache-2.0.
- D-04 CLA: ICLA + CCLA.
- D-05 Target framework: `net10.0`, evaluate `net11.0` at M3.
- D-07 Storage: library SQLite by default; CLI AOT defaults to JSONL; fallback on failure.
- D-10 Commercialization: no commercialization for now, accepting non-bundled sponsorship.

## 14. References

- [.NET Native AOT diagnostics](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics)
- [Microsoft.Extensions.AI libraries](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [OpenTelemetry GenAI semantic conventions](https://github.com/open-telemetry/semantic-conventions-genai)
- [.NET diagnostics overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)