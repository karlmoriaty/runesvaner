using System.Diagnostics;

namespace Runesvaner;

public static class RunesvanerRequestContext
{
    private static readonly AsyncLocal<Context?> Current = new();

    public static IRunesvanerRequest? Request => Current.Value?.Request;

    public static bool IsActive => Current.Value is not null;

    public static IDisposable Begin(IRunesvanerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var previous = Current.Value;
        Current.Value = new Context(request, Stopwatch.GetTimestamp());
        return new Scope(previous);
    }

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