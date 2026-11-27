using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.AI;
using OpenAI;
using Runesvaner;
using Runesvaner.Http;
using Runesvaner.MicrosoftExtensionsAI;

const string responseBody =
    """
    {
      "id": "chatcmpl-sample",
      "object": "chat.completion",
      "created": 0,
      "model": "gpt-4o-mini",
      "choices": [
        {
          "index": 0,
          "message": { "role": "assistant", "content": "Hello from the local OpenAI mock." },
          "finish_reason": "stop"
        }
      ],
      "usage": { "prompt_tokens": 100, "completion_tokens": 50, "total_tokens": 150 }
    }
    """;

await using var server = new LocalOpenAiServer(responseBody);
using var httpClient = new HttpClient(new RunesvanerHttpHandler());

var openAi = new OpenAI.Chat.ChatClient(
    "gpt-4o-mini",
    new ApiKeyCredential("test-key"),
    new OpenAIClientOptions
    {
        Endpoint = new Uri($"http://127.0.0.1:{server.Port}/"),
        Transport = new HttpClientPipelineTransport(httpClient)
    });

var store = new InMemoryRunesvanerStore();
var recorder = new RunesvanerRecorder(
    store,
    new RunesvanerOptions { Provider = "openai" });
var client = new RunesvanerChatClient(openAi.AsIChatClient(), recorder);

Console.WriteLine("OpenAI protocol sample: local mock, no API key required.");

var response = await client.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "Hello")]);

Console.WriteLine($"response: {response.Text}");

var record = (await store.ReadRequestsAsync(null, 1))[0];

Console.WriteLine($"usage: input={record.Usage?.InputTokens}, output={record.Usage?.OutputTokens}");
Console.WriteLine($"cost: {record.Cost?.Status} (OpenAI prices are not guessed)");
Console.WriteLine($"segments: {string.Join(", ", record.Segments.Select(segment => segment.Kind))}");
Console.WriteLine($"status: {record.Status}");

internal sealed class LocalOpenAiServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _server;
    private readonly string _body;

    public LocalOpenAiServer(string body)
    {
        _body = body;
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
            // The sample owns the lifecycle; a closed connection after the response is expected.
        }
    }

    private async Task ServeAsync()
    {
        using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
        using var stream = client.GetStream();

        var buffer = new byte[8_192];
        var total = 0;
        var contentLength = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total)).ConfigureAwait(false);
            if (read == 0)
            {
                return;
            }

            total += read;
            var headers = Encoding.ASCII.GetString(buffer, 0, total);

            if (headers.Contains("\r\n\r\n", StringComparison.Ordinal))
            {
                contentLength = ParseContentLength(headers);
                break;
            }
        }

        var headerEnd = Encoding.ASCII.GetString(buffer, 0, total)
            .IndexOf("\r\n\r\n", StringComparison.Ordinal);
        var bodyRead = Math.Max(0, total - (headerEnd + 4));

        while (bodyRead < contentLength)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0)).ConfigureAwait(false);
            if (read == 0)
            {
                return;
            }

            bodyRead += read;
        }

        var bodyLength = Encoding.UTF8.GetByteCount(_body);
        var response =
            $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bodyLength}\r\nConnection: close\r\n\r\n{_body}";
        await stream.WriteAsync(Encoding.UTF8.GetBytes(response)).ConfigureAwait(false);
    }

    private static int ParseContentLength(string headers)
    {
        foreach (var line in headers.Split("\r\n"))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(line[(line.IndexOf(':') + 1)..].Trim(), out var value)
                    ? value
                    : 0;
            }
        }

        return 0;
    }
}