using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Runesvaner;

public sealed class RunesvanerBackgroundStore : IRunesvanerStore, IDisposable
{
    private const int FailureThreshold = 5;
    private static readonly TimeSpan DegradeDuration = TimeSpan.FromSeconds(30);

    private readonly IRunesvanerStore _inner;
    private readonly RunesvanerSelfMonitor _monitor;
    private readonly int _capacity;
    private readonly Queue<RunesvanerRequestRecord> _queue = new();
    private readonly object _gate = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _pump;
    private int _inFlight;
    private int _consecutiveFailures;
    private long _degradedUntil;
    private int _disposed;

    public RunesvanerBackgroundStore(
        IRunesvanerStore inner,
        RunesvanerOptions? options = null,
        RunesvanerSelfMonitor? monitor = null)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _monitor = monitor ?? new RunesvanerSelfMonitor();
        _capacity = Math.Clamp(options?.BackgroundQueueCapacity ?? 4_096, 1, 1_000_000);
        _pump = Task.Run(() => PumpAsync(_stop.Token));
    }

    public RunesvanerSelfMonitor Monitor => _monitor;

    public ValueTask WriteAsync(
        RunesvanerRequestRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        if (Volatile.Read(ref _disposed) != 0)
        {
            _monitor.RecordDropped();
            return ValueTask.CompletedTask;
        }

        lock (_gate)
        {
            if (_queue.Count >= _capacity)
            {
                _queue.Dequeue();
                _monitor.RecordDropped();
            }

            _queue.Enqueue(record);
            _monitor.SetQueueLength(_queue.Count);
        }

        _monitor.RecordEnqueued();

        try
        {
            _signal.Release();
        }
        catch (ObjectDisposedException)
        {
            _monitor.RecordDropped();
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_stop.IsCancellationRequested)
            {
                break;
            }

            int pending;
            lock (_gate)
            {
                pending = _queue.Count;
            }

            if (pending == 0 && Volatile.Read(ref _inFlight) == 0)
            {
                break;
            }

            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
        }

        await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            FlushAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
        }

        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _stop.Cancel();

        try
        {
            _pump.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
        }

        _signal.Dispose();
        _stop.Dispose();
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!TryDequeue(out var record))
            {
                continue;
            }

            if (IsDegraded())
            {
                _monitor.RecordDropped();
                continue;
            }

            Interlocked.Exchange(ref _inFlight, 1);
            var started = Stopwatch.GetTimestamp();

            try
            {
                await _inner.WriteAsync(record, cancellationToken).ConfigureAwait(false);
                _monitor.RecordWritten(Stopwatch.GetElapsedTime(started));
                Interlocked.Exchange(ref _consecutiveFailures, 0);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _monitor.RecordDropped();
                break;
            }
            catch (Exception)
            {
                _monitor.RecordFailed();

                if (Interlocked.Increment(ref _consecutiveFailures) >= FailureThreshold)
                {
                    var until = Stopwatch.GetTimestamp() +
                        (long)(DegradeDuration.TotalSeconds * Stopwatch.Frequency);
                    Interlocked.Exchange(ref _degradedUntil, until);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _inFlight, 0);
            }
        }
    }

    private bool TryDequeue([NotNullWhen(true)] out RunesvanerRequestRecord? record)
    {
        lock (_gate)
        {
            if (!_queue.TryDequeue(out record))
            {
                _monitor.SetQueueLength(0);
                return false;
            }

            _monitor.SetQueueLength(_queue.Count);
            return true;
        }
    }

    private bool IsDegraded()
    {
        var until = Interlocked.Read(ref _degradedUntil);

        if (until == 0)
        {
            return false;
        }

        if (Stopwatch.GetTimestamp() < until)
        {
            return true;
        }

        Interlocked.Exchange(ref _degradedUntil, 0);
        Interlocked.Exchange(ref _consecutiveFailures, 0);
        return false;
    }
}