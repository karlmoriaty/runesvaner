using System.Runtime.CompilerServices;
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

var chat = new RunesvanerChatClient(new EchoChatClient(), recorder);
var response = await chat.GetResponseAsync(
    new[] { new ChatMessage(ChatRole.User, "AOT smoke") });
Console.WriteLine($"AOT non-streaming ok: {response.Text}");

var chunks = 0;
await foreach (var _ in chat.GetStreamingResponseAsync(
                   new[] { new ChatMessage(ChatRole.User, "AOT streaming") }))
{
    chunks++;
}

Console.WriteLine($"AOT streaming ok: {chunks} chunks");

try
{
    var failing = new RunesvanerChatClient(new ThrowingChatClient(), recorder);
    await failing.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "fail") });
}
catch (InvalidOperationException)
{
    Console.WriteLine("AOT error path ok");
}

await recorder.FlushAsync();
Console.WriteLine("AOT sample completed.");

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

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, "AOT ");
        await Task.Delay(1, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, "stream");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

internal sealed class ThrowingChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("intentional AOT error path");

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("intentional AOT error path");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}