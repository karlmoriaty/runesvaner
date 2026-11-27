using System.Text.Json.Serialization;

namespace Runesvaner;

/// <summary>JSON document shape for a price table file.</summary>
public sealed record PriceTableDocument(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("retrievedAt")] DateTimeOffset? RetrievedAt,
    [property: JsonPropertyName("entries")] PriceTableDocumentEntry[] Entries);

/// <summary>One entry in a price table JSON document.</summary>
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

/// <summary>One pricing tier in a price table JSON document.</summary>
public sealed record PriceTableDocumentTier(
    [property: JsonPropertyName("upToInputTokens")] long UpToInputTokens,
    [property: JsonPropertyName("inputPerMillion")] decimal InputPerMillion,
    [property: JsonPropertyName("outputPerMillion")] decimal OutputPerMillion,
    [property: JsonPropertyName("cachedInputPerMillion")] decimal CachedInputPerMillion = 0m,
    [property: JsonPropertyName("reasoningPerMillion")] decimal? ReasoningPerMillion = null);

/// <summary>Source-generated JSON context for price table documents.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PriceTableDocument))]
public partial class RunesvanerPricingJsonContext : JsonSerializerContext
{
}