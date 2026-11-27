namespace Runesvaner;

/// <summary>Creates request scopes and decides whether collection is active.</summary>
public interface IRunesvanerRecorder
{
    /// <summary>Gets whether collection is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Starts a request scope, or a no-op scope when sampling drops it.</summary>
    IRunesvanerRequest StartRequest(RunesvanerRequestInfo info);

    /// <summary>Flushes pending records to the underlying store.</summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>A single in-flight request being recorded.</summary>
public interface IRunesvanerRequest : IDisposable
{
    /// <summary>Gets the request identifier.</summary>
    string RequestId { get; }

    /// <summary>Adds a latency segment to the request.</summary>
    void RecordSegment(RunesvanerSegment segment);

    /// <summary>Sets the token usage for the request.</summary>
    void RecordUsage(RunesvanerUsage usage);

    /// <summary>Sets captured prompt or response content according to the capture mode.</summary>
    void RecordContent(RunesvanerContentCapture content);

    /// <summary>Completes the request and persists it.</summary>
    void Complete(RunesvanerRequestResult result);
}

/// <summary>Writes completed request records to a storage backend.</summary>
public interface IRunesvanerStore
{
    /// <summary>Writes one request record.</summary>
    ValueTask WriteAsync(RunesvanerRequestRecord record, CancellationToken cancellationToken = default);

    /// <summary>Flushes pending writes.</summary>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}