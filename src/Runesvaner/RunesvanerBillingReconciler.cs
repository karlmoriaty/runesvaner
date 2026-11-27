using System.Globalization;

namespace Runesvaner;

/// <summary>One provider billing line loaded from a CSV file.</summary>
public sealed record RunesvanerBillingLine(
    string Provider,
    string Model,
    decimal Amount,
    string Currency = "USD");

/// <summary>Recorded versus billed cost for one provider/model pair.</summary>
public sealed record RunesvanerReconciliationRow(
    string Provider,
    string Model,
    decimal Recorded,
    decimal Billed,
    decimal Difference,
    double DifferencePercent);

/// <summary>Reconciliation result for all provider/model pairs.</summary>
public sealed record RunesvanerReconciliationReport(
    IReadOnlyList<RunesvanerReconciliationRow> Rows,
    decimal RecordedTotal,
    decimal BilledTotal,
    decimal DifferenceTotal)
{
    /// <summary>Returns whether every row is within the supplied percentage tolerance.</summary>
    public bool WithinTolerance(double thresholdPercent)
        => Rows.All(row => Math.Abs(row.DifferencePercent) <= thresholdPercent);
}

/// <summary>Compares recorded costs with provider billing lines.</summary>
public static class RunesvanerBillingReconciler
{
    /// <summary>Groups both sides by provider/model and computes absolute and percentage differences.</summary>
    public static RunesvanerReconciliationReport Reconcile(
        IEnumerable<RunesvanerRequestRecord> records,
        IEnumerable<RunesvanerBillingLine> billingLines)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(billingLines);

        var recorded = records
            .Where(record => record.Cost is { Status: CostStatus.Known or CostStatus.Partial, Total: not null })
            .GroupBy(record => new
            {
                Provider = (record.Provider ?? "-").ToLowerInvariant(),
                Model = (record.RequestModel ?? record.ResponseModel ?? "-").ToLowerInvariant()
            })
            .ToDictionary(
                group => (group.Key.Provider, group.Key.Model),
                group => group.Sum(record => record.Cost!.Total!.Value));

        var billed = billingLines
            .GroupBy(line => (line.Provider.ToLowerInvariant(), line.Model.ToLowerInvariant()))
            .ToDictionary(
                group => group.Key,
                group => group.Sum(line => line.Amount));

        var keys = recorded.Keys
            .Union(billed.Keys)
            .OrderBy(key => key.Item1, StringComparer.Ordinal)
            .ThenBy(key => key.Item2, StringComparer.Ordinal);

        var rows = keys.Select(key =>
        {
            var recordedAmount = recorded.GetValueOrDefault(key);
            var billedAmount = billed.GetValueOrDefault(key);
            var difference = recordedAmount - billedAmount;
            var percent = billedAmount == 0
                ? (recordedAmount == 0 ? 0 : 100)
                : (double)(difference / billedAmount * 100m);

            return new RunesvanerReconciliationRow(
                key.Item1,
                key.Item2,
                recordedAmount,
                billedAmount,
                difference,
                percent);
        }).ToArray();

        return new RunesvanerReconciliationReport(
            rows,
            rows.Sum(row => row.Recorded),
            rows.Sum(row => row.Billed),
            rows.Sum(row => row.Difference));
    }
}

/// <summary>Parses provider billing CSV files with provider, model, amount, and currency columns.</summary>
public static class RunesvanerBillingCsv
{
    /// <summary>Parses a billing CSV file.</summary>
    public static IReadOnlyList<RunesvanerBillingLine> Parse(string path)
        => ParseLines(File.ReadLines(path));

    /// <summary>Parses billing CSV lines, skipping headers, comments, and blank rows.</summary>
    public static IReadOnlyList<RunesvanerBillingLine> ParseLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var result = new List<RunesvanerBillingLine>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) ||
                line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var fields = Split(line);

            if (fields.Count < 3)
            {
                throw new InvalidDataException($"Billing row must have at least 3 columns: {line}");
            }

            if (string.Equals(fields[0], "provider", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!decimal.TryParse(
                    fields[2],
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount))
            {
                throw new InvalidDataException($"Invalid billing amount: {fields[2]}");
            }

            result.Add(new RunesvanerBillingLine(
                fields[0],
                fields[1],
                amount,
                fields.Count > 3 && !string.IsNullOrWhiteSpace(fields[3])
                    ? fields[3]
                    : "USD"));
        }

        return result;
    }

    private static List<string> Split(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}