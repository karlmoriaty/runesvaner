using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Runesvaner;
using Runesvaner.Http;

namespace Runesvaner.IntegrationTests;

public sealed class TlsProbeTests
{
    [Fact]
    public async Task TlsProbe_RecordsEstimatedTlsSegment()
    {
        await using var server = new TlsServer();
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());

        using var request = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "test",
            "model-1",
            null));

        using (RunesvanerRequestContext.Begin(request))
        {
            var sockets = RunesvanerHttpHandler.CreateSocketsHandler(
                enableTlsProbe: true,
                validateCertificate: (_, _, _, _) => true);

            using var client = new HttpClient(new RunesvanerHttpHandler(sockets));
            var body = await client.GetStringAsync(
                $"https://127.0.0.1:{server.Port}/",
                TestContext.Current.CancellationToken);

            Assert.Equal("ok", body);
        }

        request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        var tls = Assert.Single(
            record.Segments,
            segment => segment.Kind == SegmentKind.Tls);

        Assert.True(tls.Estimated);
        Assert.Equal(SegmentMeasurement.Derived, tls.Measurement);
        Assert.True(tls.DurationUs >= 1);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Connect);
    }

    [Fact]
    public async Task TlsProbe_InvokesUserCertificateValidation()
    {
        await using var server = new TlsServer();
        var called = false;

        var sockets = RunesvanerHttpHandler.CreateSocketsHandler(
            enableTlsProbe: true,
            validateCertificate: (_, _, _, _) =>
            {
                called = true;
                return true;
            });

        using var client = new HttpClient(new RunesvanerHttpHandler(sockets));
        var body = await client.GetStringAsync(
            $"https://127.0.0.1:{server.Port}/",
            TestContext.Current.CancellationToken);

        Assert.Equal("ok", body);
        Assert.True(called);
    }

    [Fact]
    public async Task WithoutTlsProbe_SelfSignedCertificateIsRejected()
    {
        await using var server = new TlsServer();
        using var client = new HttpClient(new RunesvanerHttpHandler());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetStringAsync(
                $"https://127.0.0.1:{server.Port}/",
                TestContext.Current.CancellationToken));
    }

    private sealed class TlsServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly X509Certificate2 _certificate;
        private readonly Task _server;

        public TlsServer()
        {
            _certificate = CreateCertificate();
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
                // The client may reject the certificate; teardown is still valid.
            }

            _certificate.Dispose();
        }

        private async Task ServeAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            await using var ssl = new SslStream(
                client.GetStream(),
                leaveInnerStreamOpen: false);

            await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = _certificate
            }).ConfigureAwait(false);

            var buffer = new byte[8_192];
            var total = 0;

            while (true)
            {
                var read = await ssl.ReadAsync(buffer.AsMemory(total)).ConfigureAwait(false);
                if (read == 0)
                {
                    return;
                }

                total += read;

                if (Encoding.ASCII.GetString(buffer, 0, total)
                    .Contains("\r\n\r\n", StringComparison.Ordinal))
                {
                    break;
                }
            }

            const string body = "ok";
            var response =
                $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n{body}";
            await ssl.WriteAsync(Encoding.ASCII.GetBytes(response)).ConfigureAwait(false);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create(2_048);
            var request = new CertificateRequest(
                "CN=127.0.0.1",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var san = new SubjectAlternativeNameBuilder();
            san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());

            using var selfSigned = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));

            return X509CertificateLoader.LoadPkcs12(
                selfSigned.Export(X509ContentType.Pfx),
                password: null);
        }
    }
}