using Runesvaner;
using Runesvaner.Storage.Sqlite;

namespace Runesvaner.UnitTests;

public sealed class SqliteQueryFilterTests
{
    [Fact]
    public async Task ReadsBySessionAndAppliesLimit()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "query.db");

        try
        {
            using var store = new SqliteRunesvanerStore(path);

            var oldest = CreateRecord("a-1", "session-a", DateTimeOffset.UtcNow.AddMinutes(-2));
            var middle = CreateRecord("a-2", "session-a", DateTimeOffset.UtcNow.AddMinutes(-1));
            var newest = CreateRecord("b-1", "session-b", DateTimeOffset.UtcNow);

            await store.WriteAsync(oldest, TestContext.Current.CancellationToken);
            await store.WriteAsync(middle, TestContext.Current.CancellationToken);
            await store.WriteAsync(newest, TestContext.Current.CancellationToken);

            var sessionA = await store.ReadRequestsAsync(
                "session-a",
                10,
                TestContext.Current.CancellationToken);

            Assert.Equal(2, sessionA.Count);
            Assert.Equal("a-2", sessionA[0].RequestId);
            Assert.All(sessionA, record => Assert.Equal("session-a", record.SessionId));

            var limited = await store.ReadRequestsAsync(
                null,
                1,
                TestContext.Current.CancellationToken);

            var record = Assert.Single(limited);
            Assert.Equal("b-1", record.RequestId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task GetsRequestByIdWithoutScanningEverything()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "query.db");

        try
        {
            using var store = new SqliteRunesvanerStore(path);
            var record = CreateRecord("request-1", "session-a", DateTimeOffset.UtcNow);

            await store.WriteAsync(record, TestContext.Current.CancellationToken);

            var found = await store.GetRequestAsync(
                "request-1",
                TestContext.Current.CancellationToken);
            var missing = await store.GetRequestAsync(
                "request-2",
                TestContext.Current.CancellationToken);

            Assert.NotNull(found);
            Assert.Equal("request-1", found.RequestId);
            Assert.Null(missing);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static RunesvanerRequestRecord CreateRecord(
        string requestId,
        string sessionId,
        DateTimeOffset startedAtUtc) => new(
        "runesvaner.request.v1",
        requestId,
        "Chat",
        "test",
        "model-1",
        "model-1",
        startedAtUtc,
        1_000,
        null,
        "Ok",
        null,
        [],
        null,
        null,
        null,
        sessionId);

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-query-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}