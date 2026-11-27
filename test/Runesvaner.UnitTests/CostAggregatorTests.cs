using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class CostAggregatorTests
{
    [Fact]
    public void Aggregate_GroupsByProviderAndModel()
    {
        var records = new[]
        {
            Record("openai", "gpt-5", 0.002m, CostStatus.Known),
            Record("openai", "gpt-5", 0.001m, CostStatus.Known),
            Record("openai", "gpt-5", null, CostStatus.Unknown),
            Record("sample-rag", "echo-1", 0.000009m, CostStatus.Known)
        };

        var summary = RunesvanerCostAggregator.Aggregate(records);

        Assert.Equal(2, summary.Count);
        var openai = summary.Single(item => item.Provider == "openai");
        Assert.Equal(3, openai.Requests);
        Assert.Equal(2, openai.Priced);
        Assert.Equal(1, openai.Unknown);
        Assert.Equal(0.003m, openai.Total);
    }

    private static RunesvanerRequestRecord Record(
        string provider,
        string model,
        decimal? total,
        CostStatus status)
    {
        return new RunesvanerRequestRecord(
            "runesvaner.request.v1",
            Guid.NewGuid().ToString("N"),
            "Chat",
            provider,
            model,
            null,
            DateTimeOffset.UtcNow,
            1000,
            null,
            "Ok",
            null,
            Array.Empty<RunesvanerSegment>(),
            null,
            total is null
                ? new CostBreakdown("USD", null, null, null, null, null, status, "test-1")
                : new CostBreakdown("USD", total, null, null, null, total, status, "test-1"));
    }
}