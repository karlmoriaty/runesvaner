# Roadmap and Milestones

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Founder, maintainers, contributors |
| Related documents | `03-prd.md`, `12-testing-and-benchmarks.md`, `15-decision-log.md` |

> This document does not give calendar durations; it defines only **order, scope, and exit criteria**. Each milestone must satisfy its exit criteria before moving to the next, to avoid "all features accumulated but unusable".

---

## 1. Milestone Overview

| Milestone | Theme | Core deliverables | Exit criteria |
| --- | --- | --- | --- |
| M0 | Design freeze | Documents, decisions, governance, repository skeleton | D-01–D-25 decided; empty solution buildable; CI passing |
| M1 | Walking Skeleton | MEAI Chat + SQLite (library) / JSONL (CLI) + CLI + AOT smoke | One real session readable end to end; AOT 0 warnings |
| M2 | MVP | Cost, streaming, Studio, OTLP, privacy | 10 projects in internal test; overhead ≤ 1 ms |
| M3 | Beta | SK, VectorData, tail sampling, budget, documentation site | NuGet preview; external contributors ≥ 3 |
| M4 | 1.0 | API freeze, security audit, Windows/Linux AOT | 1.0 released; P0 at zero |
| v2 | Expansion | Agent/MCP, multimodal, AOT experiments | Separate RFC |

---

## 2. M0: Design Freeze

### 2.1 Goal

Before writing the first line of business code, solidify the key product, technical, and governance decisions to avoid rework.

### 2.2 Scope

