using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

var store = new InMemoryRunesvanerStore();
var recorder = new RunesvanerRecorder(
    store,
    new RunesvanerOptions { Provider = "anthropic" });
var client = new RunesvanerChatClient(new StreamingClient(), recorder);

Console.WriteLine("Streaming sample: three chunks with 25 ms spacing.");

await foreach (var update in client.GetStreamingResponseAsync(
                   [new ChatMessage(ChatRole.User, "Explain TTFT")],
                   new ChatOptions { ModelId = "claude-sonnet-5.5" }))
{
    Console.Write(update.Text);
}

Console.WriteLine();

var record = (await store.ReadRequestsAsync(null, 1))[0];

Console.WriteLine($"chunks: {record.Stream?.ChunkCount}");
Console.WriteLine($"ttft: {record.Stream?.TtftUs} us");
Console.WriteLine($"itl p95: {record.Stream?.ItlP95Us} us (estimated: {record.Stream?.ItlEstimated})");
Console.WriteLine($"tps: {record.Stream?.TokensPerSecond:F1}");
Console.WriteLine($"cost: {record.Cost?.Total} {record.Cost?.Currency} ({record.Cost?.Status})");

internal sealed class StreamingClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This sample is streaming-only.");

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var chunk in new[] { "Time ", "to first ", "token." })
        {
            await Task.Delay(25, cancellationToken);
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
        }

        yield return new ChatResponseUpdate(
            ChatRole.Assistant,
            [new UsageContent(new UsageDetails
            {
                InputTokenCount = 1_000,
                OutputTokenCount = 120
            })]);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}