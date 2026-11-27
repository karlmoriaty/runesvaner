using Runesvaner;
using Runesvaner.Storage.Jsonl;
using Runesvaner.Storage.Sqlite;

namespace Runesvaner.UnitTests;

public sealed class RetentionTests
{
    [Fact]
    public async Task Jsonl_DeletesFilesOlderThanRetentionWindow()
    {
        var directory = CreateDirectory();
        var sessions = Path.Combine(directory, "sessions");
        Directory.CreateDirectory(sessions);
        var oldFile = Path.Combine(sessions, "20260101.jsonl");
        File.WriteAllText(oldFile, "old");

        var store = new JsonlRunesvanerStore(
            directory,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero)),
            new RunesvanerOptions { RetentionDays = 5 });

        await store.WriteAsync(CreateRecord(0), TestContext.Current.CancellationToken);

        Assert.False(File.Exists(oldFile));
        Assert.True(File.Exists(Path.Combine(sessions, "20261127.jsonl")));
    }

    [Fact]
    public async Task Jsonl_DeletesOldestFilesWhenSizeExceeded()
    {
        var directory = CreateDirectory();
        var sessions = Path.Combine(directory, "sessions");
        Directory.CreateDirectory(sessions);
        var oldFile = Path.Combine(sessions, "20260101.jsonl");
        File.WriteAllText(oldFile, "older-content-that-exceeds-the-budget");

        var store = new JsonlRunesvanerStore(
            directory,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero)),
            new RunesvanerOptions { MaxStorageBytes = 1 });

        await store.WriteAsync(CreateRecord(0), TestContext.Current.CancellationToken);

        Assert.False(File.Exists(oldFile));
    }

    [Fact]
    public async Task Sqlite_DeletesRowsOlderThanRetentionWindow()
    {
        var path = Path.Combine(CreateDirectory(), "retention.db");

        using var store = new SqliteRunesvanerStore(
            path,
            new RunesvanerOptions { RetentionDays = 5 });

        await store.WriteAsync(
            CreateRecord(0, DateTimeOffset.UtcNow.AddDays(-10)),
            TestContext.Current.CancellationToken);
        await store.WriteAsync(
            CreateRecord(1, DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);

        await store.PruneAsync(TestContext.Current.CancellationToken);

        var records = await store.ReadRequestsAsync(
            null,
            10,
            TestContext.Current.CancellationToken);

        var record = Assert.Single(records);
        Assert.Equal("model-1", record.RequestModel);
    }

    [Fact]
    public async Task StoresRestrictPermissionsToOwnerOnUnix()
    {
        var directory = CreateDirectory();
        var sessionFile = Path.Combine(directory, "sessions", "20261127.jsonl");

        var store = new JsonlRunesvanerStore(
            directory,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 27, 0, 0, 0, TimeSpan.Zero)));
        await store.WriteAsync(CreateRecord(0), TestContext.Current.CancellationToken);

        if (OperatingSystem.IsWindows())
        {
            Assert.True(File.Exists(sessionFile));
            return;
        }

        Assert.Equal(
            RunesvanerFileSecurity.OwnerDirectoryMode,
            File.GetUnixFileMode(Path.Combine(directory, "sessions")));
        Assert.Equal(
            RunesvanerFileSecurity.OwnerFileMode,
            File.GetUnixFileMode(sessionFile));
    }

    private static RunesvanerRequestRecord CreateRecord(
        int index,
        DateTimeOffset? startedAtUtc = null) => new(
        "runesvaner.request.v1",
        Guid.NewGuid().ToString("N"),
        "Chat",
        "test",
        "model-" + index,
        "model-" + index,
        startedAtUtc ?? DateTimeOffset.UtcNow,
        1_200,
        400,
        "Ok",
        new RunesvanerUsage(100, 50, null, null, UsageSource.Provider),
        [new RunesvanerSegment(SegmentKind.Generation, 0, 1_200, false, SegmentMeasurement.Stopwatch)],
        null,
        null,
        null,
        "retention-test");

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-retention-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}