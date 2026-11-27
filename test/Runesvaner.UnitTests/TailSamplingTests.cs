using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class TailSamplingTests
{
    [Fact]
    public async Task RateZero_DropsSuccessfulFastRequest()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, Options(sampleRate: 0));

        using (var request = recorder.StartRequest(Info()))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1_000, null));
        }

        Assert.Empty(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Error_IsAlwaysKept()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, Options(sampleRate: 0));

        using (var request = recorder.StartRequest(Info()))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Error, 1_000, null));
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal("Error", record.Status);
        Assert.Equal(1, record.SamplingWeight);
    }

    [Fact]
    public async Task SlowRequest_IsAlwaysKept()
    {
        var store = new InMemoryRunesvanerStore();
        var options = Options(sampleRate: 0);
        options.TailSampling.SlowThresholdUs = 500;

        var recorder = new RunesvanerRecorder(store, options);

        using (var request = recorder.StartRequest(Info()))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1_000, null));
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(1, record.SamplingWeight);
    }

    [Fact]
    public async Task RateOne_KeepsRequestWithUnitWeight()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, Options(sampleRate: 1));

        using (var request = recorder.StartRequest(Info()))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1_000, null));
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(1, record.SamplingWeight);
    }

    [Fact]
    public void Aggregates_UseSamplingWeight()
    {
        var summaries = RunesvanerCostAggregator.Aggregate(
        [
            CreateRecord(0.010m, 2),
            CreateRecord(0.010m, 0.5)
        ]);

        var summary = Assert.Single(summaries);
        Assert.Equal(0.025m, summary.Total);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void InvalidTailSampleRate_Throws(double rate)
    {
        var options = Options(rate);
        options.TailSampling.SampleRate = rate;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RunesvanerRecorder(new InMemoryRunesvanerStore(), options));
    }

    private static RunesvanerOptions Options(double sampleRate) => new()
    {
        TailSampling = new RunesvanerTailSamplingOptions
        {
            Enabled = true,
            SampleRate = sampleRate
        }
    };

    private static RunesvanerRequestInfo Info() => new(
        RunesvanerOperation.Chat,
        "test",
        "model-1",
        null);

    private static RunesvanerRequestRecord CreateRecord(decimal cost, double weight) => new(
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
        null,
        new CostBreakdown("USD", null, null, null, null, cost, CostStatus.Known, "test-1"),
        SamplingWeight: weight);
}