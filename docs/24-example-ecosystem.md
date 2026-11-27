# Example 5: Semantic Kernel and VectorData

**Goal:** record Semantic Kernel chat completions and VectorData retrievals, and verify that
nested SK + MEAI usage does not double-record.

## Code

- `samples/Runesvaner.Sample.SemanticKernel/Program.cs`
- `samples/Runesvaner.Sample.VectorData/Program.cs`

## Run

    dotnet run --project samples/Runesvaner.Sample.SemanticKernel -c Release
    dotnet run --project samples/Runesvaner.Sample.VectorData -c Release

## Expected output

Semantic Kernel:

    Semantic Kernel sample: one recorded chat completion.
    response: Hello from Semantic Kernel.
    operation: Chat
    provider: semantic-kernel
    segments: Generation
    status: Ok

VectorData:

    VectorData sample: top-3 retrieval from a fake collection.
    results: doc-1, doc-2, doc-3
    operation: VectorSearch
    collection: knowledge-base
    top_k: 3
    segments: VectorSearch
    status: Ok

## What to look for

- SK recording uses the same request record schema as MEAI; only the adapter differs.
- VectorData records `vector.collection`, `vector.top_k`, and a `VectorSearch` segment.
- When SK is nested inside an active MEAI request (or the reverse), only the outer request is
  recorded. This is the M3 de-duplication rule.
- SK usage is not recorded by this adapter because Semantic Kernel Abstractions 1.82 has no
  unified token-usage type; usage is recorded by the MEAI layer when present.