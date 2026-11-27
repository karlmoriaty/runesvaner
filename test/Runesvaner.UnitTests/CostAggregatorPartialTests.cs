using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class CostAggregatorPartialTests
{
    [Fact]
    public void Aggregate_IncludesPartialTotalsWithoutCountingThemAsPriced()
    {
        var summaries = RunesvanerCostAggregator.Aggregate(
        [
            CreateRecord("model-1", 0.010m, CostStatus.Known),
            CreateRecord("model-1", 0.004m, CostStatus.Partial),
            CreateRecord("model-1", null, CostStatus.Unknown)
        ]);

        var summary = Assert.Single(summaries);

        Assert.Equal(3, summary.Requests);
        Assert.Equal(1, summary.Priced);
        Assert.Equal(1, summary.Partial);
        Assert.Equal(1, summary.Unknown);
        Assert.Equal(0.014m, summary.Total);
    }

    private static RunesvanerRequestRecord CreateRecord(
        string model,
        decimal? total,
        CostStatus status) => new(
        "runesvaner.request.v1",
        Guid.NewGuid().ToString("N"),
        "Chat",
        "test",
        model,
        model,
        DateTimeOffset.UtcNow,
        1_000,
        null,
        "Ok",
        null,
        [],
        null,
        new CostBreakdown(
            "USD",
            null,
            null,
            null,
            null,
            total,
            status,
            "test-1"),
        null,
        null);

}