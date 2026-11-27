using System.Runtime.CompilerServices;
using Microsoft.Extensions.VectorData;
using Runesvaner;
using Runesvaner.VectorData;

var store = new InMemoryRunesvanerStore();
var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
var searchable = new RunesvanerVectorSearchable<Document>(
    new FakeSearchable(),
    recorder,
    "knowledge-base");

Console.WriteLine("VectorData sample: top-3 retrieval from a fake collection.");

var query = new ReadOnlyMemory<float>(new float[] { 0.1f, 0.2f, 0.3f });
var ids = new List<string>();

await foreach (var result in searchable.SearchAsync(query, top: 3))
{
    ids.Add(result.Record.Id);
}

Console.WriteLine($"results: {string.Join(", ", ids)}");

var record = (await store.ReadRequestsAsync(null, 1))[0];

Console.WriteLine($"operation: {record.Operation}");
Console.WriteLine($"collection: {record.Tags?["vector.collection"]}");
Console.WriteLine($"top_k: {record.Tags?["vector.top_k"]}");
Console.WriteLine($"segments: {string.Join(", ", record.Segments.Select(segment => segment.Kind))}");
Console.WriteLine($"status: {record.Status}");

internal sealed class Document
{
    public string Id { get; set; } = "";
}

internal sealed class FakeSearchable : IVectorSearchable<Document>
{
    public async IAsyncEnumerable<VectorSearchResult<Document>> SearchAsync<TVector>(
        TVector vector,
        int top,
        VectorSearchOptions<Document>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TVector : notnull
    {
        await Task.Yield();

        for (var index = 0; index < top; index++)
        {
            yield return new VectorSearchResult<Document>(
                new Document { Id = $"doc-{index + 1}" },
                1.0 - (index * 0.1));
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}