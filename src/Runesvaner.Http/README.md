# Runesvaner.Http

HTTP segment diagnostics for Runesvaner. The handler records network-level spans into the
active request started by the MEAI decorator.

## Usage

    using Runesvaner.Http;

    var httpClient = new HttpClient(new RunesvanerHttpHandler());
    var openAi = new OpenAIClient(credential, new OpenAIClientOptions
    {
        Transport = new HttpClientPipelineTransport(httpClient)
    });

    var client = new ChatClientBuilder(openAi.GetChatClient("gpt-5").AsIChatClient())
        .UseRunesvaner()
        .Build(serviceProvider);

## Segments

| Segment | Source | Notes |
| --- | --- | --- |
| `Queue` | Handler entry relative to request start | Includes MEAI middleware time before HTTP |
| `Connect` | `SocketsHttpHandler.ConnectCallback` | DNS + TCP; only when a new connection is opened |
| `Upload` | Request content serialization wrapper | Absent for bodyless requests |
| `ProviderQueue` | Wait for response headers | HTTP-level provider wait, not token-level TTFT |
| `Generation` | Response stream EOF wrapper | Requires the caller to consume the body; `ResponseHeadersRead` gives the truest split |

`Ttft` is produced by the MEAI decorator (first non-empty text delta) and is combined with these
segments in the same request record.

## TLS (opt-in probe)

The .NET public API has no TLS-handshake-complete hook. An optional probe approximates it:

    var sockets = RunesvanerHttpHandler.CreateSocketsHandler(
        enableTlsProbe: true,
        validateCertificate: (_, _, _, errors) => errors == SslPolicyErrors.None);

    var httpClient = new HttpClient(new RunesvanerHttpHandler(sockets));

- The probe records a `Tls` segment from TCP completion to the certificate-validation callback.
- The segment is marked `Estimated=true` and `Measurement=Derived`; it is not a precise TLS duration.
- The probe is **off by default**. When enabled it installs a certificate-validation callback. If
  you pass `validateCertificate`, that callback is invoked and its result decides whether the
  connection succeeds; without it, the probe accepts only `SslPolicyErrors.None`.
- Connection reuse produces no TLS segment, which is correct: no new handshake occurred.

## Behavior

- Without an active `RunesvanerRequestContext`, the handler passes through with no allocation or
  segment writes.
- The handler respects the caller's `HttpCompletionOption`. Use `ResponseHeadersRead` when you need
  an accurate TTFB/content split; otherwise the response may already be buffered.
- Segment recording is thread-safe; connection callbacks may run off the request thread.