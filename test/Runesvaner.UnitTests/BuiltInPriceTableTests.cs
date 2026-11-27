using System.Security.Cryptography;
using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class BuiltInPriceTableTests
{
    private const string ResourceName = "Runesvaner.Pricing.builtin-prices.json";

    [Fact]
    public void BuiltIn_LoadsVerifiedSnapshot()
    {
        var table = RunesvanerPriceTableLoader.LoadBuiltIn();

        Assert.Equal("2026-11-27", table.Version);
        Assert.Equal("USD", table.Currency);
        Assert.Equal(6, table.Entries.Count);
        Assert.Equal(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero), table.RetrievedAt);
        Assert.Contains("anthropic.com/pricing", table.Source, StringComparison.Ordinal);
        Assert.Contains("prices.azure.com", table.Source, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltIn_ContainsOnlyRetrievedProviderPrices()
    {
        var table = RunesvanerPriceTableLoader.LoadBuiltIn();

        AssertPrice(table, "anthropic", "claude-fable-5.2", 8.0m, 0.20m, 40.0m);
        AssertPrice(table, "anthropic", "claude-opus-5.5", 4.0m, 0.20m, 20.0m);
        AssertPrice(table, "anthropic", "claude-sonnet-5.5", 2.0m, 0.10m, 10.0m);
        AssertPrice(table, "azure-openai", "gpt-5", 1.25m, 0.125m, 10.0m);
        AssertPrice(table, "azure-openai", "gpt-5-mini", 0.25m, 0.025m, 2.0m);
        AssertPrice(table, "azure-openai", "gpt-5-nano", 0.05m, 0.005m, 0.4m);
    }

    [Fact]
    public void BuiltIn_DoesNotGuessUnverifiedModels()
    {
        var table = RunesvanerPriceTableLoader.LoadBuiltIn();

        Assert.False(table.TryGet("openai", "gpt-5", out _));
        Assert.False(table.TryGet("anthropic", "claude-3-haiku", out _));
        Assert.False(table.TryGet("sample-rag", "echo-1", out _));
    }

    [Fact]
    public void BuiltIn_PricesKnownUsageAndKeepsUnknownExplicit()
    {
        var table = RunesvanerPriceTableLoader.LoadBuiltIn();
        Assert.True(table.TryGet("anthropic", "claude-opus-5.5", out var entry));

        var known = RunesvanerCostEngine.Calculate(
            new RunesvanerUsage(1_000, 500, null, null, UsageSource.Provider),
            entry,
            table.Version);

        Assert.Equal(CostStatus.Known, known.Status);
        Assert.Equal(0.004m, known.Input);
        Assert.Equal(0.010m, known.Output);
        Assert.Equal(0.014m, known.Total);
        Assert.Equal("anthropic/claude-opus-5.5", known.PriceEntryId);

        var unknown = RunesvanerCostEngine.Calculate(
            new RunesvanerUsage(1_000, 500, null, null, UsageSource.Provider),
            null,
            table.Version);

        Assert.Equal(CostStatus.Unknown, unknown.Status);
        Assert.Null(unknown.Total);
        Assert.Null(unknown.PriceEntryId);
    }

    [Fact]
    public void EmbeddedSnapshot_MatchesRecordedChecksum()
    {
        var assembly = typeof(RunesvanerPriceTableLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        Assert.NotNull(stream);

        var actual = Convert.ToHexStringLower(SHA256.HashData(stream!));

        var checksumPath = FindRepositoryFile(Path.Combine("pricing", "builtin-prices.sha256"));
        var expected = File.ReadAllText(checksumPath).Trim().Split(' ')[0];

        Assert.Equal(expected, actual, ignoreCase: true);
    }

    [Fact]
    public void Options_UseBuiltInTableByDefault()
        => Assert.Same(RunesvanerPriceTableLoader.LoadBuiltIn(), new RunesvanerOptions().PricingTable);

    private static void AssertPrice(
        PriceTable table,
        string provider,
        string model,
        decimal input,
        decimal cached,
        decimal output)
    {
        Assert.True(table.TryGet(provider, model, out var entry), $"{provider}/{model} missing from built-in table.");
        Assert.Equal(input, entry.InputPerMillion);
        Assert.Equal(cached, entry.CachedInputPerMillion);
        Assert.Equal(output, entry.OutputPerMillion);
        Assert.True(entry.ReasoningIncludedInOutput);
        Assert.Equal(0m, entry.BatchDiscount);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        for (var depth = 0; directory is not null && depth < 10; depth++)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate {relativePath} above {AppContext.BaseDirectory}.");
    }
}