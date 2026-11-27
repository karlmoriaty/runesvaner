# Decision Record: D-01–D-25 Resolutions

| Field | Value |
| --- | --- |
| Document status | **Resolved** |
| Version | 1.3 |
| Resolution date | 2026-11-25 |
| Resolved by | Project initiator |
| Recorded by | CodeArts design agent |
| Related documents | All design documents |

> This document records the initiator's final resolutions for D-01–D-25. All items have moved from "pending" to "resolved"; this file prevails during implementation.
> Items that still require external completion (trademark search, CLA legal review, NuGet reservation, repository name/domain) are listed separately in Section 4 and are not product decisions.

---

## 1. Resolution summary

| ID | Topic | Final resolution | Status |
| --- | --- | --- | --- |
| D-01 | Primary track | Launch AI workload diagnostics first; Native AOT as a first-class constraint and v2 experimental branch | Resolved |
| D-02 | Project name | **Runesvaner**; accept not registering a trademark during the working-name phase | Resolved |
| D-03 | License | Apache-2.0 | Resolved |
| D-04 | CLA | ICLA + CCLA; automation tooling as an implementation recommendation | Resolved |
| D-05 | Target framework | `net10.0` as primary; evaluate `net11.0` in M3 | Resolved |
| D-06 | Frontend stack | React 19 + TS + Vite + Redux Toolkit + RTK Query; CSS Modules + design tokens; Radix Primitives; uPlot + custom waterfall | Resolved |
| D-07 | Default storage | **SQLite default** in library/in-app; JSONL default for CLI AOT; fallback on failure | Resolved (revised 2026-11-26) |
| D-08 | Price table | Built-in snapshot (version/source/checksum) + user overrides; remote disabled by default; decimal + ToEven; USD storage | Resolved |
| D-09 | Content capture | Default `None` | Resolved |
| D-10 | Commercialization | Not commercialized for now; accept unbundled sponsorship; no hosted service | Resolved |
| D-11 | Trademark and succession | Held personally; if inactive for **36 consecutive months**, a successor may be nominated via RFC and the trademark/domain transferred | Resolved |
| D-12 | Light theme | Agreed; M2 prioritizes dark, 1.0 provides light; must avoid "AI feel" | Resolved |
| D-13 | OTel strategy | Core has zero dependencies; `Runesvaner.Exporters.OpenTelemetry` optional; no additional Activity created by default | Resolved |
| D-14 | Sampling | Local 100%; production documentation recommends 10%; tail sampling introduced in M3, disabled by default | Resolved |
| D-15 | Platforms | Windows 10/11 + Linux; **macOS out of scope** | Resolved |
| D-16 | Languages | UI defaults to English, switchable to Simplified Chinese and Standard German (`de-DE`); documentation primarily English, Chinese/German condensed versions | Resolved |
| D-17 | Testing and version support | Accept xUnit v3 + MEAI 10.x/11.x contract tests; LTS + latest STS | Resolved |
| D-18 | AOT heap analysis | Not on the mainline; retain research branch and issue, community maintainers welcome | Resolved |
| D-19 | Roslyn AOT analyzer | Invest in v2; may open an issue for developers willing to maintain it | Resolved |
| D-20 | Third-party semantics | No mandatory compatible platforms; primarily OTel GenAI, others per community demand | Resolved |
| D-21 | Documentation site | Astro Starlight | Resolved |
| D-22 | Community channels | GitHub Issues + Discussions | Resolved |
| D-23 | Documentation site hosting | GitHub Pages; M1 does not buy a domain | Resolved |
| D-24 | Windows ARM64 | v1 does not support win-arm64 | Resolved |
| D-25 | Release preparation | Trademark deferred; NuGet M0; CLA before the first external PR | Resolved |

---

## 2. Item-by-item record

### D-01 Primary track

**Resolution**: Launch the AI workload diagnostics tool first; add the Native AOT compatibility diagnostic tool afterward. Treat AOT as a first-class constraint and a v2 experimental branch.

**Rationale**:

- Native AOT has supported EventPipe since .NET 8, so traditional tools are not entirely ineffective; the real gap is managed heap analysis, whose technical risk does not match an individual project.
- AI workload diagnostics has verifiable user value, and there is no dominant player on the .NET side.
- AOT compatibility is a differentiating moat and is more suitable as a constraint than as the initial primary battlefield.

**Impact**: Product positioning, PRD, technical design, and AOT strategy are all written according to "AI first, AOT as constraint".

