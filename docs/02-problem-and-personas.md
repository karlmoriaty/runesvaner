# Problem and Personas

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Product, design, engineering |
| Related documents | `00-project-charter.md`, `01-market-research.md`, `03-prd.md` |

---

## 1. Problem Definition

Performance and cost problems in AI applications differ fundamentally from those in traditional web applications:

- Latency is dominated by **external model services**; the application side can influence only part of it.
- Cost is determined by **token metering**, not by CPU / memory / request count.
- A single request contains **multiple heterogeneous phases**: Embedding, vector retrieval, tool calls, LLM inference, streaming transfer, and serialization.
- Failures and retries may cause **duplicate billing** and may also cause **incomplete billing**.
- Prompt and Completion may contain **sensitive data** and must not be uploaded by default.

Therefore, neither general process-level profilers nor general APM can directly answer the questions developers care about most. Two specific scenarios illustrate this below.

---

## 2. Scenario 1: A Single 8.4-Second RAG Request

A customer service knowledge base application's P95 latency rose from 3.2 seconds to 8.4 seconds. The existing monitoring has only one Span for `POST /chat`, showing 8.4 seconds; `dotnet-counters` shows CPU normal and GC normal. The team cannot determine whether the model got slower, the network jittered, or a serial retrieval was newly added in the code.

The waterfall Runesvaner is expected to output (sample data, not real measurements):

| Segment | Duration | Share | Measurement Method |
| --- | ---: | ---: | --- |
| In-app queueing | 0.12 s | 1.4% | Middleware entry → start of execution |
| Authentication / token acquisition | 0.03 s | 0.4% | Local timing |
| DNS / TCP connection | 0.08 s | 1.0% | `HttpClient` diagnostic events |
| TLS handshake | 0.15 s | 1.8% | `HttpClient` diagnostic events |
| Request upload | 0.22 s | 2.6% | Request body write complete |
| Provider queueing | 1.90 s | 22.6% | Wait before first byte (estimated) |
| First token (TTFT) | 2.40 s | 28.6% | First non-empty delta |
| Generation | 3.10 s | 36.9% | First token → last token |
| Tool call | 0.40 s | 4.8% | Tool Span |
| Serialization / application processing | 0.20 s | 2.4% | Local timing |
| **Unaccounted** | **-0.20 s** | — | Parallel segment overlap, must be marked |

> The sum of segments need not equal the total duration: parallel tool calls, streaming overlap, and clock granularity all cause differences. Runesvaner must explicitly show `unaccounted` and `overlap`, rather than forcing normalization.

### 2.1 Key Insights

- The largest contributors are **generation** (3.1 s) and **first token** (2.4 s), not the network.
- The 1.9 s of provider queueing is "invisible time"; it can be found only by separating the completion of request upload from the first byte.
- Without segmentation, the team would likely misjudge it as "the network is slow" and optimize in the wrong direction.

---

## 3. Scenario 2: The Bill Tripled

A SaaS's monthly LLM cost rose from $1,200 to $3,600. The bill is aggregated only by model and cannot answer:

- Which feature? Is it the newly added "document summary" or "customer service Q&A"?
- Which tenant? Is it abuse by free users or growth from large customers?
- Which Prompt version? Did the new version make the output longer?
- Is it duplicate billing caused by retries, or cache misses?
- Is it a unit price change caused by a model upgrade, or usage growth?

The cost ledger Runesvaner is expected to output (sample prices, for illustration only):

| Item | Token | Unit Price (USD / 1M) | Cost |
| --- | ---: | ---: | ---: |
| Non-cached input | 4,400 | 1.25 | $0.00550 |
| Cached input | 8,000 | 0.125 | $0.00100 |
| Output | 900 | 10.00 | $0.00900 |
| Reasoning tokens | 600 | 10.00 | $0.00600 |
| **Single-request total** | — | — | **$0.02150** |

Attribution dimensions:

