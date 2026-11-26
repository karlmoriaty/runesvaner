using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class ChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_RecordsUsageAndStatus()
    {
        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var client = new RunesvanerChatClient(new FakeChatClient(), recorder);

        var response = await client.GetResponseAsync(
            new[] { new ChatMessage(ChatRole.User, "hi") },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("ok", response.Text);

        var record = Assert.Single(store.Records);
        Assert.Equal("Ok", record.Status);
        Assert.True(record.Usage?.InputTokens == 10);
        Assert.True(record.Usage?.OutputTokens == 5);
        Assert.Equal("Chat", record.Operation);
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

    private sealed class FakeChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))
            {
                Usage = new UsageDetails
                {
                    InputTokenCount = 10,
                    OutputTokenCount = 5,
                    TotalTokenCount = 15
                }
            };

            return Task.FromResult(response);
        }

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