using Microsoft.Extensions.AI;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;

namespace Runesvaner.IntegrationTests;

public sealed class EmbeddingCaptureTests
{
    [Fact]
    public async Task Embedding_RecordsBatchSizeDimensionsUsageAndSegment()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var generator = new RunesvanerEmbeddingGenerator(
            new FakeEmbeddingGenerator(),
            recorder,
            "test-provider");

        var embeddings = await generator.GenerateAsync(
            ["first", "second"],
            new EmbeddingGenerationOptions { ModelId = "embed-1" },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, embeddings.Count);

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal("Embeddings", record.Operation);
        Assert.Equal("test-provider", record.Provider);
        Assert.Equal("embed-1", record.RequestModel);
        Assert.Equal(10L, record.Usage?.InputTokens);
        Assert.Contains(record.Segments, segment => segment.Kind == SegmentKind.Embedding);
        Assert.Equal("2", record.Tags?["embedding.count"]);
        Assert.Equal("3", record.Tags?["embedding.dimensions"]);
        Assert.Equal("Ok", record.Status);
    }

    [Fact]
    public async Task Embedding_ErrorPathIsRecordedAndRethrown()
    {
        var store = new InMemoryRunesvanerStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());
        var generator = new RunesvanerEmbeddingGenerator(
            new ThrowingEmbeddingGenerator(),
            recorder,
            "test-provider");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            generator.GenerateAsync(
                ["first"],
                cancellationToken: TestContext.Current.CancellationToken));

        var record = Assert.Single(await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken));

        Assert.Equal("Error", record.Status);
        Assert.Equal("Embeddings", record.Operation);
    }

    private sealed class FakeEmbeddingGenerator
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var embeddings = values
                .Select(_ => new Embedding<float>(new float[] { 1f, 2f, 3f }))
                .ToArray();

            var result = new GeneratedEmbeddings<Embedding<float>>(embeddings)
            {
                Usage = new UsageDetails { InputTokenCount = 10 }
            };

            return Task.FromResult(result);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingEmbeddingGenerator
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("intentional embedding failure");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}