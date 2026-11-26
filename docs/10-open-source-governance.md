# Open Source Governance: License, CLA, Community, and Releases

| Field | Value |
| --- | --- |
| Document status | Draft / pending legal review |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Project founder, maintainers, contributors |
| Related documents | `09-security-privacy.md`, `11-roadmap.md`, `15-decision-log.md` |

> This document is not legal advice. The CLA, license, and trademark terms should be reviewed by qualified counsel before formal adoption.

---

## 1. Project Positioning

- **Personal open-source project**: Not affiliated with any company or organization; copyright belongs to the founder and contributors.
- **Goal**: Allow more developers to participate in maintenance while preserving project direction and quality.
- **Open**: Code, design, roadmap, issues, and RFCs are public.
- **Sustainable**: Reduce bus factor through the CLA, maintainer pipeline, and automation.

---

## 2. License Selection

### 2.1 Candidate comparison

| License | Patent grant | Commercially friendly | Ecosystem compatibility | Complexity | Recommendation |
| --- | --- | --- | --- | --- | --- |
| Apache-2.0 | Explicit | High | Consistent with OTel, CNCF | Medium | **Recommended** |
| MIT | No explicit terms | Highest | Consistent with the .NET ecosystem | Low | Optional |
| BSD-3-Clause | No explicit terms | High | Compatible | Low | Not recommended |
| MPL-2.0 | Yes | Medium | File-level copyleft | Medium | Not recommended |

**Decided (D-03): Apache-2.0.**

### 2.2 Rationale

1. **Patent grant**: AI diagnostics involve data processing and visualization, so patent risk cannot be ignored; Apache-2.0 provides explicit protection.
2. **Commercially friendly**: Enterprises can freely use, modify, and redistribute, which favors adoption.
3. **Ecosystem consistency**: OpenTelemetry uses Apache-2.0; interoperability with OTLP is more natural.
4. **Contributor-friendly**: Section 5 explicitly grants contribution licensing, which works clearly with the CLA.

### 2.3 Competitor license observations

| Project | License | Takeaway |
| --- | --- | --- |
| Langfuse | MIT Expat + EE directory | Core open source + enterprise edition |
| Arize Phoenix | Elastic License 2.0 | Restricts hosted service, non-OSI |
| OpenLLMetry | Apache-2.0 | Purely open source |
| Semantic Kernel | MIT | Microsoft ecosystem convention |

Runesvaner positions itself as **purely open source, no EE directory, no hosting restrictions**, differentiating itself with Apache-2.0.

---

## 3. CLA Design

### 3.1 Why a CLA is needed

- Allows the project to **relicense** or adjust governance when necessary.
- Explicitly grants copyright and patent licenses from contributors, reducing legal risk.
- Unifies contribution terms, avoiding individual negotiation per PR.

### 3.2 CLA types

| Type | Description | Applicable |
| --- | --- | --- |
| Individual CLA (ICLA) | Signed by an individual | Individual contributors |
| Corporate CLA (CCLA) | Signed by a company, covers employees | Corporate contributors |
| DCO | Only asserts the right to submit; grants no relicensing | Lightweight projects |

Decided (D-04): **ICLA + CCLA**; CLA Assistant (or an equivalent Action) is the implementation recommendation. DCO is not used as the primary mechanism to avoid unclear copyright ownership.

### 3.3 CLA core terms (summary)

1. **Copyright license**: The contributor grants the project owner a perpetual, worldwide, non-exclusive, irrevocable, royalty-free copyright license to reproduce, modify, distribute, and sublicense.
2. **Patent license**: Grants a patent license for making, using, selling, and importing; if the contributor initiates patent litigation, the license terminates.
3. **Representation**: The contributor represents that they have the right to submit and that the contribution is original or authorized.
4. **No warranty**: Contributions are provided "as is".
5. **Retained rights**: The contributor retains copyright; the project does not claim the contributor's moral rights.
6. **Attribution**: The contributor agrees that the project may acknowledge them in the NOTICE / contributor list (optional).
7. **Governing law and jurisdiction**: To be confirmed with counsel.
8. **Assignability**: The project owner may assign rights to a successor entity (for governance continuity).

