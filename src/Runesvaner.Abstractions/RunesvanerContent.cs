namespace Runesvaner;

public enum ContentCaptureMode
{
    None,
    Metadata,
    Full
}

public sealed record RunesvanerContentCapture(
    int PromptLength,
    string? PromptHash,
    string? Prompt,
    int ResponseLength,
    string? ResponseHash,
    string? Response);