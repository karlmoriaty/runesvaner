namespace Runesvaner.Storage.Jsonl;

/// <summary>Read-only queries over JSON Lines session files.</summary>
public sealed class JsonlRunesvanerQueryStore : IRunesvanerQueryStore
{
    private readonly string _directory;

    /// <summary>Creates a query store over the supplied data directory.</summary>
    public JsonlRunesvanerQueryStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

    /// <summary>Reads the newest records, optionally filtered by session.</summary>
    public ValueTask<IReadOnlyList<RunesvanerRequestRecord>> ReadRequestsAsync(
        string? sessionId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var records = JsonlRunesvanerStore.ReadAll(_directory);

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

    /// <summary>Reads one request by identifier.</summary>
    public ValueTask<RunesvanerRequestRecord?> GetRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = JsonlRunesvanerStore.ReadAll(_directory)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal));

        return new ValueTask<RunesvanerRequestRecord?>(record);
    }

    /// <summary>Aggregates records into session summaries.</summary>
    public ValueTask<IReadOnlyList<RunesvanerSessionSummary>> ReadSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<IReadOnlyList<RunesvanerSessionSummary>>(
            RunesvanerSessionAggregator.Aggregate(JsonlRunesvanerStore.ReadAll(_directory)));
    }
}