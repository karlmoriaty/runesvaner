using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class PriceTableLoaderTests
{
    [Fact]
    public void LoadFromJson_MapsPricingAndAliases()
    {
        var table = RunesvanerPriceTableLoader.LoadFromJson(
            """
            {
              "version": "test-1",
              "currency": "USD",
              "source": "https://example.invalid/prices",
              "retrievedAt": "2026-11-26T00:00:00Z",
              "entries": [
                {
                  "provider": "openai",
                  "model": "gpt-x",
                  "aliases": ["gpt-x-2026"],
                  "inputPerMillion": 1.25,
                  "cachedInputPerMillion": 0.125,
                  "outputPerMillion": 10.0,
                  "reasoningIncludedInOutput": true,
                  "batchDiscount": 0.5
                }
              ]
            }
            """);

        Assert.Equal("test-1", table.Version);
        Assert.Equal("USD", table.Currency);
        Assert.Equal("https://example.invalid/prices", table.Source);
        Assert.True(table.TryGet("openai", "gpt-x", out var entry));
        Assert.True(table.TryGet("openai", "gpt-x-2026", out var alias));
        Assert.Same(entry, alias);
        Assert.Equal(1.25m, entry.InputPerMillion);
        Assert.Equal(0.5m, entry.BatchDiscount);
    }

    [Fact]
    public void DuplicateKey_IsRejected()
    {
        var json =
            """
            {
              "version": "test-1",
              "currency": "USD",
              "entries": [
                {
                  "provider": "openai",
                  "model": "gpt-x",
                  "inputPerMillion": 1.0,
                  "cachedInputPerMillion": 0.1,
                  "outputPerMillion": 2.0
                },
                {
                  "provider": "openai",
                  "model": "gpt-x",
                  "inputPerMillion": 1.0,
                  "cachedInputPerMillion": 0.1,
                  "outputPerMillion": 2.0
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(() => RunesvanerPriceTableLoader.LoadFromJson(json));
    }

    [Fact]
    public void InvalidBatchDiscount_IsRejected()
    {
        var json =
            """
            {
              "version": "test-1",
              "currency": "USD",
              "entries": [
                {
                  "provider": "openai",
                  "model": "gpt-x",
                  "inputPerMillion": 1.0,
                  "cachedInputPerMillion": 0.1,
                  "outputPerMillion": 2.0,
                  "batchDiscount": 1.5
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(() => RunesvanerPriceTableLoader.LoadFromJson(json));
    }
}