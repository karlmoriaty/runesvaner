namespace Runesvaner;

/// <summary>Read-only query surface over captured requests.</summary>
public interface IRunesvanerQueryStore
{
    /// <summary>Reads the newest requests, optionally filtered by session.</summary>
    ValueTask<IReadOnlyList<RunesvanerRequestRecord>> ReadRequestsAsync(
        string? sessionId,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one request by identifier, or null when it does not exist.</summary>
    ValueTask<RunesvanerRequestRecord?> GetRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    /// <summary>Aggregates captured requests into session summaries.</summary>
    ValueTask<IReadOnlyList<RunesvanerSessionSummary>> ReadSessionsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>Aggregated view of one diagnostic session.</summary>
public sealed record RunesvanerSessionSummary(
    string SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    int Requests,
    int Priced,
    int Partial,
    int Unknown,
    decimal TotalCost,
    string[] Models,
    string[] Providers);