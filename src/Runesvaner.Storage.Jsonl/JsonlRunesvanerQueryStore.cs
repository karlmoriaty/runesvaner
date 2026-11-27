namespace Runesvaner.Storage.Jsonl;

public sealed class JsonlRunesvanerQueryStore : IRunesvanerQueryStore
{
    private readonly string _directory;

    public JsonlRunesvanerQueryStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

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

    public ValueTask<IReadOnlyList<RunesvanerSessionSummary>> ReadSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<IReadOnlyList<RunesvanerSessionSummary>>(
            RunesvanerSessionAggregator.Aggregate(JsonlRunesvanerStore.ReadAll(_directory)));
    }
}