| Dimension | Value |
| --- | --- |
| Feature | `support.answer` |
| Tenant | `tenant_7f3a` (hash) |
| Prompt version | `support-v12` |
| Model | `gpt-5` (request) / `gpt-5-2026-08` (response) |
| Attempt | 2 (first attempt 429, not billed) |
| Price table | `2026-11-01` |
| Cache | Hit 8,000 / cacheable 12,400 (64.5%) |

### 3.1 Key Insights

- The cost problem is not "how much in total", but "which item is growing and why".
- Retries and caching are two "hidden gold mines": one may cause duplicate billing, and the other may be an overlooked saving.
- The price table must be versioned; otherwise historical bills cannot be explained.

---

## 4. Personas

### 4.1 Li Wei: .NET Backend Engineer (P0)

| Dimension | Description |
| --- | --- |
| Background | 30 years old, 5 years of .NET, responsible for a customer service RAG service; familiar with ASP.NET Core, EF Core, and Redis, unfamiliar with Profiler internals |
| Goal | Reduce P95 from 8 seconds to 4 seconds; avoid being woken up by on-call |
| Current state | Uses Application Insights + self-built Stopwatch logs; can only see total duration |
| Pain points | Doesn't know where it is slow; changing a parameter requires a round of load testing; log fields are inconsistent |
| Success criteria | Locate the specific phase within 5 minutes; before/after comparison after changes |
| Key quote | "I don't need another dashboard; I need to know exactly where this request is stuck." |
| Usage scenarios | Local development, staging environment, occasional production issues |
| Resistance points | Complex integration, many dependencies, performance impact, many code changes |

### 4.2 Chen Ke: AI Platform Lead (P0)

| Dimension | Description |
| --- | --- |
| Background | 35 years old, platform team lead serving 20 business teams; focused on standardization, cost, and compliance |
| Goal | Unified integration method; delegate latency and cost to business teams; ensure AOT services can also use it |
| Current state | Teams integrate OTel/Langfuse themselves with inconsistent fields; the platform side can only do coarse-grained aggregation |
| Pain points | Fragmented integration; compliance resistance to data leaving the machine; AOT services grow in size when integrating OTel |
| Success criteria | One NuGet package + one set of configuration; OTLP optional; no AOT build regression |
| Key quote | "I can accept fewer features, but not uncontrollable integration or uncontrollable data." |
| Usage scenarios | Platform SDK, build pipelines, compliance review |
| Resistance points | Vendor lock-in, implicit upload, unstable public API |

### 4.3 Zhou Yuan: Independent Developer (P1)

| Dimension | Description |
| --- | --- |
| Background | 27 years old, independently developing an AI SaaS, one person for frontend + backend; extremely cost-sensitive |
| Goal | Control monthly API cost; quickly locate slow requests; no added operational burden |
| Current state | Uses the free tier of a hosted APM; discovers overspending only after the bill arrives |
| Pain points | SaaS observability tools charge by usage; local debugging cannot show the real call chain |
| Success criteria | Zero-configuration local view of waterfall and cost; no service deployment |
| Key quote | "I don't want to deploy a whole platform just to look at one request." |
| Usage scenarios | Local development, single-machine deployment, VPS |
| Resistance points | Complex configuration, Docker, cloud accounts, usage-based billing |

### 4.4 Wang Lan: SRE / FinOps (P1)

| Dimension | Description |
| --- | --- |
| Background | 38 years old, SRE turned FinOps; focused on budgets, anomalies, and attribution |
| Goal | Detect cost anomalies early; allocate by team/feature; identify retry waste |
| Current state | Uses cloud vendor bills + self-built Excel; coarse granularity, lagging |
| Pain points | Bills are only by model dimension; anomalies cannot be explained; retry costs are invisible |
| Success criteria | Attribution by tenant/feature/model; budget alerts; drill-down into anomalous requests |
| Key quote | "I don't want to learn about overspending at month-end; I want to know when the request happens." |
| Usage scenarios | Weekly reports, budget reviews, anomaly investigation |
| Resistance points | Inaccurate data, inconsistent definitions, manual price table maintenance |

