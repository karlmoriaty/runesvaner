namespace Runesvaner;

/// <summary>Point-in-time counters for the background writer and persistence pipeline.</summary>
public sealed record RunesvanerSelfMonitorSnapshot(
    long Enqueued,
    long Written,
    long Dropped,
    long Failed,
    int QueueLength,
    double AveragePersistMilliseconds);

/// <summary>Thread-safe counters for queueing, persistence, drops, and failures.</summary>
public sealed class RunesvanerSelfMonitor
{
    private long _enqueued;
    private long _written;
    private long _dropped;
    private long _failed;
    private long _persistTicks;
    private long _queueLength;

    /// <summary>Records that one request was enqueued.</summary>
    public void RecordEnqueued() => Interlocked.Increment(ref _enqueued);

    /// <summary>Records a successful write and its persistence duration.</summary>
    public void RecordWritten(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _written);
        Interlocked.Add(ref _persistTicks, elapsed.Ticks);
    }

    /// <summary>Records a dropped request.</summary>
    public void RecordDropped() => Interlocked.Increment(ref _dropped);

    /// <summary>Records a failed write.</summary>
    public void RecordFailed() => Interlocked.Increment(ref _failed);

    /// <summary>Sets the current queue length.</summary>
    public void SetQueueLength(int length) => Interlocked.Exchange(ref _queueLength, length);

    /// <summary>Returns a consistent snapshot of all counters.</summary>
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