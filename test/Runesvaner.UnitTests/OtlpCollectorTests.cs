using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenTelemetry.Exporter;
using Runesvaner;
using Runesvaner.Exporters.OpenTelemetry;

namespace Runesvaner.UnitTests;

public sealed class OtlpCollectorTests
{
    [Fact]
    public async Task Exporter_SendsTracesToHttpCollectorWithGenAiFields()
    {
        await using var collector = new OtlpCollector();

        await using var exporter = RunesvanerOpenTelemetryExporter.Create(options =>
        {
            options.Endpoint = new Uri($"http://127.0.0.1:{collector.Port}/v1/traces");
            options.Protocol = OtlpExportProtocol.HttpProtobuf;
        });

        exporter.Export(CreateRecord());

        await exporter.DisposeAsync();

        var body = await collector.TraceBody.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        Assert.NotEmpty(body);

        var payload = Encoding.UTF8.GetString(body);

        Assert.Contains("gen_ai.operation.name", payload, StringComparison.Ordinal);
        Assert.Contains("gen_ai.usage.input_tokens", payload, StringComparison.Ordinal);
        Assert.Contains("gen_ai.usage.output_tokens", payload, StringComparison.Ordinal);
        Assert.Contains("runesvaner.request_id", payload, StringComparison.Ordinal);
        Assert.Contains("runesvaner.segment.kind", payload, StringComparison.Ordinal);
        Assert.Contains("otel-1", payload, StringComparison.Ordinal);
        Assert.Contains("chat", payload, StringComparison.Ordinal);
    }

    private static RunesvanerRequestRecord CreateRecord() => new(
        "runesvaner.request.v1",
        "otel-1",
        "Chat",
        "openai",
        "gpt-5",
        "gpt-5-2026",
        DateTimeOffset.UtcNow,
        1_000,
        500,
        "Ok",
        new RunesvanerUsage(10, 5, 4, null, UsageSource.Provider),
        [
            new RunesvanerSegment(SegmentKind.Generation, 0, 1_000, false, SegmentMeasurement.Stopwatch)
        ],
        new Dictionary<string, string> { ["feature"] = "test" },
        new CostBreakdown("USD", 0.001m, 0.0001m, 0.002m, 0m, 0.0031m, CostStatus.Known, "test-1"),
        new RunesvanerStreamStats(500, null, 2, 100, 120, true, 25.0, false, false, "stop"));

    private sealed class OtlpCollector : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private readonly TaskCompletionSource<byte[]> _traces =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public OtlpCollector()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _loop = Task.Run(LoopAsync);
        }

        public int Port { get; }

        public Task<byte[]> TraceBody => _traces.Task;

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();

            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Test teardown.
            }

            _stop.Dispose();
        }

        private async Task LoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await _listener.AcceptTcpClientAsync(_stop.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleAsync(client));
            }
        }

        private async Task HandleAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var (path, body) = await ReadRequestAsync(stream).ConfigureAwait(false);

                if (path.Contains("/v1/traces", StringComparison.OrdinalIgnoreCase))
                {
                    _traces.TrySetResult(body);
                }

                const string response =
                    "HTTP/1.1 200 OK\r\nContent-Type: application/x-protobuf\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                await stream
                    .WriteAsync(Encoding.ASCII.GetBytes(response))
                    .ConfigureAwait(false);
            }
        }

        private static async Task<(string Path, byte[] Body)> ReadRequestAsync(Stream stream)
        {
            var buffer = new byte[8_192];
            var total = 0;
            string headers;
            int headerEnd;

            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total)).ConfigureAwait(false);
                if (read == 0)
                {
                    return ("", []);
                }

                total += read;
                headers = Encoding.ASCII.GetString(buffer, 0, total);
                headerEnd = headers.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                if (headerEnd >= 0)
                {
                    break;
                }

                if (total >= buffer.Length)
                {
                    throw new InvalidOperationException("HTTP headers exceed the test buffer.");
                }
            }

            var firstLine = headers[..headers.IndexOf("\r\n", StringComparison.Ordinal)];
            var path = firstLine.Split(' ')[1];
            var contentLength = ParseContentLength(headers);
            var body = new byte[contentLength];
            var bodyOffset = headerEnd + 4;
            var available = Math.Min(contentLength, total - bodyOffset);
            Array.Copy(buffer, bodyOffset, body, 0, available);

            while (available < contentLength)
            {
                var read = await stream
                    .ReadAsync(body.AsMemory(available))
                    .ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                available += read;
            }

            return (path, body);
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