using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Runesvaner.Storage.Sqlite;

public sealed class SqliteRunesvanerStore : IRunesvanerStore, IRunesvanerQueryStore, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();

    public SqliteRunesvanerStore(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connection = new SqliteConnection($"Data Source={fullPath};Pooling=False");
        _connection.Open();

        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS requests (
                id TEXT PRIMARY KEY,
                started_at TEXT NOT NULL,
                operation TEXT NOT NULL,
                provider TEXT NULL,
                model TEXT NULL,
                status TEXT NOT NULL,
                duration_us INTEGER NOT NULL,
                ttft_us INTEGER NULL,
                payload TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public ValueTask WriteAsync(RunesvanerRequestRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(record, RunesvanerJsonContext.Default.RunesvanerRequestRecord);

        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO requests (id, started_at, operation, provider, model, status, duration_us, ttft_us, payload)
                VALUES ($id, $started_at, $operation, $provider, $model, $status, $duration_us, $ttft_us, $payload);
                """;
            command.Parameters.AddWithValue("$id", record.RequestId);
            command.Parameters.AddWithValue("$started_at", record.StartedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$operation", record.Operation);
            command.Parameters.AddWithValue("$provider", (object?)record.Provider ?? DBNull.Value);
            command.Parameters.AddWithValue("$model", (object?)record.RequestModel ?? DBNull.Value);
            command.Parameters.AddWithValue("$status", record.Status);
            command.Parameters.AddWithValue("$duration_us", record.DurationUs);
            command.Parameters.AddWithValue("$ttft_us", record.TtftUs is null ? DBNull.Value : record.TtftUs.Value);
            command.Parameters.AddWithValue("$payload", payload);
            command.ExecuteNonQuery();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<RunesvanerRequestRecord>> ReadRequestsAsync(
        string? sessionId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var records = ReadAllRecords();

        if (!string.IsNullOrEmpty(sessionId))
        {
            records = records
                .Where(record => string.Equals(record.SessionId, sessionId, StringComparison.Ordinal))
                .ToList();
        }

        return new ValueTask<IReadOnlyList<RunesvanerRequestRecord>>(
            records
                .OrderByDescending(record => record.StartedAtUtc)
                .Take(Math.Max(1, limit))
                .ToArray());
    }

    public ValueTask<RunesvanerRequestRecord?> GetRequestAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = ReadAllRecords().FirstOrDefault(candidate =>
            string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal));

        return new ValueTask<RunesvanerRequestRecord?>(record);
    }

    public ValueTask<IReadOnlyList<RunesvanerSessionSummary>> ReadSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<IReadOnlyList<RunesvanerSessionSummary>>(
            RunesvanerSessionAggregator.Aggregate(ReadAllRecords()));
    }

    private List<RunesvanerRequestRecord> ReadAllRecords()
    {
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT payload FROM requests ORDER BY started_at DESC;";
            using var reader = command.ExecuteReader();
            var records = new List<RunesvanerRequestRecord>();

            while (reader.Read())
            {
                var payload = reader.GetString(0);
                try
                {
                    var record = JsonSerializer.Deserialize(
                        payload,
                        RunesvanerJsonContext.Default.RunesvanerRequestRecord);

                    if (record is not null)
                    {
                        records.Add(record);
                    }
                }
                catch (JsonException)
                {
                    // Ignore a payload that cannot be read; keep the rest queryable.
                }
            }

            return records;
        }
    }

    public void Dispose()
        => _connection.Dispose();
}