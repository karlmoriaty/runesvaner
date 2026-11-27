namespace Runesvaner;

/// <summary>How much prompt and response content is captured.</summary>
public enum ContentCaptureMode
{
    /// <summary>No prompt or response content is captured (default).</summary>
    None,

    /// <summary>Only lengths and hashes are captured.</summary>
    Metadata,

    /// <summary>Full prompt and response text is captured. Use with explicit consent.</summary>
    Full
}

/// <summary>Captured prompt and response content according to the capture mode.</summary>
public sealed record RunesvanerContentCapture(
    int PromptLength,
    string? PromptHash,
    string? Prompt,
    int ResponseLength,
    string? ResponseHash,
    string? Response);