using Microsoft.Extensions.AI;

namespace Runesvaner.MicrosoftExtensionsAI;

public static class RunesvanerEmbeddingGeneratorBuilderExtensions
{
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