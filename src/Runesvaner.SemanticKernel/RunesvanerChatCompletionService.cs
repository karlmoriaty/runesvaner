using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Runesvaner.SemanticKernel;

/// <summary>Semantic Kernel chat completion decorator that records latency and streaming signals.</summary>
public sealed class RunesvanerChatCompletionService : IChatCompletionService
{
    private readonly IChatCompletionService _inner;
    private readonly IRunesvanerRecorder _recorder;
    private readonly string? _provider;

    /// <summary>Creates a recording decorator around a Semantic Kernel chat completion service.</summary>
    public RunesvanerChatCompletionService(
        IChatCompletionService inner,
        IRunesvanerRecorder recorder,
        string? provider = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _provider = provider;
    }

    /// <summary>Gets the attributes reported by the inner service.</summary>
    public IReadOnlyDictionary<string, object?> Attributes => _inner.Attributes;

    /// <summary>Sends a non-streaming chat request and records it.</summary>
    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (!_recorder.IsEnabled || RunesvanerRequestContext.IsActive)
        {
            return await _inner
                .GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken)
                .ConfigureAwait(false);
        }

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            _provider,
            executionSettings?.ModelId,
            null));
        using var context = RunesvanerRequestContext.Begin(request);

        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await _inner
                .GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken)
                .ConfigureAwait(false);

            var elapsed = Stopwatch.GetElapsedTime(started);
            var durationUs = Microseconds(elapsed);

            request.RecordSegment(new RunesvanerSegment(
                SegmentKind.Generation,
                0,
                durationUs,
                false,
                SegmentMeasurement.Stopwatch));

            request.Complete(new RunesvanerRequestResult(
                RequestStatus.Ok,
                durationUs,
                null,
                ResponseModel: response.FirstOrDefault()?.ModelId));

            return response;
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

    /// <summary>Streams a chat response and records TTFT and chunk counts.</summary>
    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_recorder.IsEnabled || RunesvanerRequestContext.IsActive)
        {
            await foreach (var update in _inner
                               .GetStreamingChatMessageContentsAsync(
                                   chatHistory,
                                   executionSettings,
                                   kernel,
                                   cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return update;
            }

            yield break;
        }

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            _provider,
            executionSettings?.ModelId,
            null));
        using var context = RunesvanerRequestContext.Begin(request);

        var started = Stopwatch.GetTimestamp();
        long? ttftUs = null;
        var chunkCount = 0;

        await using var enumerator = _inner
            .GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            StreamingChatMessageContent update;

            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    break;
                }

                update = enumerator.Current;
            }
            catch (OperationCanceledException)
            {
                request.Complete(new RunesvanerRequestResult(
                    RequestStatus.Cancelled,
                    Microseconds(Stopwatch.GetElapsedTime(started)),
                    ttftUs,
                    Stream: BuildStats(ttftUs, chunkCount, cancelled: true, interrupted: false)));
                throw;
            }
            catch (Exception)
            {
                request.Complete(new RunesvanerRequestResult(
                    RequestStatus.Error,
                    Microseconds(Stopwatch.GetElapsedTime(started)),
                    ttftUs,
                    Stream: BuildStats(ttftUs, chunkCount, cancelled: false, interrupted: true)));
                throw;
            }

            chunkCount++;

            if (!string.IsNullOrEmpty(update.Content))
            {
                var nowUs = Microseconds(Stopwatch.GetElapsedTime(started));

                if (ttftUs is null)
                {
                    ttftUs = nowUs;
                    request.RecordSegment(new RunesvanerSegment(
                        SegmentKind.Ttft,
                        0,
                        ttftUs.Value,
                        false,
                        SegmentMeasurement.Stopwatch));
                }
            }

            yield return update;
        }

        request.Complete(new RunesvanerRequestResult(
            RequestStatus.Ok,
            Microseconds(Stopwatch.GetElapsedTime(started)),
            ttftUs,
            Stream: BuildStats(ttftUs, chunkCount, cancelled: false, interrupted: false)));
    }


    private static RunesvanerStreamStats BuildStats(
        long? ttftUs,
        int chunkCount,
        bool cancelled,
        bool interrupted)
        => new(
            ttftUs,
            null,
            chunkCount,
            null,
            null,
            ItlEstimated: true,
            TokensPerSecond: null,
            cancelled,
            interrupted,
            FinishReason: null);

    private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
}