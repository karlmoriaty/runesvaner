namespace Runesvaner;

/// <summary>Redaction helpers for sensitive HTTP headers and content.</summary>
public static class RunesvanerRedaction
{
    /// <summary>Replacement value used for sensitive headers.</summary>
    public const string RedactedValue = "[redacted]";

    /// <summary>Returns whether a header is on the sensitive denylist.</summary>
    public static bool IsSensitiveHeader(string name)
        => SensitiveHeaders.Contains(name);

    /// <summary>Returns a copy of the headers with sensitive values redacted.</summary>
    public static IReadOnlyDictionary<string, string> RedactHeaders(
        IEnumerable<KeyValuePair<string, string>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            result[header.Key] = IsSensitiveHeader(header.Key)
                ? RedactedValue
                : header.Value;
        }

        return result;
    }

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "proxy-authorization",
        "api-key",
        "x-api-key",
        "cookie",
        "set-cookie",
        "x-goog-api-key",
        "x-amz-security-token"
    };
}

/// <summary>Unix file permission helpers for local Runesvaner data.</summary>
public static class RunesvanerFileSecurity
{
    /// <summary>Owner-only file mode (0600).</summary>
    public const UnixFileMode OwnerFileMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>Owner-only directory mode (0700).</summary>
    public const UnixFileMode OwnerDirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    /// <summary>Restricts a file to owner read/write on Unix; no-op on Windows.</summary>
    public static void RestrictFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (OperatingSystem.IsWindows())
        {
            return;
        }

        TrySetMode(path, OwnerFileMode);
    }

    /// <summary>Restricts a directory to owner read/write/execute on Unix; no-op on Windows.</summary>
    public static void RestrictDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (OperatingSystem.IsWindows())
        {
            return;
        }

        TrySetMode(path, OwnerDirectoryMode);
    }

    private static void TrySetMode(string path, UnixFileMode mode)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, mode);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}