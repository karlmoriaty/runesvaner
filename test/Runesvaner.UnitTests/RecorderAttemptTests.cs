using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class RecorderAttemptTests
{
    [Fact]
    public async Task RecordsAttemptAndDuplicateBillingFlag()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());

        using (var request = recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   "test",
                   "model-1",
                   null)))
        {
            request.RecordUsage(new RunesvanerUsage(100, 20, null, null, UsageSource.Provider));
            request.Complete(new RunesvanerRequestResult(
                RequestStatus.Ok,
                1_000,
                200,
                Attempt: 2));
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(2, record.Attempt);
        Assert.True(record.PossibleDuplicateBilling);
    }

    [Fact]
    public async Task FirstAttemptIsNotFlaggedAsDuplicateBilling()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());

        using (var request = recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   "test",
                   "model-1",
                   null)))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1_000, null));
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(1, record.Attempt);
        Assert.False(record.PossibleDuplicateBilling);
    }
}