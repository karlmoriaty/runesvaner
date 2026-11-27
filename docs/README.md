# Runesvaner Design Documentation

> Current stage: **M1 complete; M2 cost engine in progress.** Design documentation and a runnable skeleton coexist.
> Repository: https://github.com/karlmoriaty/runesvaner
> English is the source of truth. Chinese legacy full translation: [`zh-Hans/README.md`](zh-Hans/README.md).

---

## 1. Reading paths

### 1.1 Project owner (about 30 minutes)

1. `00-project-charter.md`: why, what, and what not.
2. `01-market-research.md`: evidence and recommendation for the two slices.
3. `15-decision-log.md`: D-01–D-25 decision records.
4. `11-roadmap.md`: milestones and exit criteria.

### 1.2 Engineering (about 2 hours)

1. `05-technical-design.md`: architecture, threading, overhead.
2. `06-observability-model.md`: data model and formulas.
3. `07-api-design.md`: public API.
4. `08-native-aot-strategy.md`: AOT constraints.
5. `12-testing-and-benchmarks.md`: tests and benchmarks.
6. `14-hidden-requirements.md`: details that are easy to miss.

### 1.3 Design (about 1 hour)

1. `04-ux-and-visual-design.md`: design language, color, layout.
2. `02-problem-and-personas.md`: users and scenarios.
3. `16-brand-naming.md`: naming and brand.
4. `18-localization-scope.md`: English complete; Chinese/German core summaries.

### 1.4 Contributors (about 30 minutes)

1. `10-open-source-governance.md`: license, CLA, governance.
2. `17-docs-and-community.md`: build, issues, PRs.
3. `11-roadmap.md`: current milestone and available work.
4. `19-open-implementation-decisions.md`: implementation decisions.

---

## 2. Document map

| Document | Content | Status |
| --- | --- | --- |
| `00-project-charter.md` | Vision, scope, success metrics, kill criteria | Accepted |
| `01-market-research.md` | Market, competitors, two-slice scoring, sources | Draft |
| `02-problem-and-personas.md` | Pain points, personas, JTBD, journey, objections | Draft |
| `03-prd.md` | Functional requirements, acceptance criteria, NFRs | Accepted |
| `04-ux-and-visual-design.md` | Design language, color, layout, components, logo | Draft |
| `05-technical-design.md` | Architecture, modules, threading, overhead, cost engine | Draft |
| `06-observability-model.md` | Spans, segments, metrics, cost, schema | Draft |
| `07-api-design.md` | Public API, DI, options, extension points | Draft / API not frozen |
| `08-native-aot-strategy.md` | AOT constraints, matrix, CI, size | Draft |
| `09-security-privacy.md` | Threat model, privacy, Studio security | Draft |
| `10-open-source-governance.md` | License, CLA, governance, release | Draft / CLA pending legal review |
| `11-roadmap.md` | M0–M4 and v2 | Draft |
| `12-testing-and-benchmarks.md` | Test pyramid, benchmark gates | Draft |
| `13-risk-register.md` | 24 risks and mitigations | Draft |
| `14-hidden-requirements.md` | 60 hidden requirements | Draft |
| `15-decision-log.md` | **D-01–D-25 decision records** | **Accepted** |
| `16-brand-naming.md` | Runesvaner naming, NuGet check, trademark status | Accepted |
| `17-docs-and-community.md` | Docs site, community, release, contributor experience | Draft |
| `18-localization-scope.md` | Localization tiers, glossary, translation maintenance | Accepted |
| `19-open-implementation-decisions.md` | D-23–D-25 decision records | Accepted |
| `benchmarks-baseline.md` | Recorder benchmark baseline | Draft |
| `../pricing/README.md` | Price table schema and illustrative sample | Draft |
| `zh-Hans/` | Chinese legacy full translation; will be condensed per D-16/D-18 | Legacy |

---

## 3. Status legend

| Status | Meaning |
| --- | --- |
| Draft | Open for discussion; may change |
| Review | Awaiting owner/maintainer review |
| Accepted | Decided; implementation may rely on it |
| Superseded | Replaced by a newer document; kept for history |
| Blocking | Historical state; there are no blocking items now |
| Legacy | Non-English translation kept for continuity; English governs |

---

## 4. Single source of truth

