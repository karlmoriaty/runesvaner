using Microsoft.Data.Sqlite;
using Runesvaner;
using Runesvaner.Storage.Sqlite;

namespace Runesvaner.IntegrationTests;

public sealed class SqliteStoreTests
{
    [Fact]
    public async Task Write_PersistsRow()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-tests",
            Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "runesvaner.db");

        try
        {
            var record = new RunesvanerRequestRecord(
                "runesvaner.request.v1",
                "sqlite-1",
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

            using (var store = new SqliteRunesvanerStore(databasePath))
            {
                await store.WriteAsync(record, TestContext.Current.CancellationToken);
            }

            using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM requests WHERE id = $id;";
            command.Parameters.AddWithValue("$id", "sqlite-1");

            var count = (long)command.ExecuteScalar()!;
            Assert.Equal(1L, count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}