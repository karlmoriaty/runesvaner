using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace Runesvaner.MicrosoftExtensionsAI;

public sealed class RunesvanerChatClient : IChatClient
{
    private readonly IChatClient _inner;
    private readonly IRunesvanerRecorder _recorder;
    private readonly ContentCaptureMode _contentCapture;

    public RunesvanerChatClient(
        IChatClient inner,
        IRunesvanerRecorder recorder,
        ContentCaptureMode contentCapture = ContentCaptureMode.None)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _contentCapture = contentCapture;
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

        var capture = _contentCapture != ContentCaptureMode.None;
        var requestMessages = capture ? Materialize(messages) : messages;
        var prompt = capture ? PromptText(requestMessages) : null;

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            null,
            options?.ModelId,
            null));
        using var context = RunesvanerRequestContext.Begin(request);

        var started = Stopwatch.GetTimestamp();

        try
        {
            var response = await _inner.GetResponseAsync(requestMessages, options, cancellationToken).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(started);

            if (prompt is not null)
            {
                request.RecordContent(BuildContent(prompt, response.Text ?? "", _contentCapture));
            }

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
            if (prompt is not null)
            {
                request.RecordContent(BuildContent(prompt, "", _contentCapture));
            }

            request.Complete(new RunesvanerRequestResult(
                RequestStatus.Cancelled,
                Microseconds(Stopwatch.GetElapsedTime(started)),
                null));
            throw;
        }
        catch (Exception)
        {
            if (prompt is not null)
            {
                request.RecordContent(BuildContent(prompt, "", _contentCapture));
            }

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

        var capture = _contentCapture != ContentCaptureMode.None;
        var requestMessages = capture ? Materialize(messages) : messages;
        var prompt = capture ? PromptText(requestMessages) : null;
        var responseBuilder = capture ? new StringBuilder() : null;

        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            null,
            options?.ModelId,
            null));
        using var context = RunesvanerRequestContext.Begin(request);

        var started = Stopwatch.GetTimestamp();
        long? ttftUs = null;
        var chunkCount = 0;
        var textTimestamps = new List<long>(16);
        UsageDetails? usage = null;
        string? finishReason = null;

        await using var enumerator = _inner
            .GetStreamingResponseAsync(requestMessages, options, cancellationToken)
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
                RecordUsage(request, usage);
                RecordCapture(request, prompt, responseBuilder, _contentCapture);
                request.Complete(new RunesvanerRequestResult(
                    RequestStatus.Cancelled,
                    Microseconds(Stopwatch.GetElapsedTime(started)),
                    ttftUs,
                    Stream: BuildStats(
                        ttftUs,
                        textTimestamps,
                        chunkCount,
                        usage,
                        finishReason,
                        cancelled: true,
                        interrupted: false)));
                throw;
            }
            catch (Exception)
            {
                RecordUsage(request, usage);
                RecordCapture(request, prompt, responseBuilder, _contentCapture);
                request.Complete(new RunesvanerRequestResult(
                    RequestStatus.Error,
                    Microseconds(Stopwatch.GetElapsedTime(started)),
                    ttftUs,
                    Stream: BuildStats(
                        ttftUs,
                        textTimestamps,
                        chunkCount,
                        usage,
                        finishReason,
                        cancelled: false,
                        interrupted: true)));
                throw;
            }

            chunkCount++;

            if (!string.IsNullOrEmpty(update.Text))
            {
                responseBuilder?.Append(update.Text);

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

                textTimestamps.Add(nowUs);
            }

            if (update.FinishReason is { } reason)
            {
                finishReason = reason.ToString();
            }

            foreach (var content in update.Contents)
            {
                if (content is UsageContent { Details: { } details })
                {
                    usage = details;
                }
            }

            yield return update;
        }

        RecordUsage(request, usage);
        RecordCapture(request, prompt, responseBuilder, _contentCapture);
        request.Complete(new RunesvanerRequestResult(
            RequestStatus.Ok,
            Microseconds(Stopwatch.GetElapsedTime(started)),
            ttftUs,
            ResponseModel: options?.ModelId,
            Stream: BuildStats(
                ttftUs,
                textTimestamps,
                chunkCount,
                usage,
                finishReason,
                cancelled: false,
                interrupted: false)));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => _inner.GetService(serviceType, serviceKey);

    public void Dispose()
        => _inner.Dispose();

    private static IReadOnlyList<ChatMessage> Materialize(IEnumerable<ChatMessage> messages)
        => messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

    private static string PromptText(IEnumerable<ChatMessage> messages)
        => string.Join("\n", messages.Select(message => message.Text ?? string.Empty));

    private static RunesvanerContentCapture BuildContent(
        string prompt,
        string response,
        ContentCaptureMode mode)
        => new(
            prompt.Length,
            Hash(prompt),
            mode == ContentCaptureMode.Full ? prompt : null,
            response.Length,
            Hash(response),
            mode == ContentCaptureMode.Full ? response : null);

    private static string Hash(string text)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];

    private static void RecordCapture(
        IRunesvanerRequest request,
        string? prompt,
        StringBuilder? response,
        ContentCaptureMode mode)
    {
        if (mode == ContentCaptureMode.None || prompt is null)
        {
            return;
        }

        request.RecordContent(BuildContent(prompt, response?.ToString() ?? "", mode));
    }

    private static void RecordUsage(IRunesvanerRequest request, UsageDetails? usage)
    {
        if (usage is null)
        {
            return;
        }

        request.RecordUsage(new RunesvanerUsage(
            usage.InputTokenCount,
            usage.OutputTokenCount,
            usage.CachedInputTokenCount,
            usage.ReasoningTokenCount,
            UsageSource.Provider));
    }

    private static RunesvanerStreamStats BuildStats(
        long? ttftUs,
        List<long> textTimestamps,
        int chunkCount,
        UsageDetails? usage,
        string? finishReason,
        bool cancelled,
        bool interrupted)
    {
        var (p50, p95) = Intervals(textTimestamps);
        double? tokensPerSecond = null;

        if (usage?.OutputTokenCount is { } outputTokens && textTimestamps.Count >= 2)
        {
            var seconds = (textTimestamps[^1] - textTimestamps[0]) / 1_000_000.0;
            if (seconds > 0)
            {
                tokensPerSecond = outputTokens / seconds;
            }
        }

        return new RunesvanerStreamStats(
            ttftUs,
            null,
            chunkCount,
            p50,
            p95,
            ItlEstimated: true,
            tokensPerSecond,
            cancelled,
            interrupted,
            finishReason);
    }

    private static (long? P50, long? P95) Intervals(List<long> timestamps)
    {
        if (timestamps.Count < 2)
        {
            return (null, null);
        }

        var gaps = new long[timestamps.Count - 1];
        for (var i = 0; i < gaps.Length; i++)
        {
            gaps[i] = timestamps[i + 1] - timestamps[i];
        }

        Array.Sort(gaps);
        return (Percentile(gaps, 0.50), Percentile(gaps, 0.95));
    }

    private static long Percentile(long[] sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
}