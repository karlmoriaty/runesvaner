namespace Runesvaner;

/// <summary>Thread-safe in-memory store with query support.</summary>
public sealed class InMemoryRunesvanerStore : IRunesvanerStore, IRunesvanerQueryStore
{
    private readonly object _gate = new();
    private readonly List<RunesvanerRequestRecord> _records = new();

    /// <summary>Stores one request record in memory.</summary>
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

    /// <summary>No-op flush for the in-memory store.</summary>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <summary>Reads the newest records, optionally filtered by session.</summary>
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

    /// <summary>Reads one request by identifier.</summary>
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

    /// <summary>Aggregates in-memory records into session summaries.</summary>
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