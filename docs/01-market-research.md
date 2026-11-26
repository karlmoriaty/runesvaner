# Market and Competitive Research: .NET AI Diagnostics vs Native AOT Diagnostics

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Research date | 2026-11-25 |
| Data snapshot | GitHub API, NuGet API, Microsoft Learn (all captured on the same day) |
| Related documents | `00-project-charter.md`, `03-prd.md`, `15-decision-log.md` |

---

## 1. Method and Limitations

### 1.1 Method

1. Verify official documentation: Native AOT diagnostics capabilities, `dotnet-counters`, `Microsoft.Extensions.AI`, and OpenTelemetry GenAI semantic conventions.
2. GitHub repository data: Stars, Forks, License, time of last commit (API snapshot).
3. NuGet search: download counts and maintenance status of packages related to `.NET LLM observability`.
4. Competitor license verification: read the repository `LICENSE` text.
5. Technical feasibility reasoning: based on official capability boundaries, with no code prototype (documentation only at this stage).

### 1.2 Limitations

- GitHub Stars are influenced by propagation, language ecosystem, and release timing, and do not equal product quality.
- NuGet download counts include CI and mirror traffic and do not equal real user counts.
- No user interviews were conducted; user needs are judged from public Issues, competitor features, and engineering experience, and must be validated with interviews in M1.
- Prices, licenses, and APIs may change; this document notes the date and must be rechecked periodically.

---

## 2. Wedge A: Native AOT Compatible Diagnostics

### 2.1 Official Capability Boundaries (Key Facts)

According to Microsoft Learn's [Native AOT diagnostics](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics) (page updated 2025-04):

| Capability | Support Level | Notes |
| --- | --- | --- |
| Observability and telemetry | Partial support | EventPipe supported since .NET 8; requires setting `<EventSourceSupport>true</EventSourceSupport>` |
| `dotnet-trace` / `dotnet-counters` / `dotnet-monitor` | Available | Works via EventPipe and the diagnostics port, but not all runtime events are supported |
| Development-time diagnostics | Fully supported | `build` / `run` / `test` still use the full runtime; only `publish` produces an AOT binary |
| Native debugging | Partial support | WinDbg / gdb / lldb available; managed debuggers unavailable; symbol files required |
| CPU Profiling | Partial support | Platform tools such as PerfView and `perf` available |
| Managed heap analysis | **Not supported** | `dotnet-gcdump`, PerfView heap analysis, and Visual Studio heap analysis are all unavailable |

In addition, the [Native AOT overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) explicitly states limitations including: no dynamic loading, no runtime code generation, mandatory trimming, single file, some runtime libraries not fully annotated as AOT-compatible, and limited diagnostics support.

### 2.2 Real Gaps Derived from This

