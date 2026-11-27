using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Runesvaner;
using Runesvaner.Http;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class TtftAccuracyTests
{
    private const int FirstChunkDelayMilliseconds = 250;
    private const int AccuracyBudgetMicroseconds = 10_000;

    private const string FirstChunk =
        """
        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{"role":"assistant","content":"first"},"finish_reason":null}]}

        """;

    private const string RemainingChunks =
        """
        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{"content":" second"},"finish_reason":null}]}

        data: {"id":"chatcmpl-test","object":"chat.completion.chunk","created":0,"model":"gpt-4o-mini","choices":[{"index":0,"delta":{},"finish_reason":"stop"}],"usage":{"prompt_tokens":10,"completion_tokens":2,"total_tokens":12}}

        data: [DONE]

        """;

    [Fact]
    public async Task StreamingTtft_MatchesClientObservationWithinTenMilliseconds()
    {
        await using var server = new ChunkedSseServer(
            FirstChunkDelayMilliseconds,
            FirstChunk,
            RemainingChunks);

        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var httpClient = new HttpClient(new RunesvanerHttpHandler());
        var timing = new TimingChatClient(CreateOpenAiClient(httpClient, server.Port));
        var client = new RunesvanerChatClient(timing, recorder);

        var started = Stopwatch.GetTimestamp();

        await foreach (var _ in client.GetStreamingResponseAsync(
                           [new ChatMessage(ChatRole.User, "hi")],
                           cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.NotNull(record.Stream);
        Assert.NotNull(record.Stream.TtftUs);
        Assert.True(timing.FirstTextTimestamp != 0, "The client did not observe a text chunk.");

        var observedMicroseconds =
            (timing.FirstTextTimestamp - started) * 1_000_000 / Stopwatch.Frequency;
        var errorMicroseconds = Math.Abs(record.Stream.TtftUs.Value - observedMicroseconds);

        Assert.True(
            errorMicroseconds <= AccuracyBudgetMicroseconds,
            $"TTFT error {errorMicroseconds} us exceeds {AccuracyBudgetMicroseconds} us " +
            $"(recorded={record.Stream.TtftUs.Value}, observed={observedMicroseconds}).");
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

    private sealed class TimingChatClient : IChatClient
    {
        private readonly IChatClient _inner;
        private long _firstTextTimestamp;

        public TimingChatClient(IChatClient inner) => _inner = inner;

        public long FirstTextTimestamp => Volatile.Read(ref _firstTextTimestamp);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => _inner.GetResponseAsync(messages, options, cancellationToken);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var update in _inner
                               .GetStreamingResponseAsync(messages, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                if (!string.IsNullOrEmpty(update.Text) &&
                    Volatile.Read(ref _firstTextTimestamp) == 0)
                {
                    Volatile.Write(ref _firstTextTimestamp, Stopwatch.GetTimestamp());
                }

                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => _inner.GetService(serviceType, serviceKey);

        public void Dispose()
            => _inner.Dispose();
    }

    private sealed class ChunkedSseServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task _server;
        private readonly int _delayMilliseconds;
        private readonly string _firstChunk;
        private readonly string _remainingChunks;

        public ChunkedSseServer(
            int delayMilliseconds,
            string firstChunk,
            string remainingChunks)
        {
            _delayMilliseconds = delayMilliseconds;
            _firstChunk = firstChunk;
            _remainingChunks = remainingChunks;

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _server = Task.Run(ServeAsync);
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

        private async Task ServeAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            using var stream = client.GetStream();

            var buffer = new byte[8_192];
            var total = 0;

            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total)).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                total += read;
                var text = Encoding.ASCII.GetString(buffer, 0, total);

                if (text.Contains("\r\n\r\n", StringComparison.Ordinal))
                {
                    break;
                }
            }

            var first = _firstChunk.TrimEnd() + "\n\n";
            var rest = _remainingChunks.TrimEnd() + "\n\n";
            var contentLength = Encoding.UTF8.GetByteCount(first) + Encoding.UTF8.GetByteCount(rest);

            var headers =
                $"HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nContent-Length: {contentLength}\r\nConnection: close\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(headers)).ConfigureAwait(false);

            await Task.Delay(_delayMilliseconds).ConfigureAwait(false);

            await stream
                .WriteAsync(Encoding.UTF8.GetBytes(first))
                .ConfigureAwait(false);

            await stream
                .WriteAsync(Encoding.UTF8.GetBytes(rest))
                .ConfigureAwait(false);
        }
    }
}