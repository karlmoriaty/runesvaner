using Microsoft.Extensions.AI;

namespace Runesvaner.MicrosoftExtensionsAI;

/// <summary>EmbeddingGeneratorBuilder extensions for Runesvaner recording.</summary>
public static class RunesvanerEmbeddingGeneratorBuilderExtensions
{
    /// <summary>Adds recording with an explicit recorder instance.</summary>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> UseRunesvaner(
        this EmbeddingGeneratorBuilder<string, Embedding<float>> builder,
        IRunesvanerRecorder recorder,
        string? provider = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(recorder);

        builder.Use(inner => new RunesvanerEmbeddingGenerator(inner, recorder, provider));
        return builder;
    }

    /// <summary>Adds recording and resolves the recorder from dependency injection.</summary>
    public static EmbeddingGeneratorBuilder<string, Embedding<float>> UseRunesvaner(
        this EmbeddingGeneratorBuilder<string, Embedding<float>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Use((inner, services) =>
        {
            var recorder = services.GetService(typeof(IRunesvanerRecorder)) as IRunesvanerRecorder
                ?? throw new InvalidOperationException(
                    "IRunesvanerRecorder is not registered. Call AddRunesvaner() before building the embedding generator.");

            var provider =
                (services.GetService(typeof(RunesvanerOptions)) as RunesvanerOptions)?.Provider;

            return new RunesvanerEmbeddingGenerator(inner, recorder, provider);
        });

        return builder;
    }
}