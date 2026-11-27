using System.Text.Json;

namespace Runesvaner;

public static class RunesvanerPriceTableLoader
{
    private static readonly Lazy<PriceTable> BuiltIn = new(LoadBuiltInCore);

    public static PriceTable Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        using var stream = File.OpenRead(fullPath);

        var document = JsonSerializer.Deserialize(
            stream,
            RunesvanerPricingJsonContext.Default.PriceTableDocument)
            ?? throw new InvalidDataException($"Price table is empty: {fullPath}");

        return Create(document, fullPath);
    }

    public static PriceTable LoadFromJson(string json)
    {
        var document = JsonSerializer.Deserialize(
            json,
            RunesvanerPricingJsonContext.Default.PriceTableDocument)
            ?? throw new InvalidDataException("Price table JSON is empty.");

        return Create(document, null);
    }

    public static PriceTable LoadBuiltIn() => BuiltIn.Value;

    private static PriceTable LoadBuiltInCore()
    {
        using var stream = typeof(RunesvanerPriceTableLoader).Assembly
            .GetManifestResourceStream("Runesvaner.Pricing.builtin-prices.json")
            ?? throw new InvalidOperationException("Built-in price table resource is missing.");

        var document = JsonSerializer.Deserialize(
            stream,
            RunesvanerPricingJsonContext.Default.PriceTableDocument)
            ?? throw new InvalidDataException("Built-in price table is empty.");

        return Create(document, "builtin");
    }

    private static PriceTable Create(PriceTableDocument document, string? path)
    {
        if (string.IsNullOrWhiteSpace(document.Version))
        {
            throw new InvalidDataException($"Price table version is required: {path ?? "<json>"}");
        }

        if (string.IsNullOrWhiteSpace(document.Currency))
        {
            throw new InvalidDataException($"Price table currency is required: {path ?? "<json>"}");
        }

        if (document.Entries is null || document.Entries.Length == 0)
        {
            throw new InvalidDataException($"Price table entries are required: {path ?? "<json>"}");
        }

        var entries = new Dictionary<string, PriceEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in document.Entries)
        {
            if (string.IsNullOrWhiteSpace(source.Provider) || string.IsNullOrWhiteSpace(source.Model))
            {
                throw new InvalidDataException("Every price entry requires provider and model.");
            }

            ValidateNonNegative(source.InputPerMillion, "inputPerMillion", source.Provider, source.Model);
            ValidateNonNegative(source.CachedInputPerMillion, "cachedInputPerMillion", source.Provider, source.Model);
            ValidateNonNegative(source.OutputPerMillion, "outputPerMillion", source.Provider, source.Model);

            if (source.ReasoningPerMillion is < 0m)
            {
                throw new InvalidDataException(
                    $"reasoningPerMillion must not be negative for {source.Provider}/{source.Model}.");
            }

            if (source.BatchDiscount is < 0m or > 1m)
            {
                throw new InvalidDataException(
                    $"batchDiscount must be between 0 and 1 for {source.Provider}/{source.Model}.");
            }

            var tiers = source.Tiers?.Select(tier =>
            {
                ValidateNonNegative(tier.InputPerMillion, "tier.inputPerMillion", source.Provider, source.Model);
                ValidateNonNegative(tier.OutputPerMillion, "tier.outputPerMillion", source.Provider, source.Model);
                ValidateNonNegative(tier.CachedInputPerMillion, "tier.cachedInputPerMillion", source.Provider, source.Model);

                if (tier.ReasoningPerMillion is < 0m)
                {
                    throw new InvalidDataException(
                        $"tier.reasoningPerMillion must not be negative for {source.Provider}/{source.Model}.");
                }

                return new PriceTier(
                    tier.UpToInputTokens,
                    tier.InputPerMillion,
                    tier.OutputPerMillion,
                    tier.CachedInputPerMillion,
                    tier.ReasoningPerMillion);
            }).ToArray();

            ValidateTiers(tiers, source.Provider, source.Model);

            var entry = new PriceEntry(
                source.Provider,
                source.Model,
                source.InputPerMillion,
                source.CachedInputPerMillion,
                source.OutputPerMillion,
                source.ReasoningPerMillion,
                tiers,
                source.BatchDiscount,
                source.ReasoningIncludedInOutput);

            Add(entries, $"{source.Provider}/{source.Model}", entry);

            foreach (var alias in source.Aliases ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    Add(entries, $"{source.Provider}/{alias}", entry);
                }
            }
        }

        return new PriceTable(
            document.Version,
            document.Currency,
            entries,
            document.Source,
            document.RetrievedAt);
    }

    private static void Add(
        Dictionary<string, PriceEntry> entries,
        string key,
        PriceEntry entry)
    {
        if (!entries.TryAdd(key, entry))
        {
            throw new InvalidDataException($"Duplicate price key: {key}");
        }
    }

    private static void ValidateNonNegative(
        decimal value,
        string field,
        string provider,
        string model)
    {
        if (value < 0m)
        {
            throw new InvalidDataException(
                $"{field} must not be negative for {provider}/{model}.");
        }
    }

    private static void ValidateTiers(
        PriceTier[]? tiers,
        string provider,
        string model)
    {
        if (tiers is null || tiers.Length == 0)
        {
            return;
        }

        var previous = 0L;
        foreach (var tier in tiers.OrderBy(tier => tier.UpToInputTokens))
        {
            if (tier.UpToInputTokens <= previous)
            {
                throw new InvalidDataException(
                    $"tier.upToInputTokens must be strictly increasing for {provider}/{model}.");
            }

            previous = tier.UpToInputTokens;
        }
    }
}