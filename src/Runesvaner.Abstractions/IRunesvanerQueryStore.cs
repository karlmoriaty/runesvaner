namespace Runesvaner;

public interface IRunesvanerQueryStore
{
    ValueTask<IReadOnlyList<RunesvanerRequestRecord>> ReadRequestsAsync(
        string? sessionId,
        int limit,
        CancellationToken cancellationToken = default);

    ValueTask<RunesvanerRequestRecord?> GetRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RunesvanerSessionSummary>> ReadSessionsAsync(
        CancellationToken cancellationToken = default);
}

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