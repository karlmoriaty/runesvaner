using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;

using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class OpenAiProtocolTests
{
    private const string NonStreamingBody =
        """
        {
          "id": "chatcmpl-test",
          "object": "chat.completion",
          "created": 0,
          "model": "gpt-4o-mini",
          "choices": [
            {
              "index": 0,
              "message": { "role": "assistant", "content": "hello" },
              "finish_reason": "stop"
            }
          ],
          "usage": { "prompt_tokens": 100, "completion_tokens": 50, "total_tokens": 150 }
        }
        """;

    private const string StreamingBody =
        """
        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{"role":"assistant","content":"hel"},"finish_reason":null}]}

        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{"content":"lo"},"finish_reason":null}]}

        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{},"finish_reason":"stop"}],"usage":{"prompt_tokens":100,"completion_tokens":50,"total_tokens":150}}

        data: [DONE]

        """;

    [Fact]
    public async Task NonStreaming_RecordsUsageAndKeepsUnknownCostExplicit()
    {
        var handler = new MockHandler(NonStreamingBody, "application/json");
        using var httpClient = new HttpClient(handler);
        var inner = CreateChatClient(httpClient);
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(
            store,
            new RunesvanerOptions { Provider = "openai" });
        var client = new RunesvanerChatClient(inner, recorder);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("hello", response.Text);

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(100L, record.Usage?.InputTokens);
        Assert.Equal(50L, record.Usage?.OutputTokens);
        Assert.Equal(CostStatus.Unknown, record.Cost?.Status);
        Assert.Null(record.Cost?.Total);
        Assert.Equal("gpt-4o-mini", record.ResponseModel);
        Assert.Contains("gpt-4o-mini", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Streaming_RecordsUsageAndStreamStats()
    {
        var handler = new MockHandler(StreamingBody, "text/event-stream");
        using var httpClient = new HttpClient(handler);
        var inner = CreateChatClient(httpClient);
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(
            store,
            new RunesvanerOptions { Provider = "openai" });
        var client = new RunesvanerChatClient(inner, recorder);

        var chunks = 0;

        await foreach (var _ in client.GetStreamingResponseAsync(
                           [new ChatMessage(ChatRole.User, "hi")],
                           cancellationToken: TestContext.Current.CancellationToken))
        {
            chunks++;
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.True(chunks >= 2);
        Assert.NotNull(record.Stream);
        Assert.NotNull(record.Stream.TtftUs);
        Assert.Equal(50L, record.Usage?.OutputTokens);
        Assert.NotNull(record.Stream.FinishReason);
        Assert.Contains("stop", record.Stream.FinishReason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"stream\":true", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("include_usage", handler.RequestBody, StringComparison.Ordinal);
    }

    private static IChatClient CreateChatClient(HttpClient httpClient)
    {
        var chatClient = new OpenAI.Chat.ChatClient(
            "gpt-4o-mini",
            new ApiKeyCredential("test-key"),
            new OpenAIClientOptions
            {
                Transport = new HttpClientPipelineTransport(httpClient)
            });

        return chatClient.AsIChatClient();
    }

    private sealed class MockHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly string _contentType;

        public MockHandler(string body, string contentType)
        {
            _body = body;
            _contentType = contentType;
        }

        public string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var content = new StringContent(_body, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue(_contentType);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
        }
    }
}