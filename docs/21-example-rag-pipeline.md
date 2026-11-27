# Example 2: RAG pipeline

**Goal:** capture an embedding step, a vector search, and a generation step as separate requests.

## Code

`samples/Runesvaner.Sample.Rag/Program.cs`

The sample uses fake embedding and vector-search clients plus a fake generator. It records three
logical operations in one run and loads the illustrative price table from
`pricing/example-prices.json`.

## Run

    dotnet run --project samples/Runesvaner.Sample.Rag -c Release

## Expected output

    RAG sample captured: embedding + vector search + generation.
    Data: <repository>/.runesvaner
    Run: dotnet run --project src/Runesvaner.Cli -c Release -- report

## Inspect

    dotnet run --project src/Runesvaner.Cli -c Release -- report
    dotnet run --project src/Runesvaner.Cli -c Release -- costs

`report` shows one row per operation. `costs` groups by provider/model and reports the example
cost; the sample table is explicitly illustrative and must not be read as provider pricing.

## What to look for

- `Embeddings`, `VectorSearch`, and `Chat` operation names.
- Embedding segments carry batch size and dimensions in tags.
- Cost is computed from the example table, with `CostStatus.Known`.