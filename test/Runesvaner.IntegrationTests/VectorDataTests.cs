using System.Runtime.CompilerServices;
using Microsoft.Extensions.VectorData;
using Runesvaner;
using Runesvaner.VectorData;

namespace Runesvaner.IntegrationTests;

public sealed class VectorDataTests
{
    [Fact]
    public async Task Search_RecordsCollectionTopKAndDuration()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var searchable = new RunesvanerVectorSearchable<TestRecord>(
            new FakeSearchable(),
            recorder,
            "docs");

        var results = new List<VectorSearchResult<TestRecord>>();

        await foreach (var result in searchable.SearchAsync(
                           new ReadOnlyMemory<float>(new float[] { 1f, 2f, 3f }),
                           2,
                           cancellationToken: TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        Assert.Equal(2, results.Count);

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal("VectorSearch", record.Operation);
        Assert.Equal("docs", record.Tags?["vector.collection"]);
        Assert.Equal("2", record.Tags?["vector.top_k"]);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.VectorSearch);
        Assert.Equal("Ok", record.Status);
    }

    [Fact]
    public async Task Search_NestedInExistingContext_IsNotRecordedTwice()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var outer = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "outer",
            "outer-model",
            null));

        using (RunesvanerRequestContext.Begin(outer))
        {
            var searchable = new RunesvanerVectorSearchable<TestRecord>(
                new FakeSearchable(),
                recorder,
                "docs");

            await foreach (var _ in searchable.SearchAsync(
                               new ReadOnlyMemory<float>(new float[] { 1f }),
                               1,
                               cancellationToken: TestContext.Current.CancellationToken))
            {
            }
        }

        outer.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal(outer.RequestId, record.RequestId);
    }

    [Fact]
    public async Task Search_ErrorPathIsRecordedAndRethrown()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var searchable = new RunesvanerVectorSearchable<TestRecord>(
            new ThrowingSearchable(),
            recorder,
            "docs");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in searchable.SearchAsync(
                               new ReadOnlyMemory<float>(new float[] { 1f }),
                               1,
                               cancellationToken: TestContext.Current.CancellationToken))
            {
            }
        });

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal("Error", record.Status);
    }

    private sealed class TestRecord
    {
        public int Id { get; set; }
    }

    private sealed class FakeSearchable : IVectorSearchable<TestRecord>
    {
        public async IAsyncEnumerable<VectorSearchResult<TestRecord>> SearchAsync<TVector>(
            TVector vector,
            int top,
            VectorSearchOptions<TestRecord>? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TVector : notnull
        {
            await Task.Yield();

            for (var index = 0; index < top; index++)
            {
                yield return new VectorSearchResult<TestRecord>(
                    new TestRecord { Id = index },
                    1.0 - (index * 0.1));
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }

    private sealed class ThrowingSearchable : IVectorSearchable<TestRecord>
    {
        public async IAsyncEnumerable<VectorSearchResult<TestRecord>> SearchAsync<TVector>(
            TVector vector,
            int top,
            VectorSearchOptions<TestRecord>? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TVector : notnull
        {
            await Task.Yield();

            if (top > 0)
            {
                throw new InvalidOperationException("intentional vector search failure");
            }

            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}