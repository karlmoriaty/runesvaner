using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class SessionAggregatorTests
{
    [Fact]
    public void Aggregate_GroupsBySessionAndComputesCostAndRange()
    {
        var start = new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero);
        var records = new[]
        {
            Record("s1", "gpt-5", start, 1_000, 0.001m),
            Record("s1", "gpt-5", start.AddSeconds(2), 2_000, 0.002m),
            Record("s2", "echo-1", start.AddSeconds(5), 3_000, null)
        };

        var sessions = RunesvanerSessionAggregator.Aggregate(records);

        Assert.Equal(2, sessions.Count);
        var first = sessions.Single(session => session.SessionId == "s1");
        Assert.Equal(2, first.Requests);
        Assert.Equal(2, first.Priced);
        Assert.Equal(0.003m, first.TotalCost);
        Assert.Equal(start, first.StartedAtUtc);
        Assert.Equal(start.AddSeconds(2).AddTicks(2_000 * 10), first.EndedAtUtc);
        Assert.Equal(new[] { "gpt-5" }, first.Models);
    }

    private static RunesvanerRequestRecord Record(
        string sessionId,
        string model,
        DateTimeOffset startedAt,
        long durationUs,
        decimal? total)
    {
        return new RunesvanerRequestRecord(
            "runesvaner.request.v1",
            Guid.NewGuid().ToString("N"),
            "Chat",
            "openai",
            model,
            null,
            startedAt,
            durationUs,
            null,
            "Ok",
            null,
            Array.Empty<RunesvanerSegment>(),
            null,
            total is null
                ? new CostBreakdown("USD", null, null, null, null, null, CostStatus.Unknown, "test-1")
                : new CostBreakdown("USD", total, null, null, null, total, CostStatus.Known, "test-1"),
            SessionId: sessionId);
    }
}