# Co-maintainers and developers wanted

Runesvaner is an Apache-2.0 .NET profiler for AI workloads: latency segments, streaming metrics,
cost ledgers, HTTP diagnostics, a local Studio, and Native AOT constraints. It is currently
maintained by one person, and the project is looking for co-maintainers.

This is an unpaid, personal open-source project. There is no company behind it and no commercial
support promise. What it offers is real ownership of a focused tool, public credit, and a
maintainer path.

## Roles

| Role | Scope | Time |
| --- | --- | --- |
| Maintainer | Merge, release, roadmap, issue triage | 3–5 hours/week |
| Reviewer | Code review on one area (core, providers, Studio, docs) | 1–3 hours/week |
| Docs maintainer | Docs site, examples, tutorials | 1–2 hours/week |
| Studio maintainer | React 19 + RTK Query UI and local API | 2–4 hours/week |
| Provider maintainer | MEAI, OpenAI protocol, Anthropic/Azure contract tests | 2–4 hours/week |
| Security responder | Vulnerability triage and disclosure | On demand |

You can start as a reviewer and become a maintainer later. Documentation, examples, and
reproduction scripts are as valuable as core code.

## What we expect

- Follow the code style and the "no suppressed warnings" rule.
- Keep claims honest: no guessed prices, no unlabeled estimates, no "AOT ready" without CI proof.
- Review within 7 days when you accept a review request.
- Discuss roadmap-changing work in an Issue before implementation.
- Sign the CLA (effective since 2026-11-28) before your first merge.

## How to apply

Open a **Co-maintainer application** issue:

https://github.com/karlmoriaty/runesvaner/issues/new?template=co-maintainer.md

Include:

1. GitHub handle and the area you want to own.
2. Weekly availability and timezone.
3. Relevant experience (links to PRs, projects, or writing).
4. Why Runesvaner, and what you would improve first.
5. Confirmation that you can sign the CLA.

You can also start smaller: pick a `good first issue`, open a PR, and mention that you are
interested in maintaining. The application process is asynchronous; there is no formal interview.

## First 30 days for a new maintainer

1. Read `docs/05-technical-design.md`, `docs/06-observability-model.md`, and
   `docs/25-compatibility-matrix.md`.
2. Land one small PR in your area.
3. Review two PRs with the Owner shadowing.
4. Take ownership of one label (`area:docs`, `area:studio`, `area:providers`, or `area:aot`).
5. Agree on a release checklist and a backup reviewer for your area.

## What this is not

- Not a paid position.
- Not a promise of commit access on day one.
- Not a commitment to accept every contribution; scope and honesty rules still apply.
- Not a replacement for the CLA or the security policy.

## Current priorities

- Provider contract tests (Anthropic, Azure OpenAI) with local mocks.
- Studio keyboard navigation and budget-alert persistence.
- JSONL query performance and retention edge cases.
- Documentation: troubleshooting, migration notes, and more end-to-end examples.
- Native AOT validation for optional packages where feasible.