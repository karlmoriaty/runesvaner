# Example 1: Console with DI-free recording

**Goal:** capture one chat request with the smallest possible setup, then inspect it from the CLI.

## Code

`samples/Runesvaner.Sample.Console/Program.cs`

The sample creates a `JsonlRunesvanerStore`, a `RunesvanerRecorder`, and a
`RunesvanerChatClient` around an in-process echo client. No provider, no network, no API key.

## Run

    dotnet run --project samples/Runesvaner.Sample.Console -c Release

## Expected output

    Echo: Hello, Runesvaner
    Captured to <repository>/.runesvaner

## Inspect

    dotnet run --project src/Runesvaner.Cli -c Release -- report

The report reads `.runesvaner/sessions/*.jsonl` and prints request count, model, duration,
TTFT, and status. Cost is `unknown` because the echo model is not in the built-in price table —
that is intentional, not a zero.

## What to look for

- One request record per call, written to a dated JSONL file.
- `status=Ok` and a duration segment even when the inner client is synchronous.
- No prompt content is stored by default (`ContentCapture.None`).