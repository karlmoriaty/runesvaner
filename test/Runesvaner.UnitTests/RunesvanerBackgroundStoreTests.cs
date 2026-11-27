using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class RunesvanerBackgroundStoreTests
{
    [Fact]
    public async Task FlushesQueuedRecordsToInnerStore()
    {
        var inner = new InMemoryRunesvanerStore();
        using var store = new RunesvanerBackgroundStore(
            inner,
            new RunesvanerOptions { BackgroundQueueCapacity = 8 });

        for (var i = 0; i < 3; i++)
        {
            await store.WriteAsync(CreateRecord(i), TestContext.Current.CancellationToken);
        }

        await store.FlushAsync(TestContext.Current.CancellationToken);

        var records = await inner.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken);
        var snapshot = store.Monitor.Snapshot();

        Assert.Equal(3, records.Count);
        Assert.Equal(3, snapshot.Written);
        Assert.Equal(0, snapshot.Dropped);
        Assert.Equal(0, snapshot.Failed);
    }

    [Fact]
    public async Task DropsOldestRecordWhenQueueIsFull()
    {
        var inner = new BlockingStore();

        try
        {
            using var store = new RunesvanerBackgroundStore(
                inner,
                new RunesvanerOptions { BackgroundQueueCapacity = 1 });

            for (var i = 0; i < 3; i++)
            {
                await store.WriteAsync(CreateRecord(i), TestContext.Current.CancellationToken);
            }

            Assert.True(store.Monitor.Snapshot().Dropped >= 1);

            inner.Release();
            await store.FlushAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            inner.Release();
        }
    }

    [Fact]
    public async Task CountsFailuresWithoutBreakingCaller()
    {
        using var store = new RunesvanerBackgroundStore(
            new ThrowingStore(),
            new RunesvanerOptions { BackgroundQueueCapacity = 8 });

        await store.WriteAsync(CreateRecord(0), TestContext.Current.CancellationToken);
        await store.FlushAsync(TestContext.Current.CancellationToken);

        var snapshot = store.Monitor.Snapshot();

        Assert.Equal(1, snapshot.Enqueued);
        Assert.Equal(0, snapshot.Written);
        Assert.Equal(1, snapshot.Failed);
    }

    [Fact]
    public async Task DegradesAfterConsecutiveFailures()
    {
        using var store = new RunesvanerBackgroundStore(
            new ThrowingStore(),
            new RunesvanerOptions { BackgroundQueueCapacity = 8 });

        for (var i = 0; i < 5; i++)
        {
            await store.WriteAsync(CreateRecord(i), TestContext.Current.CancellationToken);
        }

        await store.FlushAsync(TestContext.Current.CancellationToken);

        await store.WriteAsync(CreateRecord(5), TestContext.Current.CancellationToken);
        await store.FlushAsync(TestContext.Current.CancellationToken);

        var snapshot = store.Monitor.Snapshot();

        Assert.Equal(5, snapshot.Failed);
        Assert.True(snapshot.Dropped >= 1);
    }

    [Fact]
    public async Task AcceptsWritesAfterDisposeWithoutThrowing()
    {
        var store = new RunesvanerBackgroundStore(
            new InMemoryRunesvanerStore(),
            new RunesvanerOptions { BackgroundQueueCapacity = 4 });

        store.Dispose();
        await store.WriteAsync(CreateRecord(0), TestContext.Current.CancellationToken);

        Assert.True(store.Monitor.Snapshot().Dropped >= 1);
    }

    private static RunesvanerRequestRecord CreateRecord(int index) => new(
        "runesvaner.request.v1",
        Guid.NewGuid().ToString("N"),
        "Chat",
        "test",
        "model-" + index,
        "model-" + index,
        DateTimeOffset.UtcNow,
        1_200,
        400,
        "Ok",
        new RunesvanerUsage(100, 50, null, null, UsageSource.Provider),
        [new RunesvanerSegment(SegmentKind.Generation, 0, 1_200, false, SegmentMeasurement.Stopwatch)],
        null,
        null,
        null,
        "background-test");

    private sealed class BlockingStore : IRunesvanerStore
    {
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
            => new(_release.Task);

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public void Release() => _release.TrySetResult();
    }

    private sealed class ThrowingStore : IRunesvanerStore
    {
        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException(new InvalidOperationException("intentional failure"));

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}