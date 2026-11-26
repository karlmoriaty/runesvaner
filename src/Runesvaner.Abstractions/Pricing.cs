namespace Runesvaner;

public enum CostStatus
{
    Known,
    Partial,
    Unknown
}

public sealed record PriceTier(
    long UpToInputTokens,
    decimal InputPerMillion,
    decimal OutputPerMillion,
    decimal CachedInputPerMillion = 0m,
    decimal? ReasoningPerMillion = null);

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

public sealed record PriceTable(
    string Version,
    string Currency,
    IReadOnlyDictionary<string, PriceEntry> Entries,
    string? Source = null,
    DateTimeOffset? RetrievedAt = null)
{
    public bool TryGet(string provider, string model, out PriceEntry entry)
        => Entries.TryGetValue($"{provider}/{model}", out entry!);
}

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