---

## 5. Jobs-to-be-Done

### 5.1 Functional Jobs

| JTBD | Trigger Scenario | Expected Outcome |
| --- | --- | --- |
| When a RAG request becomes slow, I want to quickly locate the specific phase | P95 alerts, user complaints | An actionable conclusion within 5 minutes |
| When the bill is anomalous, I want to attribute it by dimension | Monthly bills, budget alerts | Find the growth source and anomalous requests |
| When I move to AOT, I want to confirm diagnostics won't break the build | Release pipeline | No new AOT/Trim warnings, controlled size |
| When I change models, I want to compare latency and cost | Model selection | Before/after comparison on the same batch of requests |
| When I develop locally, I want to see the real call chain | Debugging | No dependency on a remote platform |

### 5.2 Emotional Jobs

| JTBD | Desired Feeling |
| --- | --- |
| When facing an unfamiliar AI call stack, I want to "understand it" | Sense of control, explainability |
| When facing the bill, I want to "explain it clearly" | Trustworthy, defensible |
| When facing compliance review, I want to "prove data didn't leave" | Sense of security |

### 5.3 Social Jobs

| JTBD | Desired Image |
| --- | --- |
| Driving observability standards within the team | Professional, methodical |
| Explaining growth in cost reviews | Data-driven, reliable |

---

## 6. User Journey

| Stage | User Action | Key Touchpoint | Success Criterion | Failure Mode |
| --- | --- | --- | --- | --- |
| Discovery | Search "dotnet llm latency", browse GitHub, colleague recommendation | README, demo images, blog | Understand the value within 30 seconds | Looks like "yet another platform" |
| Integration | Install NuGet, add 3 lines of code | Quick start, examples | Working within 10 minutes | Complex configuration, dependency conflicts |
| First run | Run sample, open Studio | CLI output, UI first screen | See one real request | Blank page, no data |
| Diagnosis | View waterfall, filter slow requests, drill down | Waterfall, detail panel | Find the root cause | Inaccurate segmentation, missing tags |
| Optimization | Change code/switch model/add cache | Suggestions, comparison | Metrics improve | Don't know the next step |
| Validation | Before/after comparison, export report | Compare, export | Reproducible, shareable | Inconsistent data definitions |

### 6.1 Moments of Truth

1. **First time opening Studio**: must show "total duration + segments + cost" within one screen, otherwise it will be judged as "yet another Span tree".
2. **First time finding an anomaly**: must distinguish `estimated` from exact values, otherwise trust collapses.
3. **First cost reconciliation**: must reconcile with the provider bill, otherwise it won't be used again.
4. **First AOT publish**: must pass without warnings, otherwise it will be removed.

---

## 7. Existing Alternatives and Dissatisfaction

| Alternative | Advantages | Dissatisfaction | Runesvaner Difference |
| --- | --- | --- | --- |
| OpenTelemetry + Jaeger/Grafana | Standard, flexible, large ecosystem | Heavy configuration; no cost; Span tree not intuitive | Zero-configuration local waterfall + cost; OTLP export |
| Self-hosted Langfuse | Full-featured, large community | Requires deployment; data leaves the machine; .NET integration not native | In-process, local, .NET native |
| Application Insights | Good Azure integration | High cost; weak AI semantics; limited AOT support | Lightweight, AOT, AI-specific |
| Self-built logs + Stopwatch | Flexible, controllable | Inconsistent fields; no standard; high maintenance cost | Standardized model + automatic segmentation |
| Commercial APM (Datadog, etc.) | Full-stack, mature | Expensive; weak AI Token/cost model | Focus on AI request-level profiling |
| Manual review of provider bills | Authoritative | Lagging, coarse granularity | Request-level attribution and real-time budget |