| Work item | Deliverable |
| --- | --- |
| Requirements and competitors | Documents `00`–`03` |
| Technical design | Documents `05`–`08` |
| Security and governance | Documents `09`–`10` |
| Decisions | `15-decision-log.md`, D-01–D-25 decided |
| Naming | `16-brand-naming.md`, initial NuGet/GitHub screening completed; trademark deferred |
| Repository skeleton | `Runesvaner.sln`, `Directory.Build.props`, `global.json`, `.editorconfig` |
| Governance files | `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `CLA.md` |
| CI draft | build + test + markdown lint |
| Environment survey | .NET SDK, Windows/Linux runners, AOT toolchain availability |

### 2.3 Exit criteria

1. D-01–D-25 are all decided (see `15-decision-log.md`).
2. The repository can `dotnet build` the empty solution.
3. CI passes hello world on Windows/Linux.
4. The LICENSE, CLA, and SECURITY files are in place (the CLA is legally reviewed or explicitly marked "pending review").
5. Naming screening is complete: the NuGet root ID is available; the GitHub repository name is available; the trademark search is deferred (user decision, risk recorded).
6. At least 5 potential user interviews are completed, validating the core hypotheses (see `02`).

### 2.4 Out of scope

- Do not write business code.
- Do not do high-fidelity UI.
- Do not publish NuGet packages.
- Do not promise dates.

---

## 3. M1: Walking Skeleton

### 3.1 Goal

Prove that the architecture is feasible with the minimal end-to-end path: one MEAI Chat request → collection → SQLite (library default) / JSONL (CLI AOT default) → CLI report → AOT runtime.

### 3.2 Scope

| Module | Content |
| --- | --- |
| `Runesvaner.Abstractions` | Core models, enums, interfaces |
| `Runesvaner` | Recorder, Clock, Segmenter, RingBuffer, SelfMonitor |
| `Runesvaner.MicrosoftExtensionsAI` | Chat non-streaming + streaming decorator (minimal) |
| `Runesvaner.Storage.Sqlite` | Library/in-app default storage, WAL, retention policy |
| `Runesvaner.Storage.Jsonl` | CLI AOT default, fallback and exchange |
| `Runesvaner.Cli` | `run`, `report`, `doctor` |
| Tests | Unit + integration + AOT smoke |
| Examples | Console + RAG (mock provider) |
| Benchmark | Baseline collection overhead |

### 3.3 Exit criteria

1. Run the sample and generate one complete `request.end` event.
2. `runesvaner report` can output total duration, segments, and tokens (mock).
3. The chunk order, content, and cancellation semantics of streaming requests match those before integration.
4. `dotnet publish -r linux-x64 /p:PublishAot=true` produces 0 AOT/Trim warnings.
5. The AOT binary smoke passes (non-streaming + streaming + error paths).
6. Collection failure injection test: the business request still succeeds.
7. Overhead baseline: P95 ≤ 2 ms (M1 interim threshold, tightened to 1 ms in M2).
8. The SQLite (library) and JSONL (CLI) schemas pass golden tests; record the measured SQLite AOT results.
9. The README "30-second experience" can actually be executed (except for cost/Studio).

### 3.4 Risks

| Risk | Mitigation |
| --- | --- |
| MEAI streaming wrapper breaks cancellation | Dedicated tests; minimal wrapper |
| Activity/DI reflection warnings | Explicit registration; AOT analysis |
| SQLite AOT (#36068) | CLI defaults to JSONL; library falls back on failure; validate the workaround in M1 |
| Inaccurate segment estimation | Only measured segments first; estimated segments are marked estimated |

### 3.5 Out of scope

- Cost engine.
- Studio.
- OTLP.
- SK / VectorData.

---

## 4. M2: MVP

### 4.1 Goal

Enable external users to complete the loop of "see the waterfall + verify cost + local UI" in real projects.

### 4.2 Scope

| Module | Content |
| --- | --- |
| Cost engine | Price table, cache/reasoning/batch, unknown models, version replay |
| Streaming | TTFT, ITL, TPS, cancellation, interruption |
| Retry | attempt, backoff, duplicate billing marking |
| Storage | SQLite query optimization + framework-dependent CLI evaluation |
| CLI | `top`, `export`, `open` |
| Studio | Session list, request waterfall, cost ledger, Health |
| Export | OTLP (optional package) |
| Privacy | Content capture level, hashing, redaction, local token |
| Sampling | Head sampling + self-monitoring |
| Tests | Benchmark thresholds, AOT matrix, Studio security tests |

### 4.3 Exit criteria

1. Cost reconciled against official billing samples from at least 2 providers, with error ≤ 5%.
2. Unknown models are not shown as 0; show `—` and the reason.
3. Streaming TTFT measurement error vs. manual measurement ≤ 10 ms (local mock).
4. Studio first screen ≤ 1 s (10,000-request dataset).
5. Studio binds only to `127.0.0.1`; the API is inaccessible without a token.
6. Overhead P95 ≤ 1 ms, CPU ≤ 1% (benchmark scenario).
7. AOT Windows/Linux smoke all green.
8. 10 projects in internal test, at least 6 completing a real diagnostic session.
9. Privacy tests: default storage contains no Prompt/Completion original text.
10. OTLP export to a local collector succeeds; field mapping passes golden tests.

### 4.4 Risks

| Risk | Mitigation |
| --- | --- |
| Cost price table errors | Versioning, sources, reconciliation views, user overrides |
| Studio security vulnerabilities | Local token, Host validation, CSP, penetration testing |
| SQLite AOT failure | Automatic fallback to JSONL + ADR |
| Performance regression | CI benchmark thresholds; sampling; circuit breaker |
| Scope creep | Strictly follow the Must list; defer Should items |

### 4.5 Out of scope

- SK / VectorData.
- Tail sampling.
- Budget alerts.
- Distributed merging.
- Agent tool tree.
- Commercial support.

---

## 5. M3: Beta

### 5.1 Goal

Expand ecosystem coverage, build the community and documentation, and prepare for 1.0.

### 5.2 Scope

| Module | Content |
| --- | --- |
| Semantic Kernel | Filter integration, tool calls |
| VectorData | Retrieval Span, collection dimension |
| Tail sampling | Keep errors/slow/over-budget + weighting |
| Budget | Request/tenant/feature thresholds + alerts |
| Documentation site | Quick start, concepts, API, troubleshooting, migration |
| Community | Issue/PR templates, good first issue, Discussions |
| Compatibility | MEAI 10.x/11.x contract tests |
| Export | OTLP validation; third parties on demand (D-20) |

### 5.3 Exit criteria

1. Enabling SK and MEAI simultaneously does not produce duplicate request records.
2. VectorData retrieval Span's collection, top_k, and duration are correct.
3. Tail sampling retains 100% of error/slow requests; aggregates use weighting.
4. Budget overruns produce events and UI prompts without blocking requests.
5. The documentation site is searchable, navigable, and contains 5 end-to-end examples.
6. NuGet preview packages published with an installation success rate ≥ 95% (internal test).
7. External contributors ≥ 3, merged PRs ≥ 10.
8. The compatibility matrix is all green (TFM × OS × AOT).
9. At least 1 technical article/talk published.

### 5.4 Risks

| Risk | Mitigation |
| --- | --- |
| SK API changes | Adapter isolation; contract tests |
| Tail sampling memory growth | Buffer only lightweight records; cap |
| Documentation lag | Documentation in the same PR as code |
| Community cold start | Proactive outreach; good first issue |
| Compatibility matrix explosion | Support only LTS + latest STS |

---

## 6. M4: 1.0

### 6.1 Goal

Stabilize the public API, complete security and performance audits, and establish sustainable governance.

### 6.2 Scope

| Work | Content |
| --- | --- |
| API freeze | Public API Review, compatibility baseline |
| Security | Dependency audit, Studio penetration testing, threat model review |
| Performance | Windows/Linux benchmarks, overhead report |
| Quality | P0/P1 at zero, complete documentation |
| Governance | CLA process, maintainers ≥ 2, RFC template |
| Release | 1.0 NuGet, migration guide, release notes |
| Operations | Website/documentation site, support channels |

### 6.3 Exit criteria

1. Public API frozen, `PublicApiAnalyzers` baseline passing.
2. The breaking-change list is empty (or documented).
3. The security audit has no unfixed high-severity items.
4. Windows/Linux AOT smoke + benchmarks all green.
5. P0 = 0, P1 ≤ 3 with a plan.
6. Documentation covers all Must features and troubleshooting.
7. CLA automated checks in effect; at least 2 maintainers can release.
8. 1.0 release announcement and migration guide.
9. At least 1,000 active projects or 5,000 stars (either one, as a reference).

### 6.4 Risks

| Risk | Mitigation |
| --- | --- |
| Premature 1.0 | Be driven by exit criteria, not dates |
| API design flaws | Collect feedback during the M2/M3 preview period |
| Security audit budget | Reserve in advance; community audits |
| Insufficient maintainers | Contributor development; automation |

---

## 7. v2 Candidates

| Direction | Value | Dependency | Decision |
| --- | --- | --- | --- |
| Agent / MCP tool tree | High | SK/MCP stable | D-20 |
| Multimodal tokens | Medium | provider support | D-08 |
| Distributed session merging | Medium | OTel context | D-13 |
| Native AOT startup/size | Medium-high | Independent research branch | D-18: community-maintained, no delivery promise |
| Roslyn AOT analyzer | Medium-high | Roslyn | D-19: v2 investment, community maintenance welcome |
| Evaluation interoperability | Medium | On demand | D-20: not required |
| Hosted service | Low (conflicts with positioning) | — | Not recommended |

---

## 8. Milestone Governance

- Before each milestone starts, create a task list in GitHub Milestones.
- Update progress and risks every two weeks.
- Do not proceed to the next milestone if exit criteria are unmet; scope may be reduced, but the decision must be recorded.
- Any newly added Must feature must replace an existing Must, or the milestone must be postponed.

---

## 9. Decision Status

D-01–D-25 have all been decided; see `15-decision-log.md` and `19-open-implementation-decisions.md`. Directly related to this roadmap:

- D-07: Library/in-app SQLite by default; CLI AOT JSONL by default; fallback on failure.
- D-14: 100% local; 10% recommended for production; tail sampling in M3.
- D-15: Windows/Linux; macOS is a non-goal.
- D-17: LTS + latest STS.
- D-18/D-19: Research branches and v2 investment.
- D-23: GitHub Pages; no domain purchase in M1.
- D-24: win-arm64 is a non-goal; linux-arm64 is optional.
- D-25: Trademark deferred; NuGet placeholder in M0; CLA reviewed before the first external PR.