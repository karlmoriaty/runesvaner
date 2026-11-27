using System.Globalization;
using System.Text.Json;

namespace Runesvaner.Storage.Jsonl;

/// <summary>JSON Lines storage with retention and owner-only permissions.</summary>
public sealed class JsonlRunesvanerStore : IRunesvanerStore
{
    private readonly string _directory;
    private readonly string _sessionsDirectory;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly HashSet<string> _restricted = new(StringComparer.Ordinal);
    private readonly int _retentionDays;
    private readonly long _maxStorageBytes;
    private string? _currentPath;
    private DateOnly _currentDate;

    /// <summary>Creates a JSON Lines store under the supplied directory.</summary>
    public JsonlRunesvanerStore(
        string directory,
        TimeProvider? timeProvider = null,
        RunesvanerOptions? options = null)
    {
        _directory = Path.GetFullPath(directory);
        _sessionsDirectory = Path.Combine(_directory, "sessions");
        _timeProvider = timeProvider ?? TimeProvider.System;
        _retentionDays = options?.RetentionDays ?? 0;
        _maxStorageBytes = options?.MaxStorageBytes ?? 0;

        Directory.CreateDirectory(_sessionsDirectory);
        RunesvanerFileSecurity.RestrictDirectory(_directory);
        RunesvanerFileSecurity.RestrictDirectory(_sessionsDirectory);
    }

    /// <summary>Appends one record to the current daily JSONL file.</summary>
    public ValueTask WriteAsync(RunesvanerRequestRecord record, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var line = JsonSerializer.Serialize(record, RunesvanerJsonContext.Default.RunesvanerRequestRecord);

        lock (_gate)
        {
            var path = GetCurrentPath();
            File.AppendAllText(path, line + Environment.NewLine);
            RestrictOnce(path);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>No-op flush; JSONL writes are synchronous.</summary>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    /// <summary>Reads all JSONL records from a data directory.</summary>
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
            _currentPath = Path.Combine(_sessionsDirectory, $"{date:yyyyMMdd}.jsonl");
            Prune(_currentPath);
        }

        return _currentPath;
    }

    private void Prune(string currentPath)
    {
        if (_retentionDays <= 0 && _maxStorageBytes <= 0)
        {
            return;
        }

        if (_retentionDays > 0)
        {
            var cutoff = _currentDate.AddDays(-_retentionDays);

            foreach (var file in EnumerateSessionFiles())
            {
                if (TryParseDate(file, out var date) && date < cutoff)
                {
                    TryDelete(file);
                }
            }
        }

        if (_maxStorageBytes <= 0)
        {
            return;
        }

        var files = EnumerateSessionFiles()
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var total = files.Sum(Length);

        foreach (var file in files)
        {
            if (total <= _maxStorageBytes)
            {
                break;
            }

            if (string.Equals(file, currentPath, StringComparison.Ordinal))
            {
                continue;
            }

            var length = Length(file);
            if (TryDelete(file))
            {
                total -= length;
            }
        }
    }

    private void RestrictOnce(string path)
    {
        if (_restricted.Add(path))
        {
            RunesvanerFileSecurity.RestrictFile(path);
        }
    }

    private string[] EnumerateSessionFiles()
        => Directory.EnumerateFiles(_sessionsDirectory, "*.jsonl").ToArray();

    private static bool TryParseDate(string path, out DateOnly date)
        => DateOnly.TryParseExact(
            Path.GetFileNameWithoutExtension(path),
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static long Length(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private static bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}