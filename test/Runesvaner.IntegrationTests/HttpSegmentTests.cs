using System.ClientModel;
using System.ClientModel.Primitives;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Runesvaner;
using Runesvaner.Http;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class HttpSegmentTests
{
    private const string StreamingBody =
        """
        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{"role":"assistant","content":"hel"},"finish_reason":null}]}

        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{"content":"lo"},"finish_reason":null}]}

        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{},"finish_reason":"stop"}],"usage":{"prompt_tokens":100,"completion_tokens":50,"total_tokens":150}}

        data: [DONE]

        """;

    [Fact]
    public async Task Post_RecordsQueueConnectUploadProviderWaitAndGeneration()
    {
        await using var server = new LocalServer(40);
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());

        using var request = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "test",
            "model-1",
            null));

        using (RunesvanerRequestContext.Begin(request))
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);

            using var client = new HttpClient(new RunesvanerHttpHandler());
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"http://127.0.0.1:{server.Port}/")
            {
                Content = new StringContent("hello")
            };
            using var response = await client.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                TestContext.Current.CancellationToken);

            Assert.Equal(
                "ok",
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Queue);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Connect);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Upload);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.ProviderQueue);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Generation);
        Assert.All(record.Segments, segment => Assert.False(segment.Estimated));
        Assert.All(record.Segments, segment => Assert.True(segment.DurationUs >= 1));
    }

    [Fact]
    public async Task ChatClient_CombinesHttpSegmentsWithMeaiTtft()
    {
        await using var server = new LocalServer(
            0,
            StreamingBody,
            "text/event-stream");

        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var httpClient = new HttpClient(new RunesvanerHttpHandler());
        var client = new RunesvanerChatClient(
            CreateOpenAiClient(httpClient, server.Port),
            recorder);

        await foreach (var _ in client.GetStreamingResponseAsync(
                           [new ChatMessage(ChatRole.User, "hi")],
                           cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Connect);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Upload);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.ProviderQueue);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Generation);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Ttft);
        Assert.Equal(50L, record.Usage?.OutputTokens);
    }

    private static IChatClient CreateOpenAiClient(HttpClient httpClient, int port)
    {
        var chatClient = new OpenAI.Chat.ChatClient(
            "gpt-4o-mini",
            new ApiKeyCredential("test-key"),
            new OpenAIClientOptions
            {
                Endpoint = new Uri($"http://127.0.0.1:{port}/"),
                Transport = new HttpClientPipelineTransport(httpClient)
            });

        return chatClient.AsIChatClient();
    }

    private sealed class LocalServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task _server;
        private readonly string _body;
        private readonly string _contentType;

        public LocalServer(
            int responseDelayMilliseconds,
            string? body = null,
            string? contentType = null)
        {
            _body = body ?? "ok";
            _contentType = contentType ?? "text/plain";

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _server = Task.Run(() => ServeAsync(responseDelayMilliseconds));
        }

        public int Port { get; }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();

            try
            {
                await _server.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The test owns the lifecycle; a broken pipe after assertions is not relevant.
            }
        }

        private async Task ServeAsync(int responseDelayMilliseconds)
        {
            using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            using var stream = client.GetStream();

            var buffer = new byte[8_192];
            var total = 0;
            var headerEnd = -1;
            var contentLength = 0;

            while (headerEnd < 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total)).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                total += read;
                var text = Encoding.ASCII.GetString(buffer, 0, total);
                headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                if (headerEnd >= 0)
                {
                    contentLength = ParseContentLength(text);
                }
            }

            var bodyRead = total - (headerEnd + 4);

            while (bodyRead < contentLength)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0)).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                bodyRead += read;
            }

            await Task.Delay(responseDelayMilliseconds).ConfigureAwait(false);

            var bodyLength = Encoding.UTF8.GetByteCount(_body);
            var response =
                $"HTTP/1.1 200 OK\r\nContent-Type: {_contentType}\r\nContent-Length: {bodyLength}\r\nConnection: close\r\n\r\n{_body}";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(response)).ConfigureAwait(false);
        }

        private static int ParseContentLength(string headers)
        {
            foreach (var line in headers.Split("\r\n"))
            {
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                {
                    return int.Parse(
                        line[(line.IndexOf(':') + 1)..].Trim(),
                        CultureInfo.InvariantCulture);
                }
            }

            return 0;
        }
    }
}