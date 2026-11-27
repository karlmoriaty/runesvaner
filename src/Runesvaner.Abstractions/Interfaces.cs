namespace Runesvaner;

public interface IRunesvanerRecorder
{
    bool IsEnabled { get; }

    IRunesvanerRequest StartRequest(RunesvanerRequestInfo info);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}

public interface IRunesvanerRequest : IDisposable
{
    string RequestId { get; }

    void RecordSegment(RunesvanerSegment segment);

    void RecordUsage(RunesvanerUsage usage);

    void RecordContent(RunesvanerContentCapture content);

    void Complete(RunesvanerRequestResult result);
}

public interface IRunesvanerStore
{
    ValueTask WriteAsync(RunesvanerRequestRecord record, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}