using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Runesvaner.MicrosoftExtensionsAI;

public sealed class RunesvanerChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly IRunesvanerRecorder _recorder;

    public RunesvanerChatClient(IChatClient inner, IRunesvanerRecorder recorder)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_recorder.IsEnabled)
        {
            return await _inner.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            null,
            options?.ModelId,
            null));

        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await _inner.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(started);

            if (response.Usage is { } usage)
            {
                request.RecordUsage(new RunesvanerUsage(
                    usage.InputTokenCount,
                    usage.OutputTokenCount,
                    usage.CachedInputTokenCount,
                    usage.ReasoningTokenCount,
                    UsageSource.Provider));
            }

            request.RecordSegment(new RunesvanerSegment(
                SegmentKind.Generation,
                0,
                Microseconds(elapsed),
                false,
                SegmentMeasurement.Stopwatch));

            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, Microseconds(elapsed), null, ResponseModel: response.ModelId));
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

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_recorder.IsEnabled)
        {
            await foreach (var update in _inner.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }

            yield break;
        }

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            null,
            options?.ModelId,
            null));

        var started = Stopwatch.GetTimestamp();
        long? ttftUs = null;

        await using var enumerator = _inner
            .GetStreamingResponseAsync(messages, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatResponseUpdate update;

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
                    ttftUs));
                throw;
            }
            catch (Exception)
            {
                request.Complete(new RunesvanerRequestResult(
                    RequestStatus.Error,
                    Microseconds(Stopwatch.GetElapsedTime(started)),
                    ttftUs));
                throw;
            }

            if (ttftUs is null && !string.IsNullOrEmpty(update.Text))
            {
                ttftUs = Microseconds(Stopwatch.GetElapsedTime(started));
                request.RecordSegment(new RunesvanerSegment(
                    SegmentKind.Ttft,
                    0,
                    ttftUs.Value,
                    false,
                    SegmentMeasurement.Stopwatch));
            }

            yield return update;
        }

        request.Complete(new RunesvanerRequestResult(
            RequestStatus.Ok,
            Microseconds(Stopwatch.GetElapsedTime(started)),
            ttftUs));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);

    public void Dispose()
        => _inner.Dispose();

    private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
}