namespace Runesvaner;

public enum RunesvanerOperation
{
    Chat,
    Rag,
    Embeddings,
    Tool,
    VectorSearch,
    Rerank,
    Unknown
}

public enum SegmentKind
{
    Queue,
    Auth,
    Connect,
    Tls,
    Upload,
    ProviderQueue,
    Ttft,
    Generation,
    Embedding,
    Tool,
    VectorSearch,
    Serialize,
    RetryWait,
    CacheLookup,
    Unaccounted,
    Overlap
}

public enum SegmentMeasurement
{
    Stopwatch,
    HttpDiagnostic,
    Derived,
    Residual
}

public enum RequestStatus
{
    Ok,
    Error,
    Cancelled,
    Timeout,
    Unknown
}

public enum UsageSource
{
    Provider,
    Estimated,
    Mixed,
    Unknown
}

public readonly record struct RunesvanerRequestInfo(
    RunesvanerOperation Operation,
    string? Provider,
    string? RequestModel,
    string? Endpoint,
    IReadOnlyDictionary<string, string>? Tags = null);

public readonly record struct RunesvanerSegment(
    SegmentKind Kind,
    long StartOffsetUs,
    long DurationUs,
    bool Estimated,
    SegmentMeasurement Measurement,
    int Lane = 0);

public readonly record struct RunesvanerUsage(
    long? InputTokens,
    long? OutputTokens,
    long? CachedInputTokens,
    long? ReasoningTokens,
    UsageSource Source);

public readonly record struct RunesvanerRequestResult(
    RequestStatus Status,
    long DurationUs,
    long? TtftUs,
    int Attempt = 1,
    string? ResponseModel = null,
    RunesvanerStreamStats? Stream = null);