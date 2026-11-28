# Contributing to Runesvaner

Runesvaner is a personal open-source project. It is not affiliated with any organization.

## Current stage

The repository is at **M3 (beta)**. The public API is not frozen. Large changes should start with an
Issue or Discussion before a pull request.

## Co-maintainers wanted

The project is actively looking for co-maintainers and reviewers. See
[`docs/28-maintainers-wanted.md`](docs/28-maintainers-wanted.md) for roles, time expectations, and
the application process. You do not need to be a .NET expert to help with docs, examples, or
Studio UI work.

## Build and test

Requirements:

- .NET SDK 10.0.404 (pinned by `global.json`)
- Windows 10/11 or Linux

Commands:

    dotnet restore
    dotnet build -c Release
    dotnet test -c Release

The docs site builds with Node 20+:

    npm ci --prefix docs-site
    npm run build --prefix docs-site

## CLA (effective)

The project uses ICLA + CCLA (D-04). The CLA is **effective since 2026-11-28** and is available in
[`CLA.md`](CLA.md).

External contributors must sign by commenting on their pull request:

> I have read the Runesvaner CLA (CLA.md) and I agree to its terms.

A pull request is not merged until the signature is recorded. Corporate contributors sign the CCLA
once and list authorized employees. Do not open a PR if you cannot sign the CLA.

## Pull requests

- Keep changes focused.
- Add or update tests for behavior changes.
- Update documentation in the same PR.
- Run `dotnet build -c Release` and `dotnet test -c Release` before opening the PR.
- Use the PR template and include the CLA signature statement.
- Do not include secrets, tokens, or private data in code, logs, fixtures, or screenshots.

## Good first issues

Look for issues labeled `good first issue` and `help wanted`. Each should include context, files to
touch, acceptance checks, and a mentor. If an issue is unclear, ask in the issue before starting.

## Commit style

Use short, imperative commit messages. Examples:

- `feat: capture chat request duration`
- `fix: preserve cancellation in streaming wrapper`
- `docs: explain SQLite AOT fallback`

## Code style

- Follow `.editorconfig`.
- Enable nullable reference types.
- Avoid reflection and dynamic code in core paths.
- Keep the core package free of OpenTelemetry and UI dependencies.
- Do not add comments unless they explain non-obvious behavior.
- Public packages must keep XML documentation complete and warning-free.

## Security

See `SECURITY.md`. Do not report vulnerabilities in public issues.

## Recognition

Contributors are acknowledged in release notes. Sustained contributors may be invited as reviewers
or maintainers; see the maintainer recruitment page.