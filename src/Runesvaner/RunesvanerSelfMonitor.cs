namespace Runesvaner;

public sealed record RunesvanerSelfMonitorSnapshot(
    long Enqueued,
    long Written,
    long Dropped,
    long Failed,
    int QueueLength,
    double AveragePersistMilliseconds);

public sealed class RunesvanerSelfMonitor
{
    private long _enqueued;
    private long _written;
    private long _dropped;
    private long _failed;
    private long _persistTicks;
    private long _queueLength;

    public void RecordEnqueued() => Interlocked.Increment(ref _enqueued);

    public void RecordWritten(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _written);
        Interlocked.Add(ref _persistTicks, elapsed.Ticks);
    }

    public void RecordDropped() => Interlocked.Increment(ref _dropped);

    public void RecordFailed() => Interlocked.Increment(ref _failed);

    public void SetQueueLength(int length) => Interlocked.Exchange(ref _queueLength, length);

    public RunesvanerSelfMonitorSnapshot Snapshot()
    {
        var written = Interlocked.Read(ref _written);
        var ticks = Interlocked.Read(ref _persistTicks);

        return new RunesvanerSelfMonitorSnapshot(
            Interlocked.Read(ref _enqueued),
            written,
            Interlocked.Read(ref _dropped),
            Interlocked.Read(ref _failed),
            (int)Interlocked.Read(ref _queueLength),
            written == 0 ? 0 : TimeSpan.FromTicks(ticks).TotalMilliseconds / written);
    }
}