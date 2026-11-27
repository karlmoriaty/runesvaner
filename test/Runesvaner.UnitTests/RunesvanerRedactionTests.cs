using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class RunesvanerRedactionTests
{
    [Theory]
    [InlineData("Authorization")]
    [InlineData("authorization")]
    [InlineData("api-key")]
    [InlineData("X-Api-Key")]
    [InlineData("Cookie")]
    [InlineData("set-cookie")]
    [InlineData("x-goog-api-key")]
    public void IsSensitiveHeader_RecognizesDenylist(string name)
        => Assert.True(RunesvanerRedaction.IsSensitiveHeader(name));

    [Theory]
    [InlineData("content-type")]
    [InlineData("user-agent")]
    [InlineData("traceparent")]
    public void IsSensitiveHeader_AllowsNonSensitiveHeaders(string name)
        => Assert.False(RunesvanerRedaction.IsSensitiveHeader(name));

    [Fact]
    public void RedactHeaders_ReplacesSensitiveValues()
    {
        var redacted = RunesvanerRedaction.RedactHeaders(
        [
            new KeyValuePair<string, string>("Authorization", "Bearer secret"),
            new KeyValuePair<string, string>("Content-Type", "application/json")
        ]);

        Assert.Equal(RunesvanerRedaction.RedactedValue, redacted["Authorization"]);
        Assert.Equal("application/json", redacted["Content-Type"]);
        Assert.DoesNotContain("secret", string.Join(',', redacted.Values));
    }

    [Fact]
    public void RestrictFile_DoesNotThrowOnCurrentPlatform()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-mode-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, "test");

        try
        {
            RunesvanerFileSecurity.RestrictFile(path);

            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(
                    RunesvanerFileSecurity.OwnerFileMode,
                    File.GetUnixFileMode(path));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}