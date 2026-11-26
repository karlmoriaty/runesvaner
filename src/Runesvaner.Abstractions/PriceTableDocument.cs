using System.Text.Json.Serialization;

namespace Runesvaner;

public sealed record PriceTableDocument(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("retrievedAt")] DateTimeOffset? RetrievedAt,
    [property: JsonPropertyName("entries")] PriceTableDocumentEntry[] Entries);

public sealed record PriceTableDocumentEntry(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("aliases")] string[]? Aliases,
    [property: JsonPropertyName("inputPerMillion")] decimal InputPerMillion,
    [property: JsonPropertyName("cachedInputPerMillion")] decimal CachedInputPerMillion,
    [property: JsonPropertyName("outputPerMillion")] decimal OutputPerMillion,
    [property: JsonPropertyName("reasoningPerMillion")] decimal? ReasoningPerMillion,
    [property: JsonPropertyName("reasoningIncludedInOutput")] bool ReasoningIncludedInOutput = true,
    [property: JsonPropertyName("batchDiscount")] decimal BatchDiscount = 0m,
    [property: JsonPropertyName("tiers")] PriceTableDocumentTier[]? Tiers = null);

public sealed record PriceTableDocumentTier(
    [property: JsonPropertyName("upToInputTokens")] long UpToInputTokens,
    [property: JsonPropertyName("inputPerMillion")] decimal InputPerMillion,
    [property: JsonPropertyName("outputPerMillion")] decimal OutputPerMillion,
    [property: JsonPropertyName("cachedInputPerMillion")] decimal CachedInputPerMillion = 0m,
    [property: JsonPropertyName("reasoningPerMillion")] decimal? ReasoningPerMillion = null);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PriceTableDocument))]
public partial class RunesvanerPricingJsonContext : JsonSerializerContext
{
}