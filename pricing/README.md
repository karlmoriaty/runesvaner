# Pricing Data

Runesvaner keeps price data versioned and explicit. It never guesses a price for an unknown model.

## Files

| File | Purpose |
| --- | --- |
| `builtin-prices.json` | Verified snapshot embedded into `Runesvaner.dll` and used by `RunesvanerOptions` by default. |
| `builtin-prices.sha256` | Checksum of `builtin-prices.json`, asserted by `BuiltInPriceTableTests`. |
| `example-prices.json` | Illustrative sample data for tests and the RAG sample. **Not production pricing.** |
| `README.md` | Schema, validation rules, and snapshot policy |

## Built-in snapshot policy

A price enters `builtin-prices.json` only when all of the following are recorded:

1. A retrieval date (`retrievedAt`).
2. A source URL that can be re-checked (`source`).
3. The provider, model identifier, and unit (USD per one million tokens) exactly as published.
4. A checksum for the whole file (`builtin-prices.sha256`).

Current snapshot (`version` `2026-11-27`, `currency` `USD`, 6 entries):

| Provider | Model | Input | Cached input | Output | Source |
| --- | --- | --- | --- | --- | --- |
| `anthropic` | `claude-fable-5.2` | 8.00 | 0.20 | 40.00 | `anthropic.com/pricing` |
| `anthropic` | `claude-opus-5.5` | 4.00 | 0.20 | 20.00 | `anthropic.com/pricing` |
| `anthropic` | `claude-sonnet-5.5` | 2.00 | 0.10 | 10.00 | `anthropic.com/pricing` |
| `azure-openai` | `gpt-5` | 1.25 | 0.125 | 10.00 | Azure Retail Prices API |
| `azure-openai` | `gpt-5-mini` | 0.25 | 0.025 | 2.00 | Azure Retail Prices API |
| `azure-openai` | `gpt-5-nano` | 0.05 | 0.005 | 0.40 | Azure Retail Prices API |

The Azure values come from the Global deployment meters (`GPT 5 Inpt Glbl`, `GPT 5 Cchd Glbl`, `GPT 5 Outpt Glbl`) under `productName` `Azure OpenAI GPT5`.

Deliberately absent:

- OpenAI first-party (`openai/*`) prices. The public pricing pages returned HTTP 403 during retrieval, so no values are recorded rather than guessed. Those models resolve to `CostStatus.Unknown`.
- Models outside the table. Unknown is explicit and always surfaced, never zero.

### Updating the snapshot

1. Re-retrieve each price from the provider's own pricing page or API. Do not copy from aggregators.
2. Update `retrievedAt` and `version` (use the retrieval date).
3. Update `builtin-prices.sha256` with the new hash:

       sha256sum pricing/builtin-prices.json > pricing/builtin-prices.sha256

   On Windows PowerShell:

       (Get-FileHash pricing/builtin-prices.json -Algorithm SHA256).Hash.ToLower() + " *builtin-prices.json" | Set-Content pricing/builtin-prices.sha256

4. Run `dotnet test test/Runesvaner.UnitTests`. The checksum test fails until the digest matches the embedded resource.

The file is embedded at build time under the logical name `Runesvaner.Pricing.builtin-prices.json`. It must stay valid JSON: a broken snapshot fails the build-time embed and every test that constructs `RunesvanerOptions`.

## JSON schema

    {
      "version": "example-2026-11-26",
      "currency": "USD",
      "source": "https://example.invalid/runesvaner/example-prices",
      "retrievedAt": "2026-11-26T00:00:00Z",
      "entries": [
        {
          "provider": "sample-rag",
          "model": "echo-1",
          "aliases": ["echo"],
          "inputPerMillion": 0.10,
          "cachedInputPerMillion": 0.01,
          "outputPerMillion": 0.20,
          "reasoningPerMillion": null,
          "reasoningIncludedInOutput": true,
          "batchDiscount": 0,
          "tiers": []
        }
      ]
    }

Rules:

- `version`, `currency`, and at least one entry are required.
- Prices are decimal values per one million tokens; negative values are rejected.
- `batchDiscount` must be between `0` and `1`.
- `aliases` map the same entry under additional model names.
- `tiers` must have strictly increasing `upToInputTokens`.
- Duplicate provider/model or alias keys are rejected.
- `reasoningIncludedInOutput: true` means reasoning tokens are already inside output tokens and are not charged twice.
- `source` and `retrievedAt` are retained for audit.

## CLI

Inspect the embedded snapshot:

    dotnet run --project src/Runesvaner.Cli -c Release -- pricing validate --builtin

Validate your own table:

    dotnet run --project src/Runesvaner.Cli -c Release -- pricing validate --path pricing/example-prices.json

Aggregate recorded costs:

    dotnet run --project src/Runesvaner.Cli -c Release -- costs

## Policy

- Unknown model: `CostStatus.Unknown`, total is null; never zero.
- Partial usage: `CostStatus.Partial`; known components are summed and marked partial.
- Historical records retain the price table version used at capture time.
- The embedded snapshot is the default for `RunesvanerOptions.PricingTable`; set `PricingTable` explicitly to override it.
- The sample table is intentionally generic and must not be presented as current provider pricing.

## Verification sources

- Anthropic: <https://www.anthropic.com/pricing>
- Azure Retail Prices API: <https://prices.azure.com/api/retail/prices?$filter=productName%20eq%20'Azure%20OpenAI%20GPT5'>