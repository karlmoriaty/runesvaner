using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class SamplingTests
{
    [Fact]
    public async Task SamplingRateZero_DropsAllRequests()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(
            store,
            new RunesvanerOptions { SamplingRate = 0 });

        using (var request = recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   "test",
                   "model-1",
                   null)))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));
        }

        var records = await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken);

        Assert.Empty(records);
    }

    [Fact]
    public async Task SamplingRateOne_CapturesRequests()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(
            store,
            new RunesvanerOptions { SamplingRate = 1 });

        using (var request = recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   "test",
                   "model-1",
                   null)))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));
        }

        var records = await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken);

        Assert.Single(records);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SamplingRateOutOfRange_Throws(double rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RunesvanerRecorder(
                new InMemoryRunesvanerStore(),
                new RunesvanerOptions { SamplingRate = rate }));
    }
}