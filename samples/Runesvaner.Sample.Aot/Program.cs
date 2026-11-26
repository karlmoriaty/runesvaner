using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;
using Runesvaner.Storage.Jsonl;

var options = new RunesvanerOptions
{
    ApplicationName = "sample-aot",
    StorageMode = StorageMode.Jsonl,
    StoragePath = ".runesvaner"
};

var store = new JsonlRunesvanerStore(options.StoragePath);
var recorder = new RunesvanerRecorder(store, options);
var client = new RunesvanerChatClient(new EchoChatClient(), recorder);

var response = await client.GetResponseAsync(
    new[] { new ChatMessage(ChatRole.User, "AOT smoke") });

Console.WriteLine($"AOT sample ok: {response.Text}");
await recorder.FlushAsync();

internal sealed class EchoChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = messages.LastOrDefault()?.Text ?? "";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Echo: {prompt}"))
        {
            ModelId = "echo-1",
            Usage = new UsageDetails
            {
                InputTokenCount = 2,
                OutputTokenCount = 2,
                TotalTokenCount = 4
            }
        });
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