| Gap | Value | Difficulty | Fit for an Individual Project |
| --- | --- | --- | --- |
| Managed heap analysis (object allocation, generations, leaks) | High | Extremely high: no DAC, requires building GC heap traversal from scratch | Low |
| Startup time decomposition (runtime init, module loading, static construction, ILC code) | Medium-high | Medium-high: requires deep ILC / ETW / perf integration | Medium |
| Binary size attribution (generic instantiations, trimming residue, dependencies) | Medium | Medium: ILC logs exist, Sizoscope is a reference | Medium-high |
| "Production unobservability" caused by EventPipe being off by default | Medium | Medium: requires packaging policy and size trade-offs | Medium |
| AOT compatibility static diagnostics (Roslyn analyzer) | Medium-high | Medium: Roslyn analyzer + ILLink warning integration | High (matches the sponsor's technical preference) |
| Low-overhead always-on metrics under AOT | Medium | Low: based on `Meter` + optional EventPipe | High |

**Key judgment**: Native AOT diagnostics is not "a complete blank". The existence of EventPipe makes `dotnet-trace` / `dotnet-counters` usable under AOT, which weakens the selling point of "another general AOT Profiler". The real gaps are concentrated in **managed heap** and **startup/size attribution**; the former is extremely high technical risk, and the latter is more like a toolchain than a runtime diagnostics tool.

### 2.3 Competitive Landscape

| Tool/Direction | Relationship to AOT | Notes |
| --- | --- | --- |
| `dotnet-trace` / `dotnet-counters` | Available (EventPipe) | Officially maintained, strong general capability |
| PerfView / `perf` | CPU available | Unmanaged perspective, steep learning curve |
| `dotnet-gcdump` | Unavailable | Managed heap gap |
| Sizoscope | Binary size analysis | Community tool, offline-oriented |
| ILC logs | Startup/size clues | Requires specialized knowledge to interpret |
| OpenTelemetry .NET | Usable but with size/Trim costs | The community already has AOT size optimization PRs (e.g., removing Regex, saving about 734 KB) |

### 2.4 Conclusion

- **Not the primary launch track.**
- Can serve as the v2 "Native AOT experimental branch": startup/size attribution, AOT compatibility Roslyn analyzer.
- Managed heap analysis is not the main line; keep a research branch and issues (D-18).

---

## 3. Wedge B: Diagnostics Tools for AI Workloads

### 3.1 Market Background

| Fact | Implication |
| --- | --- |
| `Microsoft.Extensions.AI` latest version reaches 11.0.1 (NuGet snapshot) | The .NET AI abstraction has matured, with a unified integration point |
| MEAI has built-in `UseOpenTelemetry` middleware | Basic tracing is already covered officially; "just emitting Spans" has no differentiation |
| `Microsoft.Extensions.VectorData.Abstractions` reaches 11.0.1 | The RAG pipeline can be uniformly observed |
| Semantic Kernel 28.8k Stars, continuously active | A large base of enterprise .NET AI users |
| OpenTelemetry GenAI semantic conventions split into a separate repository, metrics still Development | The standard is evolving rapidly, with adaptation and interpretation costs |

### 3.2 Competitive Landscape (GitHub Snapshot, 2026-11-25)

| Project | Stars | Primary Language | License | Positioning | Coverage of .NET |
| --- | ---: | --- | --- | --- | --- |
| [Langfuse](https://github.com/langfuse/langfuse) | 37,839 | TypeScript | MIT Expat + EE directory | AI engineering platform: gateway, observability, evaluation, Prompt management | Mainly OTLP / SDK, not .NET native |
| [Arize Phoenix](https://github.com/Arize-ai/phoenix) | 12,423 | Python | Elastic License 2.0 | AI observability and evaluation | Not .NET native; ELv2 restricts hosted services |
| [OpenLLMetry](https://github.com/traceloop/openllmetry) | 7,554 | Python | Apache-2.0 | GenAI observability based on OTel | Mainly Python / TS, .NET not a focus |
| [Semantic Kernel](https://github.com/microsoft/semantic-kernel) | 28,807 | C# | MIT | .NET AI orchestration framework | Not itself a diagnostics tool |
| [dotnet/extensions](https://github.com/dotnet/extensions) | 3,300 | C# | MIT | MEAI and diagnostics/telemetry infrastructure | Provides basic middleware, does not do cost/waterfall |

### 3.3 Direct/Adjacent Competitors on the .NET Side (NuGet Search Snapshot)

Searching for `llm observability` returns 70 results, among which those directly related to .NET AI diagnostics include:

| Package | Downloads (Total) | Notes | Judgment |
| --- | ---: | --- | --- |
| `qyl` | 13,588 | Local observability stack, including collector and diagnostics collector | Tool form, not a library; can complement Runesvaner |
| `NovaCore.Agents.Observability.OpenTelemetry` | 12,671 | OTel export for Agent runtime | Bound to its own Agent framework |
| `MemoryIndexer.Sdk` | 16,822 | Memory library SDK, includes OTel | Not diagnostic-specific |
| `Progress.Observability.Instrumentation` | 3,103 | Telerik AgentClarity, MEAI client observability | Commercial vendor, validates the MEAI integration point |
| `RagObservability` | 314 | Blazor local RAG dashboard, includes token/cost | Features close but narrow in form, early |
| `SmartLLM.Telemetry` | 1 star | AI-native observability for .NET | Very early, no impact yet |

**Key judgments**:

1. The .NET side is not without attempts, but there is **no dominant player**, and most packages have low download counts and single functionality.
2. MEAI's built-in OTel middleware solves "is there a Span", but not "latency decomposition, cost attribution, streaming profiling, low AOT overhead".
3. A commercial vendor (Progress) has entered MEAI observability, which validates the track but also means clear differentiation is needed to avoid head-on competition.

### 3.4 Competitor Feature Gap Matrix

| Capability | MEAI `UseOpenTelemetry` | Langfuse | Phoenix | OpenLLMetry | Runesvaner Target |
| --- | --- | --- | --- | --- | --- |
| GenAI Span | Yes | Yes | Yes | Yes | Yes (compatible) |
| Token counting | Partial | Yes | Yes | Yes | Yes |
| Latency waterfall decomposition | No | Partial (Span tree) | Partial (Span tree) | Partial | **Yes (explicit segmentation)** |
| Cost ledger | No | Yes (platform side) | Partial | Partial | **Yes (local + versioned)** |
| Streaming TTFT / ITL | No | Partial | Partial | Partial | **Yes** |
| Retry duplicate billing detection | No | No | No | No | **Yes** |
| Cache discount attribution | No | Partial | No | Partial | **Yes** |
| AOT-safe core | Unknown | Not applicable | Not applicable | Not applicable | **Yes** |
| Local-first / zero SaaS | Yes (in-process) | Self-hosted | Self-hosted | Yes | **Yes** |
| OTLP interoperability | Yes | Yes | Yes | Yes | **Yes (optional)** |

> "Partial" means the platform has a Span tree or cost fields, but no explicit segmentation and attribution model for .NET AI requests.

---

## 4. Scoring of the Two Wedges

Scores 1–5, with 5 being best. Weights reflect the constraints of an individual open-source project.

| Dimension | Weight | A: Native AOT Diagnostics | B: AI Workload Diagnostics |
| --- | ---: | ---: | ---: |
| Market demand clarity | 20% | 3 | 5 |
| Competition intensity (lower is better) | 15% | 4 | 3 |
| Technical feasibility (individual project) | 20% | 2 | 5 |
| Differentiation sustainability | 15% | 4 | 3 |
| 10k-star potential | 10% | 2 | 4 |
| Match with the sponsor's technical preference | 10% | 5 | 4 |
| Ecosystem synergy (MEAI/SK/OTel) | 10% | 2 | 5 |
| **Weighted total** | 100% | **3.1** | **4.2** |

Calculation (A): 0.2×3 + 0.15×4 + 0.2×2 + 0.15×4 + 0.1×2 + 0.1×5 + 0.1×2 = 0.6 + 0.6 + 0.4 + 0.6 + 0.2 + 0.5 + 0.2 = 3.10 (rounded to 3.1)
Calculation (B): 0.2×5 + 0.15×3 + 0.2×5 + 0.15×3 + 0.1×4 + 0.1×4 + 0.1×5 = 1.0 + 0.45 + 1.0 + 0.45 + 0.4 + 0.4 + 0.5 = 4.20

> The difference mainly comes from technical feasibility and ecosystem synergy, not from "AI being hotter".

---

## 5. Conclusion and Recommendation

### 5.1 Decided

**The sponsor has decided: take wedge B as the primary track, with wedge A as a first-class constraint and second-phase differentiator (D-01, 2026-11-25).**

Specific statement:

> Runesvaner is a .NET-native AI request profiler. It first solves "decomposing the latency and cost of a single AI request", while treating Native AOT / Trimming as a first-class citizen constraint; once the main product is established, it will explore AOT startup/size attribution as an independent experimental branch.

### 5.2 Not Recommended Routes

| Route | Reason Not Recommended |
| --- | --- |
| Directly build "another general LLM observability platform" | Langfuse/Phoenix are already strong; head-on competition has no chance of winning |
| Directly build a "Native AOT Profiler" | Heap analysis requires runtime-level engineering; EventPipe already covers some scenarios |
| Build "AI gateway + observability" | Requires network proxy, authentication, quotas, and operations; beyond an individual project's bandwidth |
| Build "Prompt management + Eval" | Deviates from the diagnostics main line, and platform competition is fierce |

### 5.3 Differentiation Moat

1. **Explicit latency segmentation**: not "the Span tree looks deep", but a readable conclusion such as "network 1.2s, TTFT 2.1s, generation 4.0s, serialization 0.2s".
2. **Traceable cost**: price table version, source, and effective time; no guessing for unknown models.
3. **AOT safe**: zero reflection in the core, AOT CLI; this is a .NET characteristic that Python/TS platforms cannot replicate.
4. **Local-first**: does not leave the machine by default, suitable for data-sensitive enterprises and individuals.
5. **Streaming and retry semantics**: TTFT/ITL, partial responses, and duplicate billing — details that "platform-side Span trees" easily overlook.

### 5.4 Go-to-Market Strategy

| Stage | Action |
| --- | --- |
| M1 | Build a real RAG example with MEAI, record a 30-second demo; show the waterfall on the README first screen |
| M2 | Publish a NuGet preview; publish technical articles on r/dotnet, Awesome .NET AI, and Hacker News |
| M3 | Collaborate with the Semantic Kernel community; submit .NET implementation feedback to the OTel GenAI semantic conventions |
| M4 | Seek reviews from .NET community KOLs; release 1.0 and a migration guide |

---

## 6. Risks and Counterarguments

| Counterargument | Response |
| --- | --- |
| "MEAI already has built-in OTel, why do I need you?" | Built-in only emits Spans; Runesvaner does segmentation, cost, streaming, retries, and local reports, and can export to OTel |
| "Self-hosted Langfuse is enough" | Langfuse is a platform with high integration and operations costs; Runesvaner is a developer's local tool, showing results in 5 minutes |
| "Cost calculation is inaccurate; better not to calculate" | Price table versioning + unknown model marking + source field; no guessing, and users can override |
| "AOT users are few" | AOT is a constraint, not a selling point; it ensures the product does not fail in AOT services, rather than serving only AOT users |
| "An individual project can't grow big" | Introduce maintainers via the CLA; keep scope restrained; first achieve single-point excellence, then platform interoperability |

---

## 7. Sources

- [Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Diagnostics and instrumentation for Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics)
- [.NET diagnostics tools overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)
- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [Microsoft.Extensions.AI libraries](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [OpenTelemetry GenAI semantic conventions](https://github.com/open-telemetry/semantic-conventions-genai)
- [GenAI metrics](https://github.com/open-telemetry/semantic-conventions-genai/blob/main/docs/gen-ai/gen-ai-metrics.md)
- [Langfuse repository](https://github.com/langfuse/langfuse)
- [Arize Phoenix repository](https://github.com/Arize-ai/phoenix)
- [OpenLLMetry repository](https://github.com/traceloop/openllmetry)
- [Semantic Kernel repository](https://github.com/microsoft/semantic-kernel)
- [OpenTelemetry .NET Native AOT size PR #6849](https://github.com/open-telemetry/opentelemetry-dotnet/pull/6849)
- [NuGet search: llm observability](https://azuresearch-usnc.nuget.org/query?q=llm+observability)
- [NuGet: Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI)
- [NuGet: Microsoft.Extensions.VectorData.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions)

---

## 8. Decision Status

D-01–D-25 are all resolved; see `15-decision-log.md`. Directly related to this document:

- D-01: AI workload diagnostics launched first, AOT as a constraint and v2 branch.
- D-02: Project name Runesvaner.
- D-13: Zero-dependency core, OTLP optional.
- D-18: AOT heap analysis keeps a research branch, not the main line.
- D-19: Roslyn AOT analyzer invested in v2.