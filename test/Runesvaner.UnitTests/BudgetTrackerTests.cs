using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class BudgetTrackerTests
{
    [Fact]
    public void RequestLimit_RaisesOncePerRequest()
    {
        var alerts = new List<RunesvanerBudgetAlert>();
        var tracker = new RunesvanerBudgetTracker(new RunesvanerBudgetOptions
        {
            RequestLimitUsd = 0.01m
        });
        tracker.Alert += alerts.Add;

        var record = CreateRecord(0.02m);
        tracker.Observe(record);
        tracker.Observe(record);

        var alert = Assert.Single(alerts);
        Assert.Equal("request", alert.Scope);
        Assert.Equal(record.RequestId, alert.Key);
        Assert.Equal(0.02m, alert.Spent);
    }

    [Fact]
    public void SessionLimit_AccumulatesAcrossRequests()
    {
        var alerts = new List<RunesvanerBudgetAlert>();
        var tracker = new RunesvanerBudgetTracker(new RunesvanerBudgetOptions
        {
            SessionLimitUsd = 0.01m
        });
        tracker.Alert += alerts.Add;

        tracker.Observe(CreateRecord(0.006m, sessionId: "session-1"));
        Assert.Empty(alerts);

        tracker.Observe(CreateRecord(0.006m, sessionId: "session-1"));

        var alert = Assert.Single(alerts);
        Assert.Equal("session", alert.Scope);
        Assert.Equal("session-1", alert.Key);
        Assert.Equal(0.012m, alert.Spent);
    }

    [Fact]
    public void FeatureLimit_UsesFeatureTag()
    {
        var alerts = new List<RunesvanerBudgetAlert>();
        var tracker = new RunesvanerBudgetTracker(new RunesvanerBudgetOptions
        {
            FeatureLimitUsd = 0.01m
        });
        tracker.Alert += alerts.Add;

        tracker.Observe(CreateRecord(
            0.006m,
            tags: new Dictionary<string, string> { ["feature"] = "rag" }));
        tracker.Observe(CreateRecord(
            0.006m,
            tags: new Dictionary<string, string> { ["feature"] = "rag" }));

        var alert = Assert.Single(alerts);
        Assert.Equal("feature", alert.Scope);
        Assert.Equal("rag", alert.Key);
    }

    [Fact]
    public void AlertCallbackException_DoesNotBreakObservation()
    {
        var tracker = new RunesvanerBudgetTracker(new RunesvanerBudgetOptions
        {
            RequestLimitUsd = 0.01m,
            AlertCallback = _ => throw new InvalidOperationException("alert failure")
        });

        tracker.Observe(CreateRecord(0.02m));
    }

    [Fact]
    public void UnknownCost_IsIgnored()
    {
        var alerts = new List<RunesvanerBudgetAlert>();
        var tracker = new RunesvanerBudgetTracker(new RunesvanerBudgetOptions
        {
            RequestLimitUsd = 0.01m
        });
        tracker.Alert += alerts.Add;

        tracker.Observe(CreateRecord(null, status: CostStatus.Unknown));

        Assert.Empty(alerts);
    }

    [Fact]
    public void Recorder_FeedsBudgetTracker()
    {
        var alerts = new List<RunesvanerBudgetAlert>();
        var tracker = new RunesvanerBudgetTracker(new RunesvanerBudgetOptions
        {
            RequestLimitUsd = 0.01m
        });
        tracker.Alert += alerts.Add;

        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(
            store,
            new RunesvanerOptions { Provider = "anthropic" },
            budget: tracker);

        using (var request = recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   "anthropic",
                   "claude-opus-5.5",
                   null)))
        {
            request.RecordUsage(new RunesvanerUsage(1_000, 500, null, null, UsageSource.Provider));
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1_000, null));
        }

        var alert = Assert.Single(alerts);
        Assert.Equal("request", alert.Scope);
    }

    private static RunesvanerRequestRecord CreateRecord(
        decimal? cost,
        string? sessionId = null,
        Dictionary<string, string>? tags = null,
        CostStatus status = CostStatus.Known) => new(
        "runesvaner.request.v1",
        Guid.NewGuid().ToString("N"),
        "Chat",
        "test",
        "model-1",
        "model-1",
        DateTimeOffset.UtcNow,
        1_000,
        null,
        "Ok",
        null,
        [],
        tags,
        cost is null
            ? null
            : new CostBreakdown("USD", null, null, null, null, cost, status, "test-1"),
        null,
        sessionId);
}