| Topic | Authoritative document |
| --- | --- |
| Product scope and non-goals | `00-project-charter.md` |
| Features and acceptance criteria | `03-prd.md` |
| Architecture and modules | `05-technical-design.md` |
| Data model and formulas | `06-observability-model.md` |
| Public API | `07-api-design.md` + XML docs |
| AOT constraints | `08-native-aot-strategy.md` |
| Privacy and security | `09-security-privacy.md` |
| Governance and CLA | `10-open-source-governance.md` |
| Roadmap | `11-roadmap.md` |
| Decisions | `15-decision-log.md` / `19-open-implementation-decisions.md` |

If documents conflict, this table wins; add a "see X" note in the conflicting document.

---

## 5. Maintenance rules

1. Update documentation in the same PR as code; a docs-only PR needs one reviewer.
2. Keep the metadata table at the top of each document (status, version, date, audience, related docs).
3. Record breaking decisions in `15-decision-log.md`.
4. Mark obsolete documents `Superseded`; do not delete them.
5. Check links and stale content quarterly.
6. English is the source of truth. Chinese/German are core summaries or redirects; safety and legal content must remain complete.
7. Do not claim undecided items are decided.

---

## 6. Decision status

D-01–D-25 are fully decided. See `15-decision-log.md` and `19-open-implementation-decisions.md`.

Key outcomes:

1. D-01: AI workload diagnostics first; AOT is a constraint and v2 research branch.
2. D-02: product name `Runesvaner`; GitHub repository `runesvaner`.
3. D-03: Apache-2.0.
4. D-04: ICLA + CCLA.
5. D-05: `net10.0`; evaluate `net11.0` at M3.
6. D-06: React 19 + TypeScript + Vite + Redux Toolkit + RTK Query.
7. D-07: library SQLite default; CLI AOT JSONL default; fallback on failure.
8. D-08: built-in price snapshot + user overrides; decimal + ToEven; USD.
9. D-09: content capture `None` by default.
10. D-10: no commercialization; unbundled sponsorship only; no hosted service.
11. D-11: individual ownership; 36-month inactivity succession clause.
12. D-12: light theme allowed; must avoid "AI aesthetic".
13. D-13: zero-dependency core; optional OTLP exporter.
14. D-14: local 100%, production guidance 10%; tail sampling at M3.
15. D-15: Windows 10/11 + Linux; macOS non-goal.
16. D-16: English UI default; zh-Hans and de-DE selectable; English docs complete, Chinese/German core summaries.
17. D-17: xUnit v3; MEAI 10.x/11.x contract tests.
18. D-18: AOT heap analysis research branch only.
19. D-19: Roslyn AOT analyzer at v2.
20. D-20: no mandatory third-party compatibility.
21. D-21: Astro Starlight.
22. D-22: GitHub Issues + Discussions.
23. D-23: GitHub Pages; no custom domain at M1.
24. D-24: no win-arm64 in v1; win-x64 + linux-x64, linux-arm64 optional.
25. D-25: trademark suspended; NuGet root package at M0; CLA before first external PR.

---

## 7. Next steps

M1 is complete with one documented caveat: SQLite AOT remains blocked by `dotnet/efcore#36068`; the supported AOT path is JSONL, and library SQLite falls back on failure.

M1 evidence:

- Public repository `karlmoriaty/runesvaner`.
- `LICENSE`, `NOTICE`, `.gitignore`, `Directory.Build.props`, `global.json`.
- `Runesvaner.sln`, 10+ projects, console/RAG/AOT samples.
- 21 tests: unit, JSONL/SQLite integration, chat, streaming stats, cancellation, error, failure injection, cost, price table loader, OTel exporter, Studio API/detail, session aggregation, query stores.
- Benchmark baseline: 324.9 ns mean, 3.70 us P95.
- CI: Windows/Linux build and tests; Windows Native AOT smoke with non-streaming, streaming, and error paths.

M2 in progress:

1. Cost engine core: `PriceTable`, `PriceEntry`, `CostBreakdown`, tier/cached/reasoning/batch discount, unknown-model handling, recorder integration.
2. Price table loader: JSON schema, aliases, validation, `pricing validate`, and `costs` aggregation.
3. Streaming stats: chunk count, TTFT, ITL P50/P95, TPS when usage is available, finish reason, cancellation/interruption.
4. OpenTelemetry exporter: optional OTLP package, span/metric mapping, failure-isolated store decorator.
5. Studio: read-only cost ledger API + React 19/RTK Query cost page + request detail waterfall + session browser + compare view.
6. Query stores: shared JSONL/SQLite query contract and session summaries.
7. Next: verified built-in snapshot, GitHub Pages, NuGet placeholder.
3. GitHub Pages and NuGet root package placeholder remain external tasks.