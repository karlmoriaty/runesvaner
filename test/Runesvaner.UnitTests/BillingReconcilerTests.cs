using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class BillingReconcilerTests
{
    [Fact]
    public void Reconcile_ReportsDifferenceAndTolerance()
    {
        var records = new[]
        {
            CreateRecord("openai", "gpt-5", 0.010m, CostStatus.Known),
            CreateRecord("openai", "gpt-5", 0.004m, CostStatus.Partial),
            CreateRecord("anthropic", "claude-opus-5.5", 0.012m, CostStatus.Known)
        };

        var billing = new[]
        {
            new RunesvanerBillingLine("openai", "gpt-5", 0.014m),
            new RunesvanerBillingLine("anthropic", "claude-opus-5.5", 0.010m)
        };

        var report = RunesvanerBillingReconciler.Reconcile(records, billing);

        Assert.Equal(2, report.Rows.Count);
        Assert.Equal(0.026m, report.RecordedTotal);
        Assert.Equal(0.024m, report.BilledTotal);
        Assert.Equal(0.002m, report.DifferenceTotal);
        Assert.False(report.WithinTolerance(5));

        var openAi = Assert.Single(report.Rows, row => row.Provider == "openai");
        Assert.Equal(0.014m, openAi.Recorded);
        Assert.Equal(0m, openAi.Difference);

        var anthropic = Assert.Single(report.Rows, row => row.Provider == "anthropic");
        Assert.Equal(20d, anthropic.DifferencePercent, 3);
    }

    [Fact]
    public void ParseLines_HandlesHeaderQuotesAndComments()
    {
        var lines = new[]
        {
            "provider,model,amount,currency",
            "\"openai\",\"gpt-5\",1.25,USD",
            "# ignored",
            "anthropic,claude-opus-5.5,0.5"
        };

        var parsed = RunesvanerBillingCsv.ParseLines(lines);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("openai", parsed[0].Provider);
        Assert.Equal("gpt-5", parsed[0].Model);
        Assert.Equal(1.25m, parsed[0].Amount);
        Assert.Equal("USD", parsed[1].Currency);
    }

    private static RunesvanerRequestRecord CreateRecord(
        string provider,
        string model,
        decimal total,
        CostStatus status) => new(
        "runesvaner.request.v1",
        Guid.NewGuid().ToString("N"),
        "Chat",
        provider,
        model,
        model,
        DateTimeOffset.UtcNow,
        1_000,
        null,
        "Ok",
        null,
        [],
        null,
        new CostBreakdown("USD", null, null, null, null, total, status, "test-1"),
        null,
        null);
}