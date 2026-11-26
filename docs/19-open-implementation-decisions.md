# Implementation-Level Decision Record: D-23–D-25

| Field | Value |
| --- | --- |
| Document status | **Resolved** |
| Version | 1.0 |
| Decision date | 2026-11-26 |
| Decision maker | Project sponsor |
| Related documents | `11-roadmap.md`, `15-decision-log.md`, `16-brand-naming.md`, `17-docs-and-community.md` |

> D-01–D-25 are all resolved; this document records the final conclusions for 3 implementation-level items, D-23–D-25.
> These conclusions do not change the product positioning, architecture, or governance; they only affect the first batch of M0/M1 infrastructure and release preparation.

---

## 1. Decision Summary

| ID | Item | Final decision | Status |
| --- | --- | --- | --- |
| D-23 | Documentation site hosting and domain | GitHub Pages; do not purchase a domain for M1 | Resolved |
| D-24 | Windows ARM64 | v1 does not support `win-arm64`; `win-x64` + `linux-x64`, `linux-arm64` optional | Resolved |
| D-25 | Release preparation timing | Trademark search deferred; NuGet root package placeholder in M0; CLA reviewed before the first external PR | Resolved |

---

## 2. D-23 Documentation Site Hosting and Domain

**Decision**:

- The documentation site is hosted on **GitHub Pages**.
- Do not purchase a custom domain for M1.
- Default URL: `https://karlmoriaty.github.io/runesvaner/`.
- Evaluate domains such as `runesvaner.dev` after 1.0.

**Rationale**:

1. The project is already on GitHub; Pages integrates most directly with the repository and Actions.
2. An early-stage personal project does not need a domain budget and renewal burden.
3. The candidate domains currently have no DNS resolution; registration can still be evaluated later.

**Impact**:

- The M1 documentation site CI uses GitHub Actions → Pages.
- The README and NuGet package project links use the Pages URL.
- If the domain is switched in the future, redirects must be set up and the CNAME updated.

---

## 3. D-24 Windows ARM64

**Decision**:

- v1 **does not support** `win-arm64`.
- `win-x64` and `linux-x64` are guaranteed.
- `linux-arm64` is an optional validation target and does not block release.
- If a clear demand emerges later, it can be a v2 community-supported item.

**Rationale**:

1. The target users are primarily Windows x64 and Linux x64.
2. SQLite native assets under AOT are already a risk point; adding `win-arm64` would amplify CI and validation costs.
3. A personal project prioritizes the quality of the two main platforms.

**Impact**:

- The RID matrix in `08-native-aot-strategy.md`: do not add `win-arm64`.
- The CI matrix in `12-testing-and-benchmarks.md`: do not add `win-arm64`.
- NuGet packages do not publish `win-arm64` native assets.

---

## 4. D-25 Release Preparation Timing

**Decision**:

| Sub-item | Final decision | Notes |
| --- | --- | --- |
| Trademark search (classes 9/42) | **Deferred** | The sponsor currently has no trademark lawyer/budget; raise it later when needed. Not an M0 gate; evaluate again before the formal public preview or 1.0 |
| NuGet root package placeholder (0.0.1) | **M0** | The 13 planned package IDs currently return 404; the placeholder cost is low and prevents squatting |
| CLA lawyer review | **Before the first external PR is accepted** | Does not block individual development; executable text must exist before accepting external contributions |

**Rationale**:

1. Deferring the trademark search is the sponsor's explicit decision; the name has passed initial screening for NuGet/GitHub/domain availability, so it can be used during the working-name phase.
2. The NuGet root package placeholder is a low-cost, reversible brand protection action.
3. The CLA only needs to be completed before the first external PR; it does not need to block code development in M0.

**Impact**:

- `11-roadmap.md`: the M0 exit criteria no longer require "trademark search completed", but instead "trademark search deferred, risk recorded"; the NuGet placeholder is still retained.
- `16-brand-naming.md`: the trademark status changes to "deferred, evaluate again before the formal preview".
- `13-risk-register.md`: R-11 trademark risk is retained but marked "deferred".
- `10-open-source-governance.md`: the CLA review timing is before the first external PR.

---

## 5. Action Items

| Item | Status | Owner | Timing |
| --- | --- | --- | --- |
| Git commit email | **Completed**: the user confirmed `3070687583@qq.com` is bound to the GitHub account `karlmoriaty` | User | Completed |
| NuGet account registration/verification | Requires the user to perform personally | User | Before M0 |
| NuGet root package placeholder (0.0.1) | Pending (requires a NuGet API key) | User | M0 |
| Trademark search | **Deferred** | User | Later when needed |
| CLA lawyer review | Pending | User | Before the first external PR |
| GitHub repository `runesvaner` | Created (public) | Completed | 2026-11-26 |
| Documentation site GitHub Pages | Pending configuration | After kickoff | M1 |
| German native reviewer | To be determined | Sponsor/community | Before M1 |
| Chinese technical reviewer | To be determined | Sponsor/community | Before M1 |

---

## 6. Fixed Items That Do Not Require a Decision

| Item | Value |
| --- | --- |
| Repository name | `runesvaner` |
| Product name / package name / documentation site name | `Runesvaner` |
| Repository visibility | Public |
| Default branch | `main` |
| License | Apache-2.0 |
| Target framework | `net10.0` |
| Platform | Windows 10/11 + Linux; macOS is not a target |
| Storage default | Library/in-app SQLite; CLI AOT defaults to JSONL |

---

## 7. Follow-Up

D-23–D-25 are recorded. The project **kicked off on 2026-11-26**, and the first batch of actions is complete:

1. Created the public repository `karlmoriaty/runesvaner`.
2. Ran `git init -b main`, configured `origin`, and pushed the first commit.
3. Added `LICENSE`, `NOTICE`, `.gitignore`, `Directory.Build.props`, `global.json`.
4. Established `Runesvaner.sln` and the M1 project skeleton (Abstractions, Core, MEAI, JSONL, SQLite, CLI, tests, samples).
5. CI passed: Windows/Linux build and test + Windows Native AOT smoke.
6. Still to be completed: GitHub Pages, NuGet root package placeholder (requires a NuGet account/API key), RAG sample, benchmark baseline, cost engine, Studio, OTLP.

---

## 8. Change History

| Date | Version | Change |
| --- | --- | --- |
| 2026-11-26 | 0.1 | Initial D-23–D-25 pending resolution |
| 2026-11-26 | 1.0 | Sponsor resolved all items as recommended; trademark search changed to deferred; Git email confirmed as bound |