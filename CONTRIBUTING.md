# Contributing to Runesvaner

Runesvaner is a personal open-source project. It is not affiliated with any organization.

## Current stage

The repository is at **M1 Walking Skeleton**. The public API is not frozen. Large changes should start with an Issue or Discussion before a pull request.

## Build and test

Requirements:

- .NET SDK 10.0.401 or later 10.0.x
- Windows 10/11 or Linux

Commands:

    dotnet restore
    dotnet build -c Release
    dotnet test -c Release

## Pull requests

- Keep changes focused.
- Add or update tests for behavior changes.
- Update documentation in the same PR.
- Run `dotnet build -c Release` and `dotnet test -c Release` before opening the PR.
- Use the PR template.

## CLA

The project intends to use ICLA + CCLA (D-04). The CLA text is **not yet effective** and is pending legal review. Until it is active, external pull requests are not accepted; please discuss the change in an Issue first. This is a temporary gate, not a permanent policy.

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

## Security

See `SECURITY.md`. Do not report vulnerabilities in public issues.