**Follow-up**: v2 startup conditions are in `11-roadmap.md`; the AOT research issue is in D-18.

---

### D-02 Project name

**Resolution**: The project name is **Runesvaner**; the GitHub repository name is **`runesvaner`**. Accept not registering a trademark during the working-name phase.

**Availability verification (2026-11-25 / 2026-11-26)**:

| Check item | Result |
| --- | --- |
| 13 planned NuGet package IDs (root/core/abstractions/ai/cli/studio/storage.sqlite/storage.jsonl/exporters.opentelemetry/microsoft.extensions.ai/semantic.kernel/vectordata/analyzers) | All 404 (no packages currently) |
| GitHub user/organization/repository `runesvaner` | 404 (no occupation found) |
| GitHub search for repositories with the same name | 0 |
| Domains `runesvaner.dev` / `.com` / `.io` / `.tools` / `getrunesvaner.dev` | No DNS resolution (does not imply registrability) |
| Trademark search | **Not performed; requires a lawyer** |

**Naming conventions**:

| Purpose | Value |
| --- | --- |
| Product name | `Runesvaner` |
| Package prefix | `Runesvaner.*` |
| Namespace | `Runesvaner` |
| CLI | `runesvaner` |
| Configuration section | `Runesvaner` |
| Environment variable prefix | `RUNESVANER_` |
| Data directory | `.runesvaner/` |
| Meter / ActivitySource | `Runesvaner.AI` |
| Type prefix | `Runesvaner*` (e.g. `RunesvanerOptions`) |
| Studio | `Runesvaner Studio` |
| GitHub repository | `runesvaner` (product/packages/documentation site remain `Runesvaner`) |

**Risk**: The name is a coined word, unique but its pronunciation needs confirmation; external availability verification passed (no occupation found on NuGet/GitHub/domains), but trademark and domain registration still need lawyer/WHOIS confirmation. It may be used during the working-name phase; complete the search before formal preview.

**Impact**: All documents, package names, namespaces, and the CLI have been renamed to be consistent; the full verification record is in `16-brand-naming.md`.

---

### D-03 License

**Resolution**: Apache-2.0.

**Rationale**: Explicit patent grant, commercially friendly, consistent with the OpenTelemetry ecosystem; more open than Langfuse (MIT+EE) and Phoenix (ELv2).

**Follow-up**: Add `LICENSE` and `NOTICE` in M0; lawyer review before release.

---

### D-04 CLA

**Resolution**: ICLA + CCLA; automate with CLA Assistant (or an equivalent GitHub Action).

**Rationale**: Clear rights, supports relicensing, facilitates a maintainer ladder; automation reduces friction.

**Follow-up**: Draft ICLA/CCLA, lawyer review; configure the bot; document in `CONTRIBUTING.md`.

---

### D-05 Target framework

**Resolution**: `net10.0` as primary; evaluate `net11.0` in M3.

**Rationale**: .NET 8 reached end of support in November 2026; .NET 10 is LTS; avoid multi-targeting dragging down the CI of an individual project.

**Impact**: All library projects use TFM `net10.0`; MEAI is primarily 10.x, with 11.x contract tests.

---

### D-06 Frontend stack

**Resolution**:

- React 19 + TypeScript + Vite
- Redux Toolkit + RTK Query
- CSS Modules + design tokens
- Radix Primitives (unstyled, accessible)
- uPlot + custom waterfall
- No Tailwind, no default shadcn appearance

**Rationale**: Multi-panel linkage benefits from centralized state; RTK Query simplifies the local API; CSS Modules preserves design control and avoids "AI feel".

**Follow-up**: M2 builds the component library and design tokens; M3 Playwright E2E.

---

### D-07 Default storage

**Resolution (revised 2026-11-26)**:

- **Library/in-app collection**: SQLite by default; automatically fall back to JSONL if AOT initialization fails.
- **CLI**: published with Native AOT, JSONL by default; provide `--storage sqlite` for explicit selection; fall back on failure.
- **M2 evaluation**: framework-dependent CLI (SQLite default) as a supplementary publication, not a replacement for the AOT CLI.

**Basis (verification results)**:

- `dotnet/efcore#36068` "Microsoft.Data.Sqlite and NativeAOT compatibility" is still open.
- The issue states: when a Native AOT console references `Microsoft.Data.Sqlite`, `e_sqlite3` may not be found; the project does not enable `<IsAotCompatible>`; and AOT CI is missing.
- Therefore "CLI AOT + SQLite default" cannot be promised as out-of-the-box behavior.

