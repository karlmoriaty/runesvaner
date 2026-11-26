# Documentation and Community Operations

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Initiator, maintainers, documentation contributors |
| Related documents | `10-open-source-governance.md`, `11-roadmap.md`, `15-decision-log.md` |

---

## 1. Goals

1. Enable new users to see their first diagnostic session within **5 minutes**.
2. Enable engineers to understand the data model and integration approach within **30 minutes**.
3. Enable contributors to complete a local build and their first PR within **1 hour**.
4. Keep documentation and code **same-source, same-version, verifiable**.

---

## 2. Documentation site technology selection

| Option | Pros | Cons | Recommendation |
| --- | --- | --- | --- |
| Astro Starlight | Lightweight, Markdown-first, customizable, fast build | Relatively new ecosystem | **Recommended** |
| VitePress | Vue ecosystem, fast | Separated from the React UI | Alternative |
| Docusaurus | Mature, many plugins | Heavy, coupled to React version | Alternative |
| DocFX | .NET-native, strong API reference | Dated visuals, hard to customize | API reference only |

Resolved (D-21): **Astro Starlight** as the documentation site; API reference generated with DocFX and embedded, or use XML documentation directly with hand-written summaries.

Resolved (D-23): hosting uses **GitHub Pages**; M1 does not buy a custom domain; default URL `https://karlmoriaty.github.io/runesvaner/`.

> Decisions D-21 / D-23.

---

## 3. Information architecture

    docs site
    ├── Getting Started
    │   ├── 5-minute quickstart
    │   ├── Install
    │   └── First diagnosis
    ├── Concepts
    │   ├── Diagnostic session
    │   ├── Request & segments
    │   ├── Cost ledger
    │   ├── Sampling
    │   └── Privacy model
    ├── Guides
    │   ├── Microsoft.Extensions.AI
    │   ├── Semantic Kernel
    │   ├── VectorData / RAG
    │   ├── Streaming
    │   ├── Retries & caching
    │   ├── Native AOT
    │   └── OTLP export
    ├── Reference
    │   ├── Configuration
    │   ├── Public API
    │   ├── Event schema
    │   ├── Metrics
    │   └── CLI
    ├── Operations
    │   ├── Storage & retention
    │   ├── Troubleshooting
    │   ├── Security
    │   └── Upgrade guide
    ├── Contributing
    │   ├── Build & test
    │   ├── Architecture
    │   ├── RFC process
    │   └── CLA
    └── Project
        ├── Roadmap
        ├── Governance
        └── Changelog

---

## 4. Page inventory and requirements

### 4.1 Getting Started

| Page | Must include | Acceptance |
| --- | --- | --- |
| 5-minute quickstart | Install, sample, `runesvaner open`, screenshot/GIF | Completed within 5 minutes on a clean machine |
| Install | TFM, package list, AOT notes | Link to version matrix |
| First diagnosis | Waterfall interpretation of a single request | Readers can state "where it is slow" |

### 4.2 Concepts

| Page | Must include |
| --- | --- |
| Diagnostic session | Relationships among session, request, and event |
| Request & segments | Segment types, estimated, overlap, unaccounted |
| Cost ledger | Price table, cache, reasoning, retry, unknown |
| Sampling | Head/tail sampling, weighting, bias |
| Privacy model | Content levels, hashing, export, retention |

### 4.3 Guides

Each Guide must:

1. Have a runnable minimal example.
2. State prerequisites and versions.
3. Have a "verify success" step.
4. Have "common errors".
5. Contain code snippets that compile (CI check).

### 4.4 Reference

| Page | Source |
| --- | --- |
| Configuration | `RunesvanerOptions` + default value table |
| Public API | XML documentation + DocFX |
| Event schema | `06-observability-model.md` + JSON Schema |
| Metrics | Metric table + buckets |
| CLI | Commands, parameters, exit codes |

### 4.5 Operations

| Page | Content |
| --- | --- |
| Storage & retention | Directory, permissions, cleanup, disk full |
| Troubleshooting | Symptom → cause → resolution |
| Security | Threat model, vulnerability disclosure, Studio security |
| Upgrade guide | Version differences, breaking changes, migration |

---

## 5. Writing guidelines

### 5.1 Principles

- **Executable**: Every step can be followed.
- **Verifiable**: Every page has "what you should see".
- **Versioned**: Mark applicable versions; archive outdated pages.
- **Not exaggerated**: Do not write "ultimate performance" or "revolutionary".
- **No AI tone**: Avoid "let us dive deep" or "in today's fast-moving era".
- **Language strategy (D-16)**: English full; Simplified Chinese and Standard German tiered per `18-localization-scope.md` (L1 full translation, L2 condensed, L3 redirect); UI complete in three languages.

### 5.2 Code snippets

- Use real compilable code, not pseudocode.
- Mark file names and context.
- Pin dependency versions.
- Avoid `...` omitting key logic.
- CI uses `dotnet build` + snippet extraction for verification.

### 5.3 Screenshots and GIFs

- Use real data (synthetic), not mock fake images.
- Dark theme, 1280×800 or 1440×900.
- Mark version and date.
- GIF ≤ 5 MB, with textual steps.

### 5.4 Glossary

