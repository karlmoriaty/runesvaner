using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class StreamingChatClientTests
{
    [Fact]
    public async Task Streaming_PreservesOrderAndRecordsTtft()
    {
        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(new StreamingChatClient(), recorder);

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(
                           new[] { new ChatMessage(ChatRole.User, "hi") },
                           cancellationToken: TestContext.Current.CancellationToken))
        {
            updates.Add(update);
        }

        Assert.Equal(2, updates.Count);
        Assert.Equal("first", updates[0].Text);
        Assert.Equal("second", updates[1].Text);

        var record = Assert.Single(store.Records);
        Assert.Equal("Ok", record.Status);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Ttft);
        Assert.NotNull(record.Stream);
        Assert.Equal(2, record.Stream!.ChunkCount);
        Assert.NotNull(record.Stream.TtftUs);
        Assert.True(record.Stream.ItlEstimated);
        Assert.True(record.Usage?.OutputTokens == 4);
        Assert.NotNull(record.Stream.TokensPerSecond);
        Assert.Equal("stop", record.Stream.FinishReason);
    }

    [Fact]
    public async Task Streaming_Cancellation_RecordsCancelled()
    {
        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(new SlowStreamingChatClient(), recorder);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync(
                               new[] { new ChatMessage(ChatRole.User, "hi") },
                               cancellationToken: cancellation.Token))
            {
            }
        });

        var record = Assert.Single(store.Records);
        Assert.Equal("Cancelled", record.Status);
        Assert.True(record.Stream?.Cancelled);
        Assert.False(record.Stream?.Interrupted);
    }

    [Fact]
    public async Task Streaming_Error_PropagatesAndRecordsError()
    {
        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(new ThrowingStreamingChatClient(), recorder);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync(
                               new[] { new ChatMessage(ChatRole.User, "hi") },
                               cancellationToken: TestContext.Current.CancellationToken))
            {
            }
        });

        var record = Assert.Single(store.Records);
        Assert.Equal("Error", record.Status);
        Assert.True(record.Stream?.Interrupted);
        Assert.False(record.Stream?.Cancelled);
    }

    private sealed class InMemoryStore : IRunesvanerStore
    {
        public List<RunesvanerRequestRecord> Records { get; } = new();

        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return ValueTask.CompletedTask;
        }

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class StreamingChatClient : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "first");
            await Task.Delay(5, cancellationToken);
            var second = new ChatResponseUpdate(ChatRole.Assistant, "second")
            {
                FinishReason = ChatFinishReason.Stop
            };
            second.Contents.Add(new UsageContent(new UsageDetails
            {
                InputTokenCount = 3,
                OutputTokenCount = 4,
                TotalTokenCount = 7
            }));
            yield return second;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class SlowStreamingChatClient : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "first");
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, "late");
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingStreamingChatClient : IChatClient
    {
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "first");
            throw new InvalidOperationException("stream failed");
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}