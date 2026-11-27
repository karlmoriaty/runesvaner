using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.VectorData;

namespace Runesvaner.VectorData;

/// <summary>VectorData search decorator that records retrieval latency, collection, and top_k.</summary>
public sealed class RunesvanerVectorSearchable<TRecord> : IVectorSearchable<TRecord>
    where TRecord : class
{
    private readonly IVectorSearchable<TRecord> _inner;
    private readonly IRunesvanerRecorder _recorder;
    private readonly string _collectionName;
    private readonly string? _provider;

    /// <summary>Creates a recording decorator around a vector searchable collection.</summary>
    public RunesvanerVectorSearchable(
        IVectorSearchable<TRecord> inner,
        IRunesvanerRecorder recorder,
        string collectionName,
        string? provider = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        _provider = provider;
    }

    /// <summary>Searches the collection and records retrieval latency and search options.</summary>
    public async IAsyncEnumerable<VectorSearchResult<TRecord>> SearchAsync<TVector>(
        TVector vector,
        int top,
        VectorSearchOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TVector : notnull
    {
        var results = _inner.SearchAsync(vector, top, options, cancellationToken);

        if (!_recorder.IsEnabled || RunesvanerRequestContext.IsActive)
        {
            await foreach (var result in results.ConfigureAwait(false))
            {
                yield return result;
            }

            yield break;
        }

        var tags = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vector.collection"] = _collectionName,
            ["vector.top_k"] = top.ToString(CultureInfo.InvariantCulture)
        };

        if (options?.Skip is { } skip)
        {
            tags["vector.skip"] = skip.ToString(CultureInfo.InvariantCulture);
        }

        if (options?.ScoreThreshold is { } threshold)
        {
            tags["vector.score_threshold"] = threshold.ToString(CultureInfo.InvariantCulture);
        }

        if (options?.VectorProperty is { } vectorProperty)
        {
            tags["vector.property"] = vectorProperty.Body is MemberExpression member
                ? member.Member.Name
                : vectorProperty.ToString();
        }

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.VectorSearch,
            _provider,
            null,
            null,
            tags));
        using var context = RunesvanerRequestContext.Begin(request);

        var started = Stopwatch.GetTimestamp();
        var count = 0;

        await using var enumerator = results.GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            VectorSearchResult<TRecord> result;

            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    break;
                }

                result = enumerator.Current;
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

            count++;
            yield return result;
        }

        var elapsed = Microseconds(Stopwatch.GetElapsedTime(started));

        request.RecordSegment(new RunesvanerSegment(
            SegmentKind.VectorSearch,
            0,
            elapsed,
            false,
            SegmentMeasurement.Stopwatch));

        request.Complete(new RunesvanerRequestResult(
            RequestStatus.Ok,
            elapsed,
            null));
    }

    /// <summary>Delegates service resolution to the inner collection.</summary>
    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);

    private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
}