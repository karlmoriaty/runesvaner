using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace Runesvaner.Http;

/// <summary>HTTP handler that records queue, connect, upload, provider wait, and generation segments.</summary>
public sealed class RunesvanerHttpHandler : DelegatingHandler
{
    /// <summary>Creates a handler with a preconfigured SocketsHttpHandler.</summary>
    public RunesvanerHttpHandler()
        : this(CreateSocketsHandler())
    {
    }

    /// <summary>Creates a handler around an existing inner handler.</summary>
    public RunesvanerHttpHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    /// <summary>Creates a SocketsHttpHandler whose connect callback records DNS and TCP time.</summary>
    /// <param name="enableTlsProbe">Enables an estimated TLS segment from certificate validation.</param>
    /// <param name="validateCertificate">Optional certificate validation callback preserved by the probe.</param>
    public static SocketsHttpHandler CreateSocketsHandler(
        bool enableTlsProbe = false,
        RemoteCertificateValidationCallback? validateCertificate = null)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = ConnectAsync
        };

        if (enableTlsProbe)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    RecordTlsSegment();

                    return validateCertificate is null
                        ? errors == SslPolicyErrors.None
                        : validateCertificate(sender, certificate, chain, errors);
                }
            };
        }

        return handler;
    }

    private static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var recorderRequest = RunesvanerRequestContext.Request;
        var offsetUs = RunesvanerRequestContext.ElapsedMicroseconds(started);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        try
        {
            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            socket.Dispose();
            throw;
        }

        var tcpCompleted = Stopwatch.GetTimestamp();
        RunesvanerRequestContext.SetTcpCompleted(tcpCompleted);

        if (recorderRequest is not null)
        {
            var endUs = RunesvanerRequestContext.ElapsedMicroseconds(tcpCompleted);
            recorderRequest.RecordSegment(new RunesvanerSegment(
                SegmentKind.Connect,
                offsetUs,
                Math.Max(1, endUs - offsetUs),
                false,
                SegmentMeasurement.Stopwatch));
        }

        return new NetworkStream(socket, ownsSocket: true);
    }

    private static void RecordTlsSegment()
    {
        var recorderRequest = RunesvanerRequestContext.Request;

        if (recorderRequest is null ||
            !RunesvanerRequestContext.TryGetTcpCompleted(out var tcpCompleted))
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        var offsetUs = RunesvanerRequestContext.ElapsedMicroseconds(tcpCompleted);
        var durationUs = Math.Max(
            1,
            (now - tcpCompleted) * 1_000_000 / Stopwatch.Frequency);

        recorderRequest.RecordSegment(new RunesvanerSegment(
            SegmentKind.Tls,
            offsetUs,
            durationUs,
            true,
            SegmentMeasurement.Derived));
    }

    /// <summary>Records segments around the inner handler send.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var recorderRequest = RunesvanerRequestContext.Request;

        if (recorderRequest is null)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var handlerStart = Stopwatch.GetTimestamp();
        var handlerOffsetUs = RunesvanerRequestContext.ElapsedMicroseconds(handlerStart);

        recorderRequest.RecordSegment(new RunesvanerSegment(
            SegmentKind.Queue,
            0,
            Math.Max(1, handlerOffsetUs),
            false,
            SegmentMeasurement.Stopwatch));

        long uploadStart = 0;
        long uploadEnd = 0;

        if (request.Content is not null)
        {
            request.Content = new MeasuringContent(
                request.Content,
                () => uploadStart = Stopwatch.GetTimestamp(),
                () => uploadEnd = Stopwatch.GetTimestamp());
        }

        var response = await base
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var headersTimestamp = Stopwatch.GetTimestamp();
        var headersOffsetUs = RunesvanerRequestContext.ElapsedMicroseconds(headersTimestamp);

        if (uploadStart != 0 && uploadEnd != 0)
        {
            var uploadStartUs = RunesvanerRequestContext.ElapsedMicroseconds(uploadStart);
            var uploadEndUs = RunesvanerRequestContext.ElapsedMicroseconds(uploadEnd);

            recorderRequest.RecordSegment(new RunesvanerSegment(
                SegmentKind.Upload,
                uploadStartUs,
                Math.Max(1, uploadEndUs - uploadStartUs),
                false,
                SegmentMeasurement.Stopwatch));

            recorderRequest.RecordSegment(new RunesvanerSegment(
                SegmentKind.ProviderQueue,
                uploadEndUs,
                Math.Max(1, headersOffsetUs - uploadEndUs),
                false,
                SegmentMeasurement.Stopwatch));
        }
        else
        {
            recorderRequest.RecordSegment(new RunesvanerSegment(
                SegmentKind.ProviderQueue,
                handlerOffsetUs,
                Math.Max(1, headersOffsetUs - handlerOffsetUs),
                false,
                SegmentMeasurement.Stopwatch));
        }

        if (response.Content is not null)
        {
            var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            var timingStream = new TimingStream(
                stream,
                recorderRequest,
                headersTimestamp,
                headersOffsetUs);

            var content = new StreamContent(timingStream);

            foreach (var header in response.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Content = content;
        }

        return response;
    }

    private sealed class MeasuringContent : HttpContent
    {
        private readonly HttpContent _inner;
        private readonly Action _onStart;
        private readonly Action _onEnd;

        public MeasuringContent(HttpContent inner, Action onStart, Action onEnd)
        {
            _inner = inner;
            _onStart = onStart;
            _onEnd = onEnd;

            foreach (var header in inner.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            _onStart();

            try
            {
                await _inner.CopyToAsync(stream).ConfigureAwait(false);
            }
            finally
            {
                _onEnd();
            }
        }

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken)
        {
            _onStart();

            try
            {
                await _inner.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _onEnd();
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (_inner.Headers.ContentLength is { } contentLength)
            {
                length = contentLength;
                return true;
            }

            length = 0;
            return false;
        }

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
            => _inner.ReadAsStream(cancellationToken);

        protected override Task<Stream> CreateContentReadStreamAsync()
            => _inner.ReadAsStreamAsync();

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
            => _inner.ReadAsStreamAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class TimingStream : Stream
    {
        private readonly Stream _inner;
        private readonly IRunesvanerRequest _request;
        private readonly long _headersTimestamp;
        private readonly long _headersOffsetUs;
        private readonly object _gate = new();
        private bool _completed;

        public TimingStream(
            Stream inner,
            IRunesvanerRequest request,
            long headersTimestamp,
            long headersOffsetUs)
        {
            _inner = inner;
            _request = request;
            _headersTimestamp = headersTimestamp;
            _headersOffsetUs = headersOffsetUs;
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            HandleRead(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            HandleRead(read);
            return read;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin)
            => _inner.Seek(offset, origin);

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override void Flush()
            => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => _inner.FlushAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private void HandleRead(int read)
        {
            if (read != 0)
            {
                return;
            }

            lock (_gate)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
            }

            var durationUs = Stopwatch.GetElapsedTime(_headersTimestamp).Ticks / 10;

            _request.RecordSegment(new RunesvanerSegment(
                SegmentKind.Generation,
                _headersOffsetUs,
                Math.Max(1, durationUs),
                false,
                SegmentMeasurement.Stopwatch));
        }
    }
}