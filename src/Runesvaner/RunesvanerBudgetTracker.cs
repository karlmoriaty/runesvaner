namespace Runesvaner;

/// <summary>Budget thresholds and the alert callback.</summary>
public sealed class RunesvanerBudgetOptions
{
    /// <summary>Gets or sets the per-request cost limit in USD.</summary>
    public decimal? RequestLimitUsd { get; set; }

    /// <summary>Gets or sets the per-session cumulative cost limit in USD.</summary>
    public decimal? SessionLimitUsd { get; set; }

    /// <summary>Gets or sets the per-feature cumulative cost limit in USD; uses the feature tag.</summary>
    public decimal? FeatureLimitUsd { get; set; }

    /// <summary>Gets or sets a callback invoked when a budget is exceeded.</summary>
    public Action<RunesvanerBudgetAlert>? AlertCallback { get; set; }
}

/// <summary>Budget overrun alert raised once per scope and key.</summary>
public sealed record RunesvanerBudgetAlert(
    string Scope,
    string Key,
    decimal Limit,
    decimal Spent);

/// <summary>Tracks costs against request, session, and feature budgets.</summary>
public sealed class RunesvanerBudgetTracker
{
    private readonly RunesvanerBudgetOptions _options;
    private readonly object _gate = new();
    private readonly Dictionary<string, decimal> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, decimal> _features = new(StringComparer.Ordinal);
    private readonly HashSet<string> _alerted = new(StringComparer.Ordinal);

    /// <summary>Creates a tracker for the supplied budget options.</summary>
    public RunesvanerBudgetTracker(RunesvanerBudgetOptions? options = null)
        => _options = options ?? new RunesvanerBudgetOptions();

    /// <summary>Raised when a budget is exceeded for the first time in a scope.</summary>
    public event Action<RunesvanerBudgetAlert>? Alert;

    /// <summary>Observes one completed record and raises alerts for exceeded budgets.</summary>
    public void Observe(RunesvanerRequestRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var cost = record.Cost;
        if (cost?.Total is not { } total || cost.Status == CostStatus.Unknown)
        {
            return;
        }

        List<RunesvanerBudgetAlert> alerts;

        lock (_gate)
        {
            alerts = new List<RunesvanerBudgetAlert>(3);

            if (_options.RequestLimitUsd is { } requestLimit)
            {
                AddIfExceeded(alerts, "request", record.RequestId, requestLimit, total);
            }

            if (_options.SessionLimitUsd is { } sessionLimit &&
                !string.IsNullOrEmpty(record.SessionId))
            {
                AddCumulative(alerts, _sessions, "session", record.SessionId, sessionLimit, total);
            }

            if (_options.FeatureLimitUsd is { } featureLimit &&
                record.Tags is { } tags &&
                tags.TryGetValue("feature", out var feature) &&
                !string.IsNullOrWhiteSpace(feature))
            {
                AddCumulative(alerts, _features, "feature", feature, featureLimit, total);
            }
        }

        foreach (var alert in alerts)
        {
            try
            {
                _options.AlertCallback?.Invoke(alert);
            }
            catch (Exception)
            {
                // Budget reporting must never break the business request.
            }

            try
            {
                Alert?.Invoke(alert);
            }
            catch (Exception)
            {
            }
        }
    }

    private void AddIfExceeded(
        List<RunesvanerBudgetAlert> alerts,
        string scope,
        string key,
        decimal limit,
        decimal spent)
    {
        if (spent <= limit || !_alerted.Add($"{scope}:{key}"))
        {
            return;
        }

        alerts.Add(new RunesvanerBudgetAlert(scope, key, limit, spent));
    }

    private void AddCumulative(
        List<RunesvanerBudgetAlert> alerts,
        Dictionary<string, decimal> totals,
        string scope,
        string key,
        decimal limit,
        decimal amount)
    {
        var spent = totals.GetValueOrDefault(key) + amount;
        totals[key] = spent;

        if (spent <= limit || !_alerted.Add($"{scope}:{key}"))
        {
            return;
        }

        alerts.Add(new RunesvanerBudgetAlert(scope, key, limit, spent));
    }
}