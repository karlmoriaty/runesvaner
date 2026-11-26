namespace Runesvaner;

public sealed record RunesvanerStreamStats(
    long? TtftUs,
    long? TtfbUs,
    int ChunkCount,
    long? ItlP50Us,
    long? ItlP95Us,
    bool ItlEstimated,
    double? TokensPerSecond,
    bool Cancelled,
    bool Interrupted,
    string? FinishReason);