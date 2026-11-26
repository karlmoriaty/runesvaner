# Risk Register

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Founder, maintainers |
| Related documents | `00-project-charter.md`, `11-roadmap.md`, `15-decision-log.md` |

---

## 1. Scoring Method

| Dimension | Value |
| --- | --- |
| Probability P | 1 Low / 2 Medium / 3 High |
| Impact I | 1 Low / 2 Medium / 3 High |
| Exposure E | P × I (1–9) |
| Priority | E ≥ 6 High; 4–5 Medium; ≤ 3 Low |

Review cadence: review all high risks at the end of each milestone; review the Top 10 monthly.

---

## 2. Risk Matrix

| ID | Risk | Category | P | I | E | Priority |
| --- | --- | --- | ---: | ---: | ---: | --- |
| R-01 | Microsoft provides equivalent latency breakdown and cost attribution directly in MEAI | Market | 2 | 3 | 6 | High |
| R-02 | Langfuse / Phoenix ship native .NET SDKs, squeezing the space | Market | 2 | 2 | 4 | Medium |
| R-03 | Cost calculation errors destroy trust | Product | 2 | 3 | 6 | High |
| R-04 | Latency segmentation estimates are not credible; users believe the numbers are "made up" | Product | 3 | 3 | 9 | High |
| R-05 | The streaming wrapper breaks cancellation/exception semantics | Technical | 2 | 3 | 6 | High |
| R-06 | SQLite AOT compatibility (#36068) / OTel optional dependency failure | Technical | 3 | 2 | 6 | High |
| R-07 | Collection overhead exceeds targets and users remove it | Technical | 2 | 3 | 6 | High |
| R-08 | Scope creep; milestones cannot be delivered | Operations | 3 | 3 | 9 | High |
| R-09 | Individual maintainer burnout; the project stalls | Operations | 3 | 3 | 9 | High |
| R-10 | CLA process not automated, hindering contributions | Community | 2 | 2 | 4 | Medium |
| R-11 | Runesvaner trademark not searched (deferred); conflict may arise later | Legal | 2 | 2 | 4 | Medium |
| R-12 | Price table source/accuracy/copyright issues | Legal | 2 | 2 | 4 | Medium |
| R-13 | Privacy incident (content leak) | Security | 1 | 3 | 3 | Low (but zero tolerance) |
| R-14 | Supply chain attack (dependencies/build) | Security | 1 | 3 | 3 | Low (but zero tolerance) |
| R-15 | Data schema instability breaks consumers | Technical | 2 | 2 | 4 | Medium |
| R-16 | MEAI API breaking changes | Technical | 2 | 2 | 4 | Medium |
| R-17 | UI "AI-like" feel leads to poor reputation | Product | 2 | 2 | 4 | Medium |
| R-18 | High-cardinality metrics cause monitoring cost explosion | Technical | 2 | 2 | 4 | Medium |
| R-19 | Sampling bias causes incorrect cost statistics | Product | 2 | 3 | 6 | High |
| R-20 | Local Studio security vulnerability | Security | 2 | 3 | 6 | High |
| R-21 | Commercial vendors (Progress and others) preempt MEAI observability | Market | 2 | 2 | 4 | Medium |
| R-22 | .NET version support burden is too heavy | Technical | 2 | 2 | 4 | Medium |
| R-23 | Insufficient documentation/examples cause adoption failure | Operations | 3 | 2 | 6 | High |
| R-24 | Governance conflict (Owner vs. maintainers) | Community | 2 | 2 | 4 | Medium |

---

## 3. Key Risks and Mitigations

### R-04 Latency segmentation estimates are not credible (E=9)

**Trigger signals**: users report "this number is wrong"; repeated measurements of the same request differ by > 10%; provider queue time cannot be distinguished from network time.

**Mitigations**:

1. Segment layering: measured / event-derived / estimated / residual, all labeled with `measurement`.
2. The UI does not use precision visuals (for example, gauges without error bars); estimated segments use hatching and dashed lines.
3. Documentation explicitly states that "provider queue includes network RTT".
4. Provide a "raw events" view so users can verify it themselves.
5. Show `unknown` instead of 0 for segments that cannot be measured.

**Residual risk**: medium. The black box of external services cannot be fully eliminated.

### R-08 Scope creep (E=9)

**Trigger signals**: the Must list keeps growing; a milestone slips by more than one cycle; a PR involves non-goal features.

**Mitigations**:

1. Write the non-goals list into the charter; the PR template requires a check.
2. A new Must must replace an existing Must, or the milestone is postponed.
3. Each milestone has only one "main dish"; the rest are side dishes.
4. Review scope monthly.

### R-09 Maintainer burnout (E=9)

**Trigger signals**: issue response time > 7 days; two consecutive milestones without external contributors; founder investment declines.

**Mitigations**:

1. CLA plus a maintainer ladder; at least 2 people able to release.
2. Automation: CI, release, documentation, dependency updates.
3. Limit the support scope (only LTS + latest STS).
4. Public roadmap to reduce communication cost.
5. Set "maintenance windows"; do not promise immediate response outside them.
6. Succession plan (see `10`).

### R-01 Absorption by the official offering (E=6)

**Trigger signals**: MEAI ships cost/waterfall features; the .NET team announces related tooling.

**Mitigations**:

1. Differentiate on "local-first + AOT + developer experience", not on a single field.
2. Interoperate with OTel; the stronger the official offering, the more Runesvaner resembles a "front-end profiler".
3. If the official offering covers it, pivot to Native AOT startup/size or the cost ledger as a single point.
4. Do not compete head-on on standards development.

### R-03 Cost errors (E=6)

**Trigger signals**: difference from the bill > 5%; users report unknown models billed as 0; the price table is stale.

**Mitigations**:

1. Price table versioning, source, effective time, checksum.
2. `unknown` for unknown models; do not guess.
3. Verify against billing samples from at least 2 providers.
4. The UI shows "estimated/verified" status.
5. Provide user overrides and a difference report.
6. Cost calculation unit tests cover boundaries.

### R-05 Broken streaming semantics (E=6)

**Trigger signals**: cancellation stops working; exceptions are swallowed; chunk order changes; memory grows.

**Mitigations**:

1. Minimal wrapper: record timestamps only; do not buffer content.
2. Dedicated test matrix (normal/empty chunk/exception/cancellation/timeout/slow consumer).
3. Do not call `ToList` inside the wrapper.
4. Contract tests with MEAI.
5. The code review checklist includes streaming semantics.

### R-06 SQLite AOT compatibility (#36068) (E=6)

**Trigger signals**: AOT smoke fails to initialize SQLite; `e_sqlite3` is missing; WAL/query exceptions.

**Mitigations**:

1. SQLite by default in the library/application; JSONL by default when the CLI is published with Native AOT.
2. With `--storage sqlite`, attempt it; on failure fall back to JSONL and record `storage.fallback=jsonl`.
3. M1 validates the workaround: native asset copy, `SQLitePCLRaw` initialization, WAL, queries; record the result in an ADR.
4. If not feasible, M2 provides a framework-dependent CLI (SQLite default) that does not replace the AOT CLI.
5. Documentation explicitly states the host difference of "SQLite by default" under AOT to avoid misleading users.

### R-07 Overhead exceeds targets (E=6)

**Trigger signals**: P95 > 1 ms; CPU > 1%; allocations > 2 KB; users disable it.

**Mitigations**:

1. Sampling upfront; zero records when not sampled.
2. Structs, pre-allocation, no LINQ, no logging.
3. Async disk write; drop when the queue is full.
4. CI benchmark thresholds.
5. Provide a "minimal mode" (root span + usage only).
6. Publish overhead reports.

### R-19 Sampling bias (E=6)

**Trigger signals**: cost under sampling differs from full capture by > 10%; tail sampling keeps only slow requests.

**Mitigations**:

1. Record `sampling.probability`.
2. Use weighting in aggregation.
3. The UI shows the sampling mode and confidence.
4. 100% locally by default; 10% recommended in production.
5. Count tail sampling separately; do not mix it with full capture.

### R-20 Studio security (E=6)

**Trigger signals**: accessible without a token; Host validation fails; XSS reported.

**Mitigations**:

1. Loopback binding + random token.
2. Host/Origin validation, CSP, no external resources.
3. React escaping; `dangerouslySetInnerHTML` prohibited.
4. Security tests included in CI.
5. Penetration test before 1.0.

### R-23 Insufficient documentation (E=6)

**Trigger signals**: repeated "how do I integrate" in issues; trial users get stuck at the first step; high README bounce rate.

**Mitigations**:

1. A 5-minute quick start that actually runs.
2. Every Must feature has an example and troubleshooting guidance.
3. Documentation in the same PR as code.
4. Screen recordings/GIFs (M2).
5. Documentation site search (M3).

---

## 4. Risks and Milestones

| Milestone | Risks that must be mitigated below "Medium" |
| --- | --- |
| M0 | R-11 trademark (deferred), R-12 price table source, R-24 governance |
| M1 | R-05 streaming, R-07 overhead, R-15 schema |
| M2 | R-03 cost, R-04 segmentation, R-20 Studio, R-19 sampling |
| M3 | R-02 competitors, R-10 CLA automation, R-18 high cardinality |
| M4 | R-01 official offering, R-09 maintenance, R-13/R-14 security |

---

## 5. Zero-Tolerance Risks

The following risks must be designed so that they "cannot happen" or "are controllable when they do", even if their probability is low:

| Risk | Design principle |
| --- | --- |
| Content leak | No collection by default; export redaction; file permissions |
| Secret leak | Never collect Authorization; log filtering |
| Business dragged down | Collection circuit breaking; async; drop |
| Supply chain poisoning | Dependency minimization; signing; SBOM; locking |
| Local UI accessed remotely | Loopback + token + Host validation |

---

## 6. Risk Review Log

| Date | Milestone | Change |
| --- | --- | --- |
| 2026-11-25 | M0 | Initial registration |

---

## 7. Decision Status

D-01–D-25 are all decided; see `15-decision-log.md` and `19-open-implementation-decisions.md`. Directly relevant to this register:

- D-01: AI workload diagnostics first (R-01/R-02).
- D-02: Runesvaner; trademark deferred (D-25, R-11).
- D-03/D-04: Apache-2.0 / ICLA+CCLA (R-10).
- D-07: SQLite by default in the library; JSONL by default in CLI AOT; fallback on failure (R-06).
- D-10: no commercialization for now (R-09/R-13).
- D-18: AOT research branch (R-06).