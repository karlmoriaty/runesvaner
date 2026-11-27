using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.AI;

namespace Runesvaner.MicrosoftExtensionsAI;

public sealed class RunesvanerEmbeddingGenerator
    : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _inner;
    private readonly IRunesvanerRecorder _recorder;
    private readonly string? _provider;

    public RunesvanerEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> inner,
        IRunesvanerRecorder recorder,
        string? provider = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _provider = provider;
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_recorder.IsEnabled)
        {
            return await _inner.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        }

        var inputs = values as IReadOnlyCollection<string> ?? values.ToList();
        var tags = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["embedding.count"] = inputs.Count.ToString(CultureInfo.InvariantCulture)
        };

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Embeddings,
            _provider,
            options?.ModelId,
            null,
            tags));

        var started = Stopwatch.GetTimestamp();

        try
        {
            var embeddings = await _inner
                .GenerateAsync(inputs, options, cancellationToken)
                .ConfigureAwait(false);
            var elapsed = Microseconds(Stopwatch.GetElapsedTime(started));

            if (embeddings.Usage is { } usage)
            {
                request.RecordUsage(new RunesvanerUsage(
                    usage.InputTokenCount,
                    usage.OutputTokenCount,
                    usage.CachedInputTokenCount,
                    usage.ReasoningTokenCount,
                    UsageSource.Provider));
            }

            if (embeddings.Count > 0)
            {
                tags["embedding.dimensions"] = embeddings[0]
                    .Vector
                    .Length
                    .ToString(CultureInfo.InvariantCulture);
            }

            request.RecordSegment(new RunesvanerSegment(
                SegmentKind.Embedding,
                0,
                elapsed,
                false,
                SegmentMeasurement.Stopwatch));

            request.Complete(new RunesvanerRequestResult(
                RequestStatus.Ok,
                elapsed,
                null,
                ResponseModel: options?.ModelId));

            return embeddings;
        }
        catch (OperationCanceledException)
        {
            request.Complete(new RunesvanerRequestResult(
                RequestStatus.Cancelled,
                Microseconds(Stopwatch.GetElapsedTime(started)),
                null));
            throw;
        }
        catch (Exception)
        {
            request.Complete(new RunesvanerRequestResult(
                RequestStatus.Error,
                Microseconds(Stopwatch.GetElapsedTime(started)),
                null));
            throw;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);

    public void Dispose()
        => _inner.Dispose();

    private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
}