**Rationale**:

- Retain the core of D-07: SQLite default in the library/in-app, satisfying queries, aggregation, and Studio.
- The CLI, as an AOT tool, prioritizes out-of-the-box usability; JSONL is a fallback with zero native dependencies.
- The dual default is distinguished by host, sacrificing neither the first-class AOT constraint nor the SQLite primary storage.

**Implementation constraints**:

1. The library defaults to `StorageMode.Sqlite` and provides `IRunesvanerQueryStore`.
2. The CLI defaults to `StorageMode.Jsonl`; with `--storage sqlite` it attempts to initialize SQLite.
3. Under any host, if SQLite initialization fails, automatically fall back to JSONL, record `storage.fallback=jsonl` and the reason, and do not block business logic.
4. M1 must complete SQLite AOT validation: native assets, `SQLitePCLRaw` initialization, WAL, queries.
5. If validation finds a viable workaround, M2 may switch the CLI default back to SQLite; if not viable, keep the CLI JSONL default and provide a framework-dependent CLI (SQLite default) in M2.

**Impact**:

- `05-technical-design.md`: storage defaults are distinguished by host.
- `07-api-design.md`: library defaults to `StorageMode.Sqlite`; CLI overrides to `Jsonl`.
- `08-native-aot-strategy.md`: SQLite AOT risk and fallback, CLI JSONL default.
- `03-prd.md`: FR-040/041 descriptions distinguished by host.
- `11-roadmap.md`: M1 validates SQLite AOT; M2 evaluates framework-dependent CLI.
- `12-testing-and-benchmarks.md`: library SQLite + CLI JSONL + fallback tests.
- `13-risk-register.md`: R-06 update.
- `14-hidden-requirements.md`: H-36 update.

---


### D-08 Price table

**Resolution**:

- Built-in snapshot, with version, source, checksum, and retrieval time.
- User overrides take precedence.
- Remote updates are disabled by default and enabled explicitly.
- `decimal` computation; `MidpointRounding.ToEven`; store 6 decimal places, display 4.
- USD storage; display conversion possible, offline by default.

**Rationale**: Traceable, replayable, no guessing, no silent network access.

**Follow-up**: M2 price table schema and CI validation; the community maintenance process for the price table is in `14-hidden-requirements.md` H-59.

---

### D-09 Content capture

**Resolution**: Default `ContentCapture.None`.

**Rationale**: Privacy baseline; latency/cost diagnostics do not need content; `Full` is enabled only explicitly and locally, with warnings and retention limits.

**Follow-up**: M1 implements the default; M2 completes privacy tests and UI warnings.

---

### D-10 Commercialization

**Resolution**: Not commercialized for now; accept unbundled sponsorship; no hosted service.

**Rationale**: Local-first is the core positioning; individual project bandwidth is limited; sponsorship can cover infrastructure and design costs.

**Follow-up**: sponsorship chapter in `10-open-source-governance.md`; no paywall on the official website.

---

### D-11 Trademark and succession

**Resolution**: The project name/Logo is held personally by the initiator; if inactive for **36 consecutive months**, maintainers may nominate a successor via RFC and transfer the trademark/domain.

**Rationale**: Protect the brand from abuse; at the same time avoid bus factor = 1.

**Follow-up**: change the succession clause in `10-open-source-governance.md` to 36 months; lawyer review of the transfer mechanism.

---

### D-12 Light theme

**Resolution**: Agree to provide a light theme; M2 prioritizes dark, 1.0 provides light. Design requirements: warm gray paper feel, not pure white, square corners, low saturation, no purple-green neon, no AI glow.

**Rationale**: The user explicitly accepts it but emphasizes avoiding "AI feel"; light must be designed separately and cannot simply invert dark.

**Follow-up**: `04-ux-and-visual-design.md` adds light theme constraints and acceptance.

---

### D-13 OTel strategy

**Resolution**: Core has zero dependencies; `Runesvaner.Exporters.OpenTelemetry` is an optional package; no additional Activity is created by default.

**Rationale**: MEAI already has `UseOpenTelemetry` built in, and duplicating it would produce double spans; AOT users do not want to be burdened by the OTel footprint.

**Follow-up**: M2 implements OTLP export; the AOT matrix is verified separately; documentation explains span nesting.

---

### D-14 Sampling

**Resolution**: Local 100%; production documentation recommends 10%; tail sampling introduced in M3, disabled by default.

