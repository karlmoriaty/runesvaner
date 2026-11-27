# Community Playbook

## 1. Current state

- Repository: `https://github.com/karlmoriaty/runesvaner`
- Issues: `https://github.com/karlmoriaty/runesvaner/issues`
- Discussions: `https://github.com/karlmoriaty/runesvaner/discussions`
- CLA: drafted (`CLA.md`) but **not effective**. Do not accept external pull requests until the
  CLA has been reviewed and the workflow is enabled.
- Governance: individual maintainer, Apache-2.0, inactivity succession clause in D-11.

## 2. Before accepting external PRs

1. Have the CLA reviewed by a lawyer and mark `CLA.md` effective.
2. Add a CLA check to the PR workflow (or state clearly in `CONTRIBUTING.md` how to sign).
3. Confirm the PR template asks for tests, docs, and a changelog entry.
4. Publish a "Contributing" discussion explaining the CLA and the review SLA.

Until then, invite people to open **issues and discussions**, not PRs.

## 3. Good first issues (candidates)

Create these from the issue template and label them `good first issue`:

1. Add a `costs --budget <usd>` option that prints over-budget provider/model rows and exits 1.
2. Add keyboard navigation to the Studio request table (Up/Down/Enter/Esc).
3. Persist budget alerts to a local JSONL file and show them in Studio.
4. Add an index or streaming reader for JSONL query stores.
5. Add a verified OpenAI price entry when the official pricing page is retrievable again.
6. Add Anthropic streaming contract tests with a local SSE mock.
7. Add an Azure OpenAI streaming usage test with a local mock.
8. Add a `--json` output mode to `report` and `costs`.
9. Add a docs page for troubleshooting `StoragePath` and permissions.
10. Add a compatibility test for `net11.0` when the SDK is available.

Each issue should include: context, expected behaviour, files to touch, acceptance checks, and a
"good first issue" note.

## 4. Labels

| Label | Purpose |
| --- | --- |
| `good first issue` | Scoped, documented, low coupling |
| `help wanted` | Maintainer wants an external contributor |
| `area:docs` | Documentation site or Markdown |
| `area:cli` | Command-line interface |
| `area:studio` | React UI and local API |
| `area:providers` | Provider protocol and pricing |
| `area:aot` | Native AOT compatibility |
| `needs-triage` | Not yet classified |

## 5. Review SLA

- First response to an issue: within 7 days, best effort.
- PR review: within 7 days once the CLA is effective.
- A PR without tests or docs may be closed with a request to reopen when complete.
- Do not merge breaking public API changes during a preview without a decision-log entry.

## 6. Launch sequence

### Week 1 — soft launch

- Publish the NuGet preview (see `26-preview-release.md`).
- Post a short announcement in GitHub Discussions with the package list and two examples.
- Add the GitHub Pages link to the repository description and README.

### Week 2 — developer channels

- Submit a Show HN: `https://news.ycombinator.com/submit`
  Title: `Show HN: Runesvaner – in-process latency and cost profiler for .NET AI apps`
- Post to `https://www.reddit.com/r/dotnet/` with a technical summary, not only a link.
- Publish the article on `https://dev.to/new` with tags `dotnet`, `ai`, `observability`,
  `performance`.

### Week 3 — Chinese channels

- Publish a Chinese version on 掘金 (`https://juejin.cn/`) and 知乎 (`https://zhuanlan.zhihu.com/write`).
- Link back to the English repository and the docs site.

### Week 4 — follow-up

- Respond to every issue within the SLA.
- Convert repeated questions into docs pages.
- Pick the first `good first issue` candidates from real questions.

## 7. Targets

| Metric | Target |
| --- | --- |
| External contributors | 3 |
| Merged external PRs | 10 |
| Issues triaged within 7 days | 100% |
| Discussions answered within 7 days | 100% |
| Docs pages with a runnable example | 5 |

## 8. What not to do

- Do not promise commercial support.
- Do not claim provider price accuracy beyond the verified snapshot.
- Do not accept PRs before the CLA is effective.
- Do not publish a package version that was already pushed.