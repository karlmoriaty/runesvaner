using System.Text.Json;

namespace Runesvaner.Storage.Jsonl;

public sealed class JsonlRunesvanerStore : IRunesvanerStore
{
    private readonly string _directory;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private string? _currentPath;
    private DateOnly _currentDate;

    public JsonlRunesvanerStore(string directory, TimeProvider? timeProvider = null)
    {
        _directory = Path.GetFullPath(directory);
        _timeProvider = timeProvider ?? TimeProvider.System;
        Directory.CreateDirectory(Path.Combine(_directory, "sessions"));
    }

    public ValueTask WriteAsync(RunesvanerRequestRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var line = JsonSerializer.Serialize(record, RunesvanerJsonContext.Default.RunesvanerRequestRecord);

        lock (_gate)
        {
            File.AppendAllText(GetCurrentPath(), line + Environment.NewLine);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public static IEnumerable<RunesvanerRequestRecord> ReadAll(string directory)
    {
        var sessions = Path.Combine(Path.GetFullPath(directory), "sessions");
        if (!Directory.Exists(sessions))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(sessions, "*.jsonl").OrderBy(path => path, StringComparer.Ordinal))
        {
            foreach (var line in File.ReadLines(file))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                RunesvanerRequestRecord? record = null;
                try
                {
                    record = JsonSerializer.Deserialize(line, RunesvanerJsonContext.Default.RunesvanerRequestRecord);
                }
                catch (JsonException)
                {
                    // Ignore a partially written line from a crashed process.
                }

                if (record is not null)
                {
                    yield return record;
                }
            }
        }
    }

    private string GetCurrentPath()
    {
        var now = _timeProvider.GetUtcNow();
        var date = DateOnly.FromDateTime(now.UtcDateTime);

        if (_currentPath is null || _currentDate != date)
        {
            _currentDate = date;
            _currentPath = Path.Combine(_directory, "sessions", $"{date:yyyyMMdd}.jsonl");
        }

        return _currentPath;
    }
}