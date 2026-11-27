using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Runesvaner.Storage.Sqlite;

public sealed class SqliteRunesvanerStore : IRunesvanerStore, IRunesvanerQueryStore, IDisposable
{
    private const int PruneInterval = 64;

    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    private readonly string _databasePath;
    private readonly int _retentionDays;
    private readonly long _maxStorageBytes;
    private int _writes;

    public SqliteRunesvanerStore(string databasePath, RunesvanerOptions? options = null)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
            RunesvanerFileSecurity.RestrictDirectory(directory);
        }

        _databasePath = fullPath;
        _retentionDays = options?.RetentionDays ?? 0;
        _maxStorageBytes = options?.MaxStorageBytes ?? 0;
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

        RunesvanerFileSecurity.RestrictFile(fullPath);
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

            if (++_writes % PruneInterval == 0)
            {
                PruneLocked();
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask PruneAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            PruneLocked();
        }

        return ValueTask.CompletedTask;
    }

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

    public void Dispose()
        => _connection.Dispose();

    private void PruneLocked()
    {
        if (_retentionDays > 0)
        {
            var cutoff = DateTimeOffset.UtcNow
                .AddDays(-_retentionDays)
                .ToString("O", CultureInfo.InvariantCulture);

            using var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM requests WHERE started_at < $cutoff;";
            command.Parameters.AddWithValue("$cutoff", cutoff);
            command.ExecuteNonQuery();
        }

        if (_maxStorageBytes <= 0)
        {
            return;
        }

        try
        {
            if (new FileInfo(_databasePath).Length <= _maxStorageBytes)
            {
                return;
            }
        }
        catch (IOException)
        {
            return;
        }

        using var countCommand = _connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM requests;";
        var total = Convert.ToInt64(countCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
        var trim = Math.Max(1, total / 10);

        using var deleteCommand = _connection.CreateCommand();
        deleteCommand.CommandText =
            """
            DELETE FROM requests
            WHERE id IN (SELECT id FROM requests ORDER BY started_at ASC LIMIT $count);
            """;
        deleteCommand.Parameters.AddWithValue("$count", trim);
        deleteCommand.ExecuteNonQuery();
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
}