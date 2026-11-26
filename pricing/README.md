# Pricing Data

Runesvaner keeps price data versioned and explicit. It never guesses a price for an unknown model.

## Files

| File | Purpose |
| --- | --- |
| `example-prices.json` | Illustrative sample data for tests and the RAG sample. **Not production pricing.** |
| `README.md` | Schema and validation rules |

A verified built-in price snapshot will be added only when its source, retrieval time, and checksum are recorded. Until then, users provide their own table.

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

Validate a table:

    dotnet run --project src/Runesvaner.Cli -c Release -- pricing validate --path pricing/example-prices.json

Aggregate recorded costs:

    dotnet run --project src/Runesvaner.Cli -c Release -- costs

## Policy

- Unknown model: `CostStatus.Unknown`, total is null; never zero.
- Partial usage: `CostStatus.Partial`; known components are summed and marked partial.
- Historical records retain the price table version used at capture time.
- The sample table is intentionally generic and must not be presented as current provider pricing.