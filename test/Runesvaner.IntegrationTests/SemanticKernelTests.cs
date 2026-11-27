using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;
using Runesvaner.SemanticKernel;

namespace Runesvaner.IntegrationTests;

public sealed class SemanticKernelTests
{
    [Fact]
    public async Task NonStreaming_RecordsOneRequest()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(
            store,
            new RunesvanerOptions { Provider = "sk" });
        var service = new RunesvanerChatCompletionService(
            new FakeChatCompletionService(),
            recorder,
            "sk");

        var response = await service.GetChatMessageContentsAsync(
            new ChatHistory(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(response);

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal("Chat", record.Operation);
        Assert.Equal("sk", record.Provider);
        Assert.Equal("sk-model", record.ResponseModel);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Generation);
    }

    [Fact]
    public async Task NestedInExistingContext_IsNotRecordedTwice()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var outer = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "outer",
            "outer-model",
            null));

        using (RunesvanerRequestContext.Begin(outer))
        {
            var service = new RunesvanerChatCompletionService(
                new FakeChatCompletionService(),
                recorder,
                "sk");

            await service.GetChatMessageContentsAsync(
                new ChatHistory(),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        outer.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(outer.RequestId, record.RequestId);
        Assert.Equal("outer", record.Provider);
    }

    [Fact]
    public async Task MeaiNestedInsideExistingContext_IsNotRecordedTwice()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var outer = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "outer",
            "outer-model",
            null));

        using (RunesvanerRequestContext.Begin(outer))
        {
            var client = new RunesvanerChatClient(new FakeMeaiClient(), recorder);

            await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "hi")],
                cancellationToken: TestContext.Current.CancellationToken);
        }

        outer.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(outer.RequestId, record.RequestId);
    }

    [Fact]
    public async Task Streaming_RecordsTtftAndChunkCount()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var service = new RunesvanerChatCompletionService(
            new FakeChatCompletionService(),
            recorder,
            "sk");

        await foreach (var _ in service.GetStreamingChatMessageContentsAsync(
                           new ChatHistory(),
                           cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.NotNull(record.Stream);
        Assert.NotNull(record.Stream.TtftUs);
        Assert.Equal(2, record.Stream.ChunkCount);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Ttft);
    }

    private sealed class FakeChatCompletionService : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes { get; } =
            new Dictionary<string, object?>();

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ChatMessageContent>>(
            [
                new ChatMessageContent(AuthorRole.Assistant, "hello")
                {
                    ModelId = "sk-model"
                }
            ]);

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, "hel");
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, "lo");
        }
    }

    private sealed class FakeMeaiClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "meai")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
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