namespace Runesvaner;

public sealed class InMemoryRunesvanerStore : IRunesvanerStore, IRunesvanerQueryStore
{
    private readonly object _gate = new();
    private readonly List<RunesvanerRequestRecord> _records = new();

    public ValueTask WriteAsync(
        RunesvanerRequestRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _records.Add(record);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<RunesvanerRequestRecord>> ReadRequestsAsync(
        string? sessionId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            IEnumerable<RunesvanerRequestRecord> records = _records;

            if (!string.IsNullOrEmpty(sessionId))
            {
                records = records.Where(record =>
                    string.Equals(record.SessionId, sessionId, StringComparison.Ordinal));
            }

            return new ValueTask<IReadOnlyList<RunesvanerRequestRecord>>(
                records
                    .OrderByDescending(record => record.StartedAtUtc)
                    .Take(Math.Max(1, limit))
                    .ToArray());
        }
    }

    public ValueTask<RunesvanerRequestRecord?> GetRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return new ValueTask<RunesvanerRequestRecord?>(
                _records.FirstOrDefault(record =>
                    string.Equals(record.RequestId, requestId, StringComparison.Ordinal)));
        }
    }

    public ValueTask<IReadOnlyList<RunesvanerSessionSummary>> ReadSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return new ValueTask<IReadOnlyList<RunesvanerSessionSummary>>(
                RunesvanerSessionAggregator.Aggregate(_records));
        }
    }
}