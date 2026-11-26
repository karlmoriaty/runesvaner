using Runesvaner;
using Runesvaner.Storage.Jsonl;

namespace Runesvaner.IntegrationTests;

public sealed class JsonlStoreTests
{
    [Fact]
    public async Task WriteAndRead_RoundTripsRecord()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var store = new JsonlRunesvanerStore(directory);
            var record = new RunesvanerRequestRecord(
                "runesvaner.request.v1",
                "id-1",
                "Chat",
                "test",
                "echo-1",
                null,
                DateTimeOffset.UtcNow,
                1000,
                null,
                "Ok",
                null,
                Array.Empty<RunesvanerSegment>(),
                null);

            await store.WriteAsync(record, TestContext.Current.CancellationToken);

            var actual = Assert.Single(JsonlRunesvanerStore.ReadAll(directory));
            Assert.Equal("id-1", actual.RequestId);
            Assert.Equal("Chat", actual.Operation);
            Assert.Equal("Ok", actual.Status);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}