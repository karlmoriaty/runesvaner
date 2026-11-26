using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;
using Runesvaner.Storage.Jsonl;

var options = new RunesvanerOptions
{
    ApplicationName = "sample-console",
    StorageMode = StorageMode.Jsonl,
    StoragePath = ".runesvaner"
};

var store = new JsonlRunesvanerStore(options.StoragePath);
var recorder = new RunesvanerRecorder(store, options);
var client = new RunesvanerChatClient(new EchoChatClient(), recorder);

var response = await client.GetResponseAsync(
    new[] { new ChatMessage(ChatRole.User, "Hello, Runesvaner") });

Console.WriteLine(response.Text);
Console.WriteLine($"Captured to {Path.GetFullPath(options.StoragePath)}");

await recorder.FlushAsync();

internal sealed class EchoChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = messages.LastOrDefault()?.Text ?? "";
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Echo: {prompt}"))
        {
            ModelId = "echo-1",
            Usage = new UsageDetails
            {
                InputTokenCount = 3,
                OutputTokenCount = 4,
                TotalTokenCount = 7
            }
        };

        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The echo sample does not implement streaming.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}