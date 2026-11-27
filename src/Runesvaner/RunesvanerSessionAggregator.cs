namespace Runesvaner;

/// <summary>Aggregates request records into diagnostic session summaries.</summary>
public static class RunesvanerSessionAggregator
{
    /// <summary>Groups records by session and computes counts, costs, models, and providers.</summary>
    public static IReadOnlyList<RunesvanerSessionSummary> Aggregate(
        IEnumerable<RunesvanerRequestRecord> records)
    {
        return records
            .GroupBy(SessionKey)
            .Select(group =>
            {
                var items = group.ToArray();
                var startedAt = items.Min(record => record.StartedAtUtc);
                var endedAt = items.Max(record =>
                    record.StartedAtUtc.AddTicks(record.DurationUs * 10));

                return new RunesvanerSessionSummary(
                    group.Key,
                    startedAt,
                    endedAt,
                    items.Length,
                    items.Count(record => record.Cost?.Status == CostStatus.Known),
                    items.Count(record => record.Cost?.Status == CostStatus.Partial),
                    items.Count(record => record.Cost?.Status == CostStatus.Unknown),
                    items.Sum(record => record.Cost is { Status: CostStatus.Known or CostStatus.Partial, Total: { } total }
                        ? total * (decimal)record.SamplingWeight
                        : 0m),
                    items
                        .Select(record => record.RequestModel ?? record.ResponseModel)
                        .Where(model => !string.IsNullOrWhiteSpace(model))
                        .Select(model => model!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(model => model, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    items
                        .Select(record => record.Provider)
                        .Where(provider => !string.IsNullOrWhiteSpace(provider))
                        .Select(provider => provider!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(provider => provider, StringComparer.OrdinalIgnoreCase)
                        .ToArray());
            })
            .OrderByDescending(session => session.StartedAtUtc)
            .ToArray();
    }

    private static string SessionKey(RunesvanerRequestRecord record)
        => record.SessionId ?? record.StartedAtUtc.ToString("yyyyMMdd");
}