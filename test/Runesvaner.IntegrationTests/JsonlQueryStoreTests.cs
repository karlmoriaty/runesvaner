using Runesvaner;
using Runesvaner.Storage.Jsonl;

namespace Runesvaner.IntegrationTests;

public sealed class JsonlQueryStoreTests
{
    [Fact]
    public async Task QueryStore_FiltersSessionsAndReturnsDetail()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var store = new JsonlRunesvanerStore(directory);
            await store.WriteAsync(Record("r1", "s1"), TestContext.Current.CancellationToken);
            await store.WriteAsync(Record("r2", "s2"), TestContext.Current.CancellationToken);

            var query = new JsonlRunesvanerQueryStore(directory);
            var sessionRequests = await query.ReadRequestsAsync(
                "s1",
                10,
                TestContext.Current.CancellationToken);
            Assert.Single(sessionRequests);
            Assert.Equal("r1", sessionRequests[0].RequestId);

            var sessions = await query.ReadSessionsAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, sessions.Count);

            var detail = await query.GetRequestAsync("r2", TestContext.Current.CancellationToken);
            Assert.NotNull(detail);
            Assert.Equal("s2", detail!.SessionId);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static RunesvanerRequestRecord Record(string requestId, string sessionId)
        => new(
            "runesvaner.request.v1",
            requestId,
            "Chat",
            "test",
            "echo-1",
            null,
            DateTimeOffset.UtcNow,
            1_000,
            null,
            "Ok",
            null,
            Array.Empty<RunesvanerSegment>(),
            null,
            null,
            null,
            sessionId);
}