---

## 8. Kano Requirement Classification

| Type | Requirement | Notes |
| --- | --- | --- |
| Must-be | Latency waterfall, cost visibility, privacy by default, low overhead | Absence leads to immediate abandonment |
| Performance | Streaming metrics, retry detection, cache attribution, multi-dimensional filtering | The better, the more satisfied |
| Delighter | Local Studio, AOT safety, automatic price table updates, before/after comparison | Builds word of mouth |
| Indifferent | Custom themes, complex permissions | Not done |
| Reverse | Mandatory cloud, mandatory content capture, complex deployment | Explicitly avoided |

---

## 9. Objection Handling

### 9.1 "OTel Is Already Enough"

- OTel is a **transport standard**, not a **diagnostic conclusion**. It tells you when a Span starts and ends; it does not tell you that 1.9 of the 8.4 seconds was spent in provider queueing.
- Runesvaner does not replace OTel; it generates finer semantics and exports them to existing backends via OTLP.

### 9.2 "Self-Hosted Langfuse Is Fine"

- Feasible for platform teams, but too costly for an individual developer.
- Runesvaner's local first experience is 5 minutes; Langfuse requires a database, object storage, and deployment.
- The two can coexist: Runesvaner collects and profiles, Langfuse aggregates and evaluates.

### 9.3 "Cost Calculation Is Inaccurate"

- Do not guess unknown models; mark them as `unknown`.
- The price table carries version, source, and effective time.
- Support user overrides; replay historical sessions with the price table in effect at the time.
- Show both "estimated" and "verified" states in the UI.

### 9.4 "Will It Slow Down My Service"

- Target P95 increment ≤ 1 ms, CPU ≤ 1%.
- Sampling and ring buffers by default; asynchronous persistence.
- The collection pipeline has circuit breaking; failures do not affect business.
- Benchmarks are public, with CI regression thresholds.

### 9.5 "AOT Users Are Few, Why Support It"

- AOT is a **constraint**, not the only selling point. Supporting AOT means the core library has no reflection black magic, which for all users means smaller, faster, and more predictable.
- For AI services using AOT, Runesvaner is one of the few diagnostics solutions still available.

---

## 10. Assumptions and Validation Plan

| Assumption | Validation Method | Pass Criterion |
| --- | --- | --- |
| Developers prefer "conclusions" over "raw Spans" | Usability testing with 5 users | ≥ 4 people can state where it is slow within one minute |
| Cost attribution is P0 | Interviews + trial | ≥ 60% of users proactively configure a price table |
| Local-first is a differentiator | Trial conversion | ≥ 30% of users choose pure local mode |
| AOT compatibility affects selection | Questionnaire | ≥ 20% of users say "I wouldn't use it without AOT" |
| A 5-minute first experience is feasible | Timed test | ≥ 80% of users see the waterfall within 5 minutes |

---

## 11. Hidden Requirements (Summary)

See `14-hidden-requirements.md` for the full list. Directly related to users:

- Cost credibility and traceability.
- Monotonic clocks for time measurement and estimation markers.
- Streaming semantics (TTFT definition, cancellation, partial responses).
- Multi-tenancy and PII.
- Statistical bias caused by sampling.
- Metric explosion caused by high-cardinality labels.
- Fault isolation of the collection tool itself.
- JSON and configuration binding under AOT.
- Security boundaries of the local UI (DNS rebinding, tokens).
- Contributor experience (one-command build, test data).

---

## 12. Decision Status

D-01–D-25 are all resolved; see `15-decision-log.md`. Directly related to this document:

- D-01: AI workload diagnostics launched first.
- D-02: Project name Runesvaner.
- D-09: Default content capture `None`.
- D-10: No commercialization for now, accepting non-bundled sponsorship.
- D-14: Local sampling 100%, recommended 10% in production.
- D-16: UI supports English, Simplified Chinese, and Standard German.