**Rationale**: Local diagnostics need complete data; production needs low overhead; tail sampling has memory cost.

**Follow-up**: M1 head sampling; M3 tail sampling + weighted statistics.

---

### D-15 Platforms

**Resolution**: Target platforms are **Windows 10/11** and **Linux**; **macOS is out of scope** and not promised for v1.

**Rationale**: Target users are mainly Windows 10/11 and Linux professional developers; reduce the CI and test matrix to match individual project bandwidth.

**Impact**:

- Remove `macos-latest` from the CI matrix.
- AOT matrix: `win-x64`, `linux-x64`, `linux-arm64` (optional).
- Documentation no longer claims macOS support.
- If the community demands it in the future, it may be treated as a v2 community-supported item.

---

### D-16 Languages

**Resolution**:

- UI defaults to English (`en-US`), switchable to Simplified Chinese (`zh-Hans`) and Standard German (`de-DE`).
- Documentation is primarily English; Chinese and German may be condensed versions or redirect to the English UI.

**Rationale**: Target users include international .NET developers; German is the initiator's designated third language; full translation of documentation is costly, so adopt an "English primary + condensed versions" strategy.

**Impact**:

- `04-ux-and-visual-design.md` language list update.
- `17-docs-and-community.md` multilingual strategy update.
- `14-hidden-requirements.md` H-60 update.
- The documentation site needs i18n routing and language switching.

---

### D-17 Testing and version support

**Resolution**: Accept the recommended approach:

- xUnit v3 + built-in assertions (fewer dependencies).
- UI E2E uses Playwright (M3).
- MEAI 10.x/11.x contract tests.
- Support policy: LTS + latest STS, with clear EOL.

**Follow-up**: M1 sets up test projects; M3 sets up the contract test matrix.

---

### D-18 AOT heap analysis

**Resolution**: Not on the mainline; retain the research branch and issue, developers willing to maintain it are welcome.

**Rationale**: Managed heap analysis requires runtime-level engineering, and the risk does not match an individual project; but the community may be interested.

**Follow-up**:

- Create the `research/aot-runtime-diagnostics` branch (does not block the mainline).
- Open an issue describing goals, difficulties, and acceptance criteria, labeled `help wanted`.
- Not promised for delivery in v1/v2.

---

### D-19 Roslyn AOT analyzer

**Resolution**: Invest in v2; may open an issue for developers willing to maintain it.

**Rationale**: Matches the initiator's Roslyn technical preference; can detect AOT-incompatible diagnostic code; complements the main product.

**Follow-up**:

- v2 plans `Runesvaner.Analyzers`.
- Open an issue to solicit maintainers and a rule list.
- Not on the critical path for M1–M4.

---

### D-20 Third-party semantics

**Resolution**: There are no mandatory third-party platforms to be compatible with; primarily OTel GenAI semconv, with other adaptations per community demand.

**Rationale**: Reduces the compatibility matrix and commitments; OTel is the industry standard and sufficiently covers major backends.

**Impact**:

- Langfuse / Phoenix compatibility is downgraded from Should to Could.
- OpenInference and others are optional adaptations in v2.
- Documentation does not claim "guaranteed compatibility with all platforms".

---

### D-21 Documentation site

**Resolution**: Astro Starlight.

**Rationale**: Markdown-first, lightweight build, customizable, decoupled from the React UI.

**Follow-up**: M1 sets up the documentation site skeleton; M3 completes navigation and search.

**Implementation note (2026-11-27)**: Astro Starlight installation stalled twice in the local npm dependency graph (no progress after five minutes). GitHub Pages currently builds a dependency-light static site from the same Markdown; Starlight remains the target and should replace the fallback when the npm graph installs reliably.

---

### D-22 Community channels

**Resolution**: GitHub Issues + Discussions; do not create Discord/Slack for now.

**Rationale**: The fewer channels, the better, to avoid support fragmentation; Q&A flows back to Discussions for searchability.

**Follow-up**: M0 opens Discussions categories; M2 sets up issue templates and labels.

---

### D-23 Documentation site hosting and domain

**Resolution**: GitHub Pages; M1 does not buy a domain for now; default URL `https://karlmoriaty.github.io/runesvaner/`.

**Follow-up**: M1 configures GitHub Actions → Pages; evaluate a custom domain after 1.0. See `19-open-implementation-decisions.md`.

---

### D-24 Windows ARM64

**Resolution**: v1 does not support `win-arm64`; guarantee `win-x64` + `linux-x64`, `linux-arm64` optional.

