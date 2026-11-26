using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class CostEngineTests
{
    [Fact]
    public void KnownModel_CalculatesCachedOutputAndIncludedReasoning()
    {
        var usage = new RunesvanerUsage(1000, 200, 400, 100, UsageSource.Provider);
        var entry = Entry();

        var cost = RunesvanerCostEngine.Calculate(usage, entry, "test-1");

        Assert.Equal(CostStatus.Known, cost.Status);
        Assert.Equal(0.00075m, cost.Input);
        Assert.Equal(0.00005m, cost.CachedInput);
        Assert.Equal(0.002m, cost.Output);
        Assert.Equal(0m, cost.Reasoning);
        Assert.Equal(0.0028m, cost.Total);
    }

    [Fact]
    public void UnknownModel_ReturnsUnknownWithoutGuessing()
    {
        var usage = new RunesvanerUsage(1000, 200, null, null, UsageSource.Provider);

        var cost = RunesvanerCostEngine.Calculate(usage, null, "test-1");

        Assert.Equal(CostStatus.Unknown, cost.Status);
        Assert.Null(cost.Total);
        Assert.Null(cost.Input);
        Assert.Null(cost.Output);
    }

    [Fact]
    public void BatchDiscount_AppliesToTotal()
    {
        var usage = new RunesvanerUsage(1000, 200, 400, null, UsageSource.Provider);
        var entry = Entry() with { BatchDiscount = 0.5m };

        var cost = RunesvanerCostEngine.Calculate(usage, entry, "test-1");

        Assert.Equal(0.0014m, cost.Total);
    }

    [Fact]
    public void Recorder_AttachesCostToRecord()
    {
        var table = new PriceTable(
            "test-1",
            "USD",
            new Dictionary<string, PriceEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai/gpt-5"] = Entry()
            });

        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions
        {
            Provider = "openai",
            PricingTable = table
        });

        using var request = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "openai",
            "gpt-5",
            null));

        request.RecordUsage(new RunesvanerUsage(1000, 200, 400, null, UsageSource.Provider));
        request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1000, null));

        var record = Assert.Single(store.Records);
        Assert.NotNull(record.Cost);
        Assert.Equal(CostStatus.Known, record.Cost!.Status);
        Assert.Equal(0.0028m, record.Cost.Total!.Value);
    }

    private static PriceEntry Entry()
        => new(
            "openai",
            "gpt-5",
            InputPerMillion: 1.25m,
            CachedInputPerMillion: 0.125m,
            OutputPerMillion: 10m);

    private sealed class InMemoryStore : IRunesvanerStore
    {
        public List<RunesvanerRequestRecord> Records { get; } = new();

        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return ValueTask.CompletedTask;
        }

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}