| Term | Definition |
| --- | --- |
| Diagnostic Session | A single collection session |
| Request | A single logical AI operation |
| Segment | A time interval within a request |
| TTFT | Time to the first non-empty output token |
| TTFB | Time to first byte |
| Unaccounted | Time not covered by segments |
| Estimated | Derived rather than directly measured |
| Price Table | Versioned price table |

The glossary must be Chinese-English aligned to avoid multiple translations of the same concept.

---

## 6. Documentation quality metrics

| Metric | Target | Collection method |
| --- | --- | --- |
| Quickstart completion rate | ≥ 80% | Trial questionnaire |
| Documentation search failure rate | ≤ 10% | Site analytics (privacy-friendly) |
| Proportion of issues labeled "documentation" | ≤ 5% | GitHub labels |
| Broken links | 0 | CI link checker |
| Code snippet build failures | 0 | CI |
| Outdated pages (> 2 versions) | 0 | Quarterly review |
| L1 three-language sync rate | 100% | Release check |

---

## 7. Community channels

| Channel | Purpose | Enabled |
| --- | --- | --- |
| GitHub Issues | Bugs, feature requests | Yes |
| GitHub Discussions | Q&A, use cases, RFC discussion | Yes |
| GitHub Security Advisories | Vulnerabilities | Yes |
| Discord / Slack | Real-time chat | Resolved not enabled (D-22) |
| Mailing list | Announcements | Not for now |
| X / Mastodon / LinkedIn | Release announcements | Yes (low frequency) |

Principle: **the fewer channels, the better**, to avoid support fragmentation. Route all Q&A back to Discussions where possible for searchability.

---

## 8. Issue and PR operations

### 8.1 Issue triage

| Label | Response target |
| --- | --- |
| `bug` + `P0` | 48 hours |
| `bug` + `P1` | 7 days |
| `enhancement` | 14 days |
| `question` | Best effort |

### 8.2 PR review

- First contribution: provide initial feedback within 48 hours.
- Review comments are specific and actionable.
- Use `good first issue` to guide.
- Do not require contributors to do unrelated refactoring.
- Thank contributors after merge.

### 8.3 Release announcements

| Channel | Content |
| --- | --- |
| GitHub Releases | Changes, migration, contributors |
| Discussions | Discussion thread |
| Blog | Technical depth |
| Social | Short announcement + link |

---

## 9. Content marketing

| Content | Goal | Frequency |
| --- | --- | --- |
| Technical article: "Where the 8 seconds of a RAG request went" | Communicate core value | From M2 |
| Demo video (3 minutes) | Reduce comprehension cost | M2 |
| Benchmark report | Build credibility | Every milestone |
| Interoperability guides with Langfuse/Phoenix | Leverage | M3 |
| Conference talks | Community | M3–M4 |
| Awesome .NET / AI lists | Exposure | M2 |

Principle: **give conclusions and data first, then talk about the product**. No clickbait.

---

## 10. Support policy

| Type | Commitment |
| --- | --- |
| Community support | Best effort, no SLA |
| Security vulnerabilities | 48-hour response |
| Commercial support | Not provided for now |
| Old versions | Security fixes only, LTS + latest STS |
| Data migration | Provide tools and documentation |

---

## 11. Contributor experience

### 11.1 Local build

Goal: a single command.

    git clone https://github.com/<owner>/runesvaner-dotnet.git
    cd runesvaner-dotnet
    dotnet build
    dotnet test

Requirements:

- `global.json` pins the SDK.
- `Directory.Build.props` unifies analyzers.
- Docker not required (optional).
- Real API key not required (mock provider).
- devcontainer optional.

### 11.2 First PR

- `good first issue` list.
- Every issue has file locations, acceptance criteria, and a mentor.
- PR template includes a checklist.
- Fast CI feedback (≤ 10 minutes).

### 11.3 Contributor recognition

- `CONTRIBUTORS.md` or Release acknowledgments.
- Invite major contributors as Reviewers.
- No gamified "contributor levels".

---

## 12. Documentation and code synchronization

| Mechanism | Description |
| --- | --- |
| Update in the same PR | Behavior/API changes must update documentation |
| Link checking | CI checks internal links |
| Code snippet compilation | Extract from Markdown and build |
| Version marking | Mark applicable version at the top of each page |
| Quarterly review | Archive outdated content |
| Single source of truth | Data model follows `06`, API follows XML |

---

## 13. Anti-patterns

- Documentation and code out of sync.
- Quickstart requires 10 steps.
- Outdated screenshots.
- Overpromising performance.
- Using AI to generate large blocks of empty talk.
- Treating design documents as user documentation.
- Too many community channels, none maintained.
- Not responding to issues.
- Releasing without a changelog.

---

## 14. Milestone mapping

| Milestone | Documentation deliverables |
| --- | --- |
| M0 | Design documents, governance files, README |
| M1 | Quickstart, MEAI guide, CLI reference; L1 three languages; GitHub Pages |
| M2 | Cost, privacy, Studio, troubleshooting |
| M3 | Documentation site, SK/VectorData, AOT, migration |
| M4 | API reference, upgrade guide, security whitepaper |

---

## 15. Resolution status

D-16, D-21, D-22, D-10, and D-23 are resolved:

- D-16: English full; Chinese/German tiered per `18-localization-scope.md`; UI complete in three languages.
- D-21: Astro Starlight.
- D-22: GitHub Issues + Discussions.
- D-23: GitHub Pages; M1 does not buy a domain.
- D-10: Commercial support not provided for now.