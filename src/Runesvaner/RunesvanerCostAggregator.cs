namespace Runesvaner;

public sealed record RunesvanerCostSummary(
    string Provider,
    string Model,
    int Requests,
    int Priced,
    int Partial,
    int Unknown,
    decimal Total);

public static class RunesvanerCostAggregator
{
    public static IReadOnlyList<RunesvanerCostSummary> Aggregate(
        IEnumerable<RunesvanerRequestRecord> records)
    {
        return records
            .GroupBy(record => new
            {
                Provider = record.Provider ?? "-",
                Model = record.RequestModel ?? record.ResponseModel ?? "-"
            })
            .Select(group => new RunesvanerCostSummary(
                group.Key.Provider,
                group.Key.Model,
                group.Count(),
                group.Count(record => record.Cost?.Status == CostStatus.Known),
                group.Count(record => record.Cost?.Status == CostStatus.Partial),
                group.Count(record => record.Cost?.Status == CostStatus.Unknown),
                group.Sum(record =>
                    record.Cost is { Status: CostStatus.Known or CostStatus.Partial, Total: { } total }
                        ? total
                        : 0m)))
            .OrderByDescending(summary => summary.Total)
            .ThenBy(summary => summary.Provider, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Model, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}