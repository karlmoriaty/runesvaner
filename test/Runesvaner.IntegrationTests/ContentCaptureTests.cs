using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class ContentCaptureTests
{
    [Fact]
    public async Task None_DoesNotStoreContent()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(new StaticChatClient("response"), recorder);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "prompt")],
            cancellationToken: TestContext.Current.CancellationToken);

        var record = Assert.Single(await ReadAllAsync(store));
        Assert.Null(record.Content);
    }

    [Fact]
    public async Task Metadata_StoresLengthsAndHashesOnly()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(
            new StaticChatClient("response"),
            recorder,
            ContentCaptureMode.Metadata);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "prompt")],
            cancellationToken: TestContext.Current.CancellationToken);

        var content = Assert.Single(await ReadAllAsync(store)).Content;

        Assert.NotNull(content);
        Assert.Equal("prompt".Length, content.PromptLength);
        Assert.Equal("response".Length, content.ResponseLength);
        Assert.NotNull(content.PromptHash);
        Assert.NotNull(content.ResponseHash);
        Assert.Null(content.Prompt);
        Assert.Null(content.Response);
    }

    [Fact]
    public async Task Full_StoresOriginalText()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(
            new StaticChatClient("response text"),
            recorder,
            ContentCaptureMode.Full);

        await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "prompt text")],
            cancellationToken: TestContext.Current.CancellationToken);

        var content = Assert.Single(await ReadAllAsync(store)).Content;

        Assert.NotNull(content);
        Assert.Equal("prompt text", content.Prompt);
        Assert.Equal("response text", content.Response);
        Assert.NotNull(content.PromptHash);
        Assert.NotNull(content.ResponseHash);
    }

    [Fact]
    public async Task StreamingFull_AccumulatesResponseText()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(
            new StaticChatClient("ignored", "hello ", "world"),
            recorder,
            ContentCaptureMode.Full);

        await foreach (var _ in client.GetStreamingResponseAsync(
                           [new ChatMessage(ChatRole.User, "stream prompt")],
                           cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        var content = Assert.Single(await ReadAllAsync(store)).Content;

        Assert.NotNull(content);
        Assert.Equal("stream prompt", content.Prompt);
        Assert.Equal("hello world", content.Response);
    }

    private static async Task<IReadOnlyList<RunesvanerRequestRecord>> ReadAllAsync(
        InMemoryRunesvanerStore store)
        => await store.ReadRequestsAsync(null, 10, TestContext.Current.CancellationToken);

    private sealed class StaticChatClient : IChatClient
    {
        private readonly string _response;
        private readonly string[] _chunks;

        public StaticChatClient(string response, params string[] chunks)
        {
            _response = response;
            _chunks = chunks;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _response)));

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            foreach (var chunk in _chunks)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}