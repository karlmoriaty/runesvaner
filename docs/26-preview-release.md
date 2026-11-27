# NuGet Preview Release Runbook

This runbook is for the repository owner. It does not store or request credentials.

## 1. Prerequisites

- A NuGet.org account owned by you, with the `Runesvaner` package ID available.
- An API key created at `https://www.nuget.org/account/apikeys` with **Push new packages and package versions** scope.
- Two-factor authentication enabled on the account.
- CI on `main` is green and `dotnet pack` has produced ten `.nupkg` files.

## 2. Pre-flight checklist

| Check | Command / evidence |
| --- | --- |
| Tests | `dotnet test Runesvaner.sln -c Release` → 100 passed |
| Pack | `dotnet pack Runesvaner.sln -c Release --no-build -o artifacts/packages` |
| Package count | 10 packages, version `0.3.0-alpha.1` |
| No samples | `Runesvaner.Sample.*.nupkg` must not exist |
| README + XML | every package contains `README.md` and `lib/net10.0/*.xml` |
| Consumer smoke | install `Runesvaner.Extensions.DependencyInjection` from the local folder and run a recording |
| License | Apache-2.0 in every package |

## 3. Publish order

Publish dependencies first. NuGet resolves dependencies at install time, but publishing in this
order makes the first install experience predictable.

1. `Runesvaner.Abstractions`
2. `Runesvaner`
3. `Runesvaner.Storage.Jsonl`
4. `Runesvaner.Storage.Sqlite`
5. `Runesvaner.MicrosoftExtensionsAI`
6. `Runesvaner.Http`
7. `Runesvaner.SemanticKernel`
8. `Runesvaner.VectorData`
9. `Runesvaner.Exporters.OpenTelemetry`
10. `Runesvaner.Extensions.DependencyInjection`

### Commands (Git Bash / PowerShell)

    dotnet pack Runesvaner.sln -c Release -o artifacts/packages

    dotnet nuget push artifacts/packages/Runesvaner.Abstractions.0.3.0-alpha.1.nupkg \
      --api-key <NUGET_API_KEY> \
      --source https://api.nuget.org/v3/index.json

Repeat for each package in the order above. `--skip-duplicate` is safe on retries:

    dotnet nuget push artifacts/packages/<package>.0.3.0-alpha.1.nupkg \
      --api-key <NUGET_API_KEY> \
      --source https://api.nuget.org/v3/index.json \
      --skip-duplicate

Do not put the key in a committed file, shell history, or `nuget.config`. Use an environment
variable or a password manager prompt:

    $env:NUGET_API_KEY = "<paste once>"   # PowerShell
    dotnet nuget push ... --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

## 4. Verify

1. Open each package page on `https://www.nuget.org/packages/<id>/0.3.0-alpha.1`.
2. Check that the README renders and the dependency list matches the repository.
3. Install from nuget.org into a temporary project:

       dotnet new console -o /tmp/runesvaner-smoke
       cd /tmp/runesvaner-smoke
       dotnet add package Runesvaner.Extensions.DependencyInjection --version 0.3.0-alpha.1
       dotnet add package Runesvaner.MicrosoftExtensionsAI --version 0.3.0-alpha.1
       dotnet run

   The consumer must resolve `Runesvaner`, `Runesvaner.Abstractions`, and the storage packages
   without extra steps.
4. Run one recording with `StorageMode.Memory` and confirm a request record is returned.

Indexing can take a few minutes. A 404 immediately after push is normal.

## 5. Version and rollback policy

- Preview versions use `0.3.0-alpha.N`; increment `N` for republishes of the same feature set.
- NuGet does not allow deleting a published version from search; unlist it and publish a new
  patch/preview instead.
- If a package is broken, unlist the affected version, fix, bump to the next preview, and publish
  all dependents that reference it.
- Never republish the same version with different bits.

## 6. Common failures

| Error | Cause | Fix |
| --- | --- | --- |
| 403 Forbidden | key scope or account mismatch | recreate the key with push scope; confirm account |
| 409 Conflict | version already exists | bump the preview number; do not overwrite |
| 400 Bad Request | malformed package / missing license | inspect `dotnet pack` output, validate nuspec |
| Package not found after push | indexing delay | wait and refresh; do not republish |

## 7. Root package decision

D-25 reserved `Runesvaner` as the root package. Publishing `Runesvaner` `0.3.0-alpha.1` also
claims that ID. If a separate `0.0.1` placeholder was already published, keep it and publish the
real version normally; NuGet allows the same ID at a higher version.