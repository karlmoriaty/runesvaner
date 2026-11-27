using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Runesvaner;
using Runesvaner.SemanticKernel;

var store = new InMemoryRunesvanerStore();
var recorder = new RunesvanerRecorder(
    store,
    new RunesvanerOptions { Provider = "semantic-kernel" });
var service = new RunesvanerChatCompletionService(
    new FakeChatCompletionService(),
    recorder,
    "semantic-kernel");

Console.WriteLine("Semantic Kernel sample: one recorded chat completion.");

var response = await service.GetChatMessageContentsAsync(new ChatHistory());

Console.WriteLine($"response: {response[0].Content}");

var record = (await store.ReadRequestsAsync(null, 1))[0];

Console.WriteLine($"operation: {record.Operation}");
Console.WriteLine($"provider: {record.Provider}");
Console.WriteLine($"segments: {string.Join(", ", record.Segments.Select(segment => segment.Kind))}");
Console.WriteLine($"status: {record.Status}");

internal sealed class FakeChatCompletionService : IChatCompletionService
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
            new ChatMessageContent(AuthorRole.Assistant, "Hello from Semantic Kernel.")
            {
                ModelId = "sk-demo"
            }
        ]);

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new StreamingChatMessageContent(AuthorRole.Assistant, "Hello ");
        yield return new StreamingChatMessageContent(AuthorRole.Assistant, "from SK.");
    }
}