### 3.4 CLA automation

- Use [CLA Assistant](https://github.com/cla-assistant/cla-assistant) or a GitHub Action.
- Automatically comment on the first commit of a PR, requesting a signature.
- Store signature records in the repository's `cla-signatures` branch or an external service.
- A bot checks for unsigned PRs and blocks merging.
- Corporate contributors are covered by a CCLA across multiple employees.

### 3.5 CLA anti-patterns

- Overly broad terms that make contributors unwilling to sign.
- Not explaining the purpose of relicensing.
- Manual signing with no record.
- Changing the CLA without notifying existing contributors.
- Using the CLA to deprive contributors of patent defense rights.

---

## 4. Governance Model

### 4.1 Roles

| Role | Permissions | How appointed |
| --- | --- | --- |
| Owner (BDFL) | Final decisions, releases, trademark, CLA | Founder |
| Maintainer | Merge, release, roadmap | Invited by Owner |
| Reviewer | Code review, labels | Invited by Maintainer |
| Contributor | Submit PRs/Issues | Anyone |
| Security Responder | Vulnerability response | Designated by Owner |

### 4.2 Decision process

| Type | Process |
| --- | --- |
| Routine fix | 1 Reviewer approval + CI passing |
| New feature | Issue discussion + 1 Maintainer approval |
| Public API change | RFC + API Review + 2 Maintainers |
| Data model change | RFC + compatibility assessment |
| License/governance change | RFC + Owner approval + contributor notification |
| Security issue | Handled privately, disclosed after the fix |

### 4.3 Lazy Consensus

- Proposals are published in an Issue/Discussion for 7 days.
- If there is no substantive objection, it passes.
- If there is an objection, it enters RFC discussion; the Owner makes the final ruling.
- Blocking objections must include reasons and alternatives.

### 4.4 RFC process

1. Copy `docs/rfcs/0000-template.md`.
2. Submit a PR with status `Draft`.
3. Discussion period ≥ 7 days.
4. Change status to `Accepted` / `Rejected` / `Deferred`.
5. After implementation, link it in the documentation.

RFC scope:

- Breaking changes to the public API.
- Major versions of the data schema.
- New packages/dependencies.
- Governance and license.
- Performance threshold adjustments.

---

## 5. Trademark and Brand

- The project name and logo are held by the Owner (pending D-02/D-11).
- Permitted: mentioning the project name in factual descriptions, and stating "based on Runesvaner" in a fork.
- Not permitted: using the project name/logo to imply official endorsement, as a commercial product name, or registering a similar trademark.
- Forks must use a different name and logo to avoid confusion.
- The trademark policy is independent of the code license.

---

## 6. Code of Conduct

Adopt Contributor Covenant 2.1 (or a newer version):

- Harassment, discrimination, personal attacks, and disclosure of privacy are prohibited.
- Maintainers have the right to remove inappropriate content and to temporarily or permanently ban.
- Reporting channel: project email (TBD) + private handling.
- Conflict handling: avoidance, mediation, and, if necessary, a decision by the Owner.

---

## 7. Security Policy

- `SECURITY.md` describes supported versions, reporting channels, and response time limits.
- Private fixes followed by long-term non-disclosure are not supported.
- Security advisories are published via GitHub Security Advisories.
- Patches and acknowledgments are released after critical vulnerabilities are fixed.

---

## 8. Release Process

### 8.1 Versioning strategy

- SemVer 2.0: `MAJOR.MINOR.PATCH`.
- Preview: `1.0.0-preview.1`.
- Experimental packages: `0.x`, with no compatibility promise.
- Breaking changes to the public API → major version.
- The data schema major version is independent of the package version but must be noted in the release notes.

### 8.2 Branch model

| Branch | Purpose |
| --- | --- |
| `main` | Main development line, always buildable |
| `release/1.x` | Maintain older versions |
| `feature/*` | Feature branches, deleted after the PR |
| `hotfix/*` | Emergency fixes |

- Trunk-based, short-lived branches.
- Merge requirements: CI all green, 1–2 Reviewers, no unresolved discussions.
- No merge commits (squash or rebase, TBD).

### 8.3 Release checklist

1. Version number and changelog.
2. All tests pass (unit, integration, AOT, benchmarks).
3. Public API compatibility check.
4. Dependency vulnerability scanning.
5. SBOM generation.
6. NuGet package signing.
7. GitHub Release + release notes.
8. Documentation site update.
9. Social channel announcement.

### 8.4 NuGet publishing

- Use Trusted Publishing (OIDC) to avoid long-lived API keys.
- Package metadata: authors, repository URL, license, icon, README, SourceLink.
- Symbol packages (`.snupkg`).
- Package validation: `dotnet package validate`.
- Preview packages are marked `preview` and are not mixed with stable packages.

---

## 9. Issue and PR Conventions

### 9.1 Issue templates

- Bug: version, OS, TFM, AOT, reproduction steps, expected/actual, logs (redacted).
- Feature: problem, scenario, proposed solution, alternatives.
- Performance: benchmark, dataset, environment.
- Security: not public, follow SECURITY.md.

### 9.2 PR template

- What does the pull request do
- Current behavior
- Expected behavior
- How implemented
- Checklist (tests, docs, AOT, performance)
- Breaking changes
- Fixed issues

### 9.3 Labels

`bug`, `enhancement`, `performance`, `aot`, `privacy`, `good first issue`, `help wanted`, `needs design`, `blocked`, `breaking change`.

### 9.4 Good First Issue

- Clear scope, clear file location, and acceptance criteria.
- Does not involve the public API, AOT, or privacy.
- Provides a mentor (maintainer).

---

## 10. Contributor Growth

| Stage | Behavior | Recognition |
| --- | --- | --- |
| First contribution | Fixes docs/tests | Acknowledgment |
| Regular contributor | Multiple PRs | Invited as Reviewer |
| Reviewer | Reviews, labels | Listed in the maintainer list |
| Maintainer | Merge, release, roadmap | Repository permissions |

Principle: Contributors earn trust through behavior, not by company/seniority.

---

## 11. Funding and Sustainability

| Method | Adopted | Description |
| --- | --- | --- |
| GitHub Sponsors | Accepted (unbundled) | Does not affect features |
| Open Collective | Optional | Transparent accounting |
| Corporate sponsorship | Accepted (unbundled) | No roadmap control attached |
| Commercial support | Not for now | Limited bandwidth for a personal project |
| Hosted service | Explicitly not doing | Conflicts with the local-first positioning (D-10) |

Principles:

- Do not accept sponsorship that comes with roadmap control.
- Do not accept revenue that conflicts with privacy commitments.
- Funding use is public (infrastructure, design, security audits).

---

## 12. Succession and Bus Factor

- At least 2 Maintainers have release permissions.
- The CLA allows rights to be assigned to a successor entity.
- Key decisions are documented to avoid word-of-mouth transfer.
- If the Owner is inactive for 36 consecutive months (D-11):
  - Maintainers may nominate a new Owner via RFC.
  - The trademark and domain must have an agreed transfer mechanism in advance.

---

## 13. Legal Review Checklist

Before the official open-source release, counsel should review:

1. License text and NOTICE.
2. ICLA / CCLA terms.
3. Trademark registration and usage policy.
4. Privacy policy (if there is a website/cloud service in the future).
5. Price table data sources and usage licenses.
6. Compatibility of third-party dependency licenses.
7. Export control and sanctions terms (if applicable).
8. Contributor patent terms.

---

## 14. Decision Status

D-01–D-25 have all been decided; see `15-decision-log.md`. Directly related to this document:

- D-02: Project name Runesvaner; trademark search pending.
- D-03: Apache-2.0.
- D-04: ICLA + CCLA + automation.
- D-10: No commercialization for now; unbundled sponsorship accepted; no hosting.
- D-11: Held personally; 36-month succession clause.
- D-17: LTS + latest STS.