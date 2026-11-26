using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class FailureInjectionTests
{
    [Fact]
    public async Task StoreFailure_DoesNotBreakBusinessRequest()
    {
        var recorder = new RunesvanerRecorder(new ThrowingStore(), new RunesvanerOptions());
        var client = new RunesvanerChatClient(new FakeChatClient(), recorder);

        var response = await client.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, "hi") },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("ok", response.Text);
    }

    private sealed class ThrowingStore : IRunesvanerStore
    {
        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
            => throw new IOException("store unavailable");

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }

    private sealed class FakeChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

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