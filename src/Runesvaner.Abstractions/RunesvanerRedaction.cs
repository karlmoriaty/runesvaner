namespace Runesvaner;

public static class RunesvanerRedaction
{
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

    public const string RedactedValue = "[redacted]";

    public static bool IsSensitiveHeader(string name)
        => SensitiveHeaders.Contains(name);

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
}

public static class RunesvanerFileSecurity
{
    public const UnixFileMode OwnerFileMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite;

    public const UnixFileMode OwnerDirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    public static void RestrictFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (OperatingSystem.IsWindows())
        {
            return;
        }

        TrySetMode(path, OwnerFileMode);
    }

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