namespace Runesvaner;

public static class RunesvanerCostEngine
{
    private const decimal Million = 1_000_000m;

    public static CostBreakdown Calculate(
        RunesvanerUsage usage,
        PriceEntry? entry,
        string priceTableVersion,
        string currency = "USD")
    {
        if (entry is null)
        {
            return new CostBreakdown(
                currency,
                null,
                null,
                null,
                null,
                null,
                CostStatus.Unknown,
                priceTableVersion);
        }

        var input = usage.InputTokens;
        var output = usage.OutputTokens;
        var cached = usage.CachedInputTokens ?? 0;
        var nonCached = Math.Max(0, (input ?? 0) - cached);

        var inputCost = input is null
            ? (decimal?)null
            : TieredInputCost(nonCached, entry);

        var cachedCost = usage.CachedInputTokens is null
            ? (decimal?)null
            : cached * entry.CachedInputPerMillion / Million;

        var outputCost = output is null
            ? (decimal?)null
            : output.Value * entry.OutputPerMillion / Million;

        decimal? reasoningCost = null;
        if (usage.ReasoningTokens is not null)
        {
            reasoningCost = entry.ReasoningIncludedInOutput
                ? 0m
                : usage.ReasoningTokens.Value * (entry.ReasoningPerMillion ?? 0m) / Million;
        }

        var status = input is null || output is null
            ? CostStatus.Partial
            : CostStatus.Known;

        decimal? total = null;
        if (inputCost is not null || cachedCost is not null || outputCost is not null || reasoningCost is not null)
        {
            total = (inputCost ?? 0m) + (cachedCost ?? 0m) + (outputCost ?? 0m) + (reasoningCost ?? 0m);
            if (entry.BatchDiscount > 0m)
            {
                total *= 1m - entry.BatchDiscount;
            }
        }

        return new CostBreakdown(
            currency,
            Round6(inputCost),
            Round6(cachedCost),
            Round6(outputCost),
            Round6(reasoningCost),
            Round6(total),
            status,
            priceTableVersion,
            $"{entry.Provider}/{entry.Model}");
    }

    private static decimal TieredInputCost(decimal tokens, PriceEntry entry)
    {
        if (entry.Tiers is not { Count: > 0 })
        {
            return tokens * entry.InputPerMillion / Million;
        }

        var remaining = tokens;
        var previous = 0L;
        var cost = 0m;

        foreach (var tier in entry.Tiers.OrderBy(tier => tier.UpToInputTokens))
        {
            var capacity = tier.UpToInputTokens - previous;
            if (capacity <= 0)
            {
                continue;
            }

            var slice = Math.Min(remaining, capacity);
            cost += slice * tier.InputPerMillion / Million;
            remaining -= slice;
            previous = tier.UpToInputTokens;

            if (remaining <= 0m)
            {
                break;
            }
        }

        if (remaining > 0m)
        {
            cost += remaining * entry.InputPerMillion / Million;
        }

        return cost;
    }

    private static decimal? Round6(decimal? value)
        => value is null ? null : Math.Round(value.Value, 6, MidpointRounding.ToEven);
}