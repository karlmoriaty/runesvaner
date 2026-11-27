using System.Diagnostics;

namespace Runesvaner;

/// <summary>Async-local correlation between a recorder request and HTTP callbacks.</summary>
public static class RunesvanerRequestContext
{
    private static readonly AsyncLocal<Context?> Current = new();

    /// <summary>Gets the active recorder request, or null outside a request scope.</summary>
    public static IRunesvanerRequest? Request => Current.Value?.Request;

    /// <summary>Gets whether a request scope is active.</summary>
    public static bool IsActive => Current.Value is not null;

    /// <summary>Begins a request scope and returns a disposable that restores the previous scope.</summary>
    public static IDisposable Begin(IRunesvanerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var previous = Current.Value;
        Current.Value = new Context(request, Stopwatch.GetTimestamp());
        return new Scope(previous);
    }

    /// <summary>Converts an absolute stopwatch timestamp into microseconds relative to the request start.</summary>
    public static long ElapsedMicroseconds(long timestamp)
    {
        var context = Current.Value;
        if (context is null)
        {
            return 0;
        }

        var delta = timestamp - context.StartedTimestamp;
        return delta <= 0 ? 0 : delta * 1_000_000 / Stopwatch.Frequency;
    }

    /// <summary>Returns the current time in microseconds relative to the request start.</summary>
    public static long NowMicroseconds()
        => ElapsedMicroseconds(Stopwatch.GetTimestamp());

    private sealed record Context(IRunesvanerRequest Request, long StartedTimestamp);

    private sealed class Scope : IDisposable
    {
        private readonly Context? _previous;
        private bool _disposed;

        public Scope(Context? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Current.Value = _previous;
        }
    }
}