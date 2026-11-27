# Example 4: OpenAI protocol without credentials

**Goal:** verify the real OpenAI HTTP protocol path and HTTP segments without an API key or network
access.

## Code

`samples/Runesvaner.Sample.OpenAIProtocol/Program.cs`

The sample starts a loopback TCP server that returns an OpenAI chat-completion JSON response,
points the official `Microsoft.Extensions.AI.OpenAI` client at it, and wraps the client with
`RunesvanerChatClient` plus `RunesvanerHttpHandler`.

## Run

    dotnet run --project samples/Runesvaner.Sample.OpenAIProtocol -c Release

## Expected output

    OpenAI protocol sample: local mock, no API key required.
    response: Hello from the local OpenAI mock.
    usage: input=100, output=50
    cost: Unknown (OpenAI prices are not guessed)
    segments: Queue, Connect, Upload, ProviderQueue, Generation, Generation
    status: Ok

## What to look for

- `usage` is parsed from the provider protocol.
- `cost` stays `Unknown`: first-party OpenAI prices were not verifiable at snapshot time, and
  Runesvaner never guesses.
- Two `Generation` segments appear: one from the HTTP response-stream layer and one from the MEAI
  decorator. They describe different layers and overlap in time; the Studio waterfall renders
  segments on lanes.
- `Connect` is measured by `ConnectCallback` (DNS + TCP). No TLS segment is produced for plain
  HTTP.

## Safety notes

- The API key is the literal `test-key`; it never leaves the process.
- The SDK endpoint is explicitly set to `127.0.0.1`; the default would reach the real provider.