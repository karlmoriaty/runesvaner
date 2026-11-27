namespace Runesvaner;

/// <summary>Logical AI operation recorded for a request.</summary>
public enum RunesvanerOperation
{
    /// <summary>Chat completion.</summary>
    Chat,

    /// <summary>Retrieval-augmented generation pipeline.</summary>
    Rag,

    /// <summary>Embedding generation.</summary>
    Embeddings,

    /// <summary>Tool or function invocation.</summary>
    Tool,

    /// <summary>Vector search or retrieval.</summary>
    VectorSearch,

    /// <summary>Reranking.</summary>
    Rerank,

    /// <summary>Operation is not known.</summary>
    Unknown
}

/// <summary>Latency segment kind inside a request waterfall.</summary>
public enum SegmentKind
{
    /// <summary>In-application queueing before the call.</summary>
    Queue,

    /// <summary>Authentication work.</summary>
    Auth,

    /// <summary>Connection establishment.</summary>
    Connect,

    /// <summary>TLS handshake.</summary>
    Tls,

    /// <summary>Request upload.</summary>
    Upload,

    /// <summary>Provider-side queueing or processing before the response.</summary>
    ProviderQueue,

    /// <summary>Time to first token.</summary>
    Ttft,

    /// <summary>Response generation or download.</summary>
    Generation,

    /// <summary>Embedding generation segment.</summary>
    Embedding,

    /// <summary>Tool invocation segment.</summary>
    Tool,

    /// <summary>Vector search segment.</summary>
    VectorSearch,

    /// <summary>Serialization segment.</summary>
    Serialize,

    /// <summary>Retry backoff wait.</summary>
    RetryWait,

    /// <summary>Cache lookup segment.</summary>
    CacheLookup,

    /// <summary>Residual time not covered by measured segments.</summary>
    Unaccounted,

    /// <summary>Parallel overlap between segments.</summary>
    Overlap
}

/// <summary>How a segment duration was obtained.</summary>
public enum SegmentMeasurement
{
    /// <summary>Measured with a stopwatch.</summary>
    Stopwatch,

    /// <summary>Measured from HTTP diagnostics.</summary>
    HttpDiagnostic,

    /// <summary>Derived from other measurements.</summary>
    Derived,

    /// <summary>Residual calculation.</summary>
    Residual
}

/// <summary>Final status of a recorded request.</summary>
public enum RequestStatus
{
    /// <summary>Request succeeded.</summary>
    Ok,

    /// <summary>Request failed.</summary>
    Error,

    /// <summary>Request was cancelled by the caller.</summary>
    Cancelled,

    /// <summary>Request timed out.</summary>
    Timeout,

    /// <summary>Status is unknown.</summary>
    Unknown
}

/// <summary>Source of token usage values.</summary>
public enum UsageSource
{
    /// <summary>Reported by the provider.</summary>
    Provider,

    /// <summary>Estimated locally.</summary>
    Estimated,

    /// <summary>Mix of provider and estimated values.</summary>
    Mixed,

    /// <summary>Source is unknown.</summary>
    Unknown
}

/// <summary>Metadata supplied when a recorder request scope is created.</summary>
public readonly record struct RunesvanerRequestInfo(
    RunesvanerOperation Operation,
    string? Provider,
    string? RequestModel,
    string? Endpoint,
    IReadOnlyDictionary<string, string>? Tags = null);

/// <summary>One measured interval inside a request.</summary>
public readonly record struct RunesvanerSegment(
    SegmentKind Kind,
    long StartOffsetUs,
    long DurationUs,
    bool Estimated,
    SegmentMeasurement Measurement,
    int Lane = 0);

/// <summary>Token usage for one request.</summary>
public readonly record struct RunesvanerUsage(
    long? InputTokens,
    long? OutputTokens,
    long? CachedInputTokens,
    long? ReasoningTokens,
    UsageSource Source);

/// <summary>Outcome of a request, including attempt and streaming details.</summary>
public readonly record struct RunesvanerRequestResult(
    RequestStatus Status,
    long DurationUs,
    long? TtftUs,
    int Attempt = 1,
    string? ResponseModel = null,
    RunesvanerStreamStats? Stream = null);