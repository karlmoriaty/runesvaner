using Microsoft.Extensions.VectorData;

namespace Runesvaner.VectorData;

/// <summary>Extension methods for wrapping VectorData searchable collections.</summary>
public static class RunesvanerVectorSearchableExtensions
{
    /// <summary>Wraps a vector searchable collection with Runesvaner recording.</summary>
    public static IVectorSearchable<TRecord> AsRunesvaner<TRecord>(
        this IVectorSearchable<TRecord> inner,
        IRunesvanerRecorder recorder,
        string collectionName,
        string? provider = null)
        where TRecord : class
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(collectionName);

        return new RunesvanerVectorSearchable<TRecord>(
            inner,
            recorder,
            collectionName,
            provider);
    }
}