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

## TLS

The .NET public API has no TLS-handshake-complete hook (`ConnectCallback` returns a plain stream
and `SocketsHttpHandler` performs TLS internally). Runesvaner does **not** fabricate a TLS segment.
TLS time remains inside connection establishment and is therefore not attributed to a separate
segment. Capturing it would require changing certificate validation callbacks or replacing the
HTTP stack, which is out of scope for the default path.

## Behavior

- Without an active `RunesvanerRequestContext`, the handler passes through with no allocation or
  segment writes.
- The handler respects the caller's `HttpCompletionOption`. Use `ResponseHeadersRead` when you need
  an accurate TTFB/content split; otherwise the response may already be buffered.
- Segment recording is thread-safe; connection callbacks may run off the request thread.