**Follow-up**: Do not add `win-arm64` to the RID/CI matrix; treat as a v2 community item if there is clear demand.

---

### D-25 Release preparation timing

**Resolution**: Trademark search deferred (no lawyer/budget, revisit later); NuGet root package reserved in M0; CLA reviewed before the first external PR.

**Follow-up**: M0 only does the NuGet reservation; evaluate the trademark again before formal public preview or 1.0; CLA review does not block individual development.

---

## 3. Impact of resolutions on documents

| Document | Change points |
| --- | --- |
| `README.md` | Project name, status, support matrix, storage, platforms, roadmap |
| `00-project-charter.md` | Resolution status, v1 scope, AOT research branch, termination conditions |
| `01-market-research.md` | Conclusion changed to "resolved"; heap analysis changed to "retained as research" |
| `02-problem-and-personas.md` | Reference to resolutions |
| `03-prd.md` | SQLite default by host, platforms, languages, third-party compatibility priority |
| `04-ux-and-visual-design.md` | Light theme constraints, three languages |
| `05-technical-design.md` | Library SQLite primary storage, CLI JSONL default, AOT interaction |
| `06-observability-model.md` | Storage consistency description |
| `07-api-design.md` | Library defaults to `StorageMode.Sqlite`; CLI overrides to Jsonl |
| `08-native-aot-strategy.md` | SQLite AOT verification and fallback, CLI JSONL default, macOS removed |
| `09-security-privacy.md` | SQLite file permissions, platform scope |
| `10-open-source-governance.md` | 36-month succession, license/CLA confirmation, language strategy |
| `11-roadmap.md` | M0 resolutions complete, dual platform, v2 research |
| `12-testing-and-benchmarks.md` | SQLite priority, macOS removed |
| `13-risk-register.md` | SQLite/AOT risks, CLA/trademark status |
| `14-hidden-requirements.md` | H-36 raised, platform/language updates |
| `15-decision-log.md` | This file |
| `16-brand-naming.md` | Runesvaner selected, Aperture not adopted |
| `17-docs-and-community.md` | Documentation site/language/channel confirmation |
| `18-localization-scope.md` | English full, Chinese/German core condensed, glossary |
| `19-open-implementation-decisions.md` | D-23–D-25 resolution record |
| `docs/README.md` | Status and reading path |

---

## 4. Items still requiring external completion

These are not product decisions, but they affect release:

| Item | Owner | Timing |
| --- | --- | --- |
| `Runesvaner` trademark search (USPTO/EUIPO/CNIPA, classes 9/42) | Initiator | **Deferred** (revisit when needed later) |
| ICLA/CCLA lawyer review | Initiator | Before the first external PR |
| NuGet root package reservation (`Runesvaner` 0.0.1); requires the user to register a NuGet account; the 13 planned package IDs have been verified as 404 | Initiator | M0 |
| GitHub repository `runesvaner` verified as 404; to be created (repository name confirmed by the user) | Initiator | At project start |
| Domain WHOIS confirmation (no candidates have DNS resolution) | Initiator | Do not buy a domain for now (D-23); evaluate after 1.0 |
| Legality review of price table sources | Initiator + lawyer | Before M2 |
| Windows/Linux AOT toolchain validation | Engineering | M0/M1 |
| SQLite AOT validation and ADR | Engineering | M1 |

---

## 5. Next steps after resolution

Until the initiator explicitly says "start work", only documentation is produced:

1. Review this resolution and the modified documents.
2. Complete the external items in Section 4 (may run in parallel with document review).
3. After "start work" is said, create `Runesvaner.sln`, project skeletons, governance files, and CI.

---

## 6. Change log

| Date | Version | Change |
| --- | --- | --- |
| 2026-11-25 | 0.1 | Initial D-01–D-22 pending |
| 2026-11-25 | 1.0 | Initiator completed all resolutions; project name changed to Runesvaner; D-07 changed to SQLite default; D-11 changed to 36 months; D-15 removed macOS; D-16 added German |
| 2026-11-26 | 1.1 | Completed external availability verification of Runesvaner; based on `dotnet/efcore#36068`, refined D-07 into library SQLite / CLI AOT JSONL; added `18-localization-scope.md` |
| 2026-11-26 | 1.2 | Confirmed GitHub repository name `runesvaner`; added `19-open-implementation-decisions.md` (D-23–D-25) |
| 2026-11-26 | 1.3 | D-23–D-25 all resolved; trademark search deferred; Git email confirmed bound |