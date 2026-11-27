namespace Runesvaner;

/// <summary>Whether a cost could be computed for a request.</summary>
public enum CostStatus
{
    /// <summary>All required usage and price data were available.</summary>
    Known,

    /// <summary>Only part of the usage data was available; known components were summed.</summary>
    Partial,

    /// <summary>No price entry matched; the cost is intentionally unknown.</summary>
    Unknown
}

/// <summary>One tier of progressive input pricing.</summary>
public sealed record PriceTier(
    long UpToInputTokens,
    decimal InputPerMillion,
    decimal OutputPerMillion,
    decimal CachedInputPerMillion = 0m,
    decimal? ReasoningPerMillion = null);

/// <summary>Price data for one provider and model.</summary>
public sealed record PriceEntry(
    string Provider,
    string Model,
    decimal InputPerMillion,
    decimal CachedInputPerMillion,
    decimal OutputPerMillion,
    decimal? ReasoningPerMillion = null,
    IReadOnlyList<PriceTier>? Tiers = null,
    decimal BatchDiscount = 0m,
    bool ReasoningIncludedInOutput = true);

/// <summary>A versioned price table with provider/model entries.</summary>
public sealed record PriceTable(
    string Version,
    string Currency,
    IReadOnlyDictionary<string, PriceEntry> Entries,
    string? Source = null,
    DateTimeOffset? RetrievedAt = null)
{
    /// <summary>Looks up a provider/model entry.</summary>
    public bool TryGet(string provider, string model, out PriceEntry entry)
        => Entries.TryGetValue($"{provider}/{model}", out entry!);
}

/// <summary>Itemized cost for one request.</summary>
public sealed record CostBreakdown(
    string Currency,
    decimal? Input,
    decimal? CachedInput,
    decimal? Output,
    decimal? Reasoning,
    decimal? Total,
    CostStatus Status,
    string PriceTableVersion,
    string? PriceEntryId = null);