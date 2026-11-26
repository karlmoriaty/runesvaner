using System.Diagnostics;

namespace Runesvaner;

public sealed class RunesvanerRecorder : IRunesvanerRecorder
{
    private readonly IRunesvanerStore _store;
    private readonly RunesvanerOptions _options;
    private readonly TimeProvider _timeProvider;

    public RunesvanerRecorder(
        IRunesvanerStore store,
        RunesvanerOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _store = store;
        _options = options ?? new RunesvanerOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsEnabled => _options.Enabled;

    public IRunesvanerRequest StartRequest(RunesvanerRequestInfo info)
        => new RunesvanerRequestScope(RequestId(), info, _store, _timeProvider);

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => _store.FlushAsync(cancellationToken);

    private static string RequestId() => Guid.NewGuid().ToString("N");

    private sealed class RunesvanerRequestScope : IRunesvanerRequest
    {
        private readonly RunesvanerRequestInfo _info;
        private readonly IRunesvanerStore _store;
        private readonly DateTimeOffset _startedAtUtc;
        private readonly long _startTimestamp;
        private readonly List<RunesvanerSegment> _segments = new(16);
        private RunesvanerUsage? _usage;
        private bool _completed;

        public RunesvanerRequestScope(
            string requestId,
            RunesvanerRequestInfo info,
            IRunesvanerStore store,
            TimeProvider timeProvider)
        {
            RequestId = requestId;
            _info = info;
            _store = store;
            _startedAtUtc = timeProvider.GetUtcNow();
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public string RequestId { get; }

        public void RecordSegment(RunesvanerSegment segment)
        {
            if (!_completed)
            {
                _segments.Add(segment);
            }
        }

        public void RecordUsage(RunesvanerUsage usage)
        {
            if (!_completed)
            {
                _usage = usage;
            }
        }

        public void Complete(RunesvanerRequestResult result)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;

            var record = new RunesvanerRequestRecord(
                "runesvaner.request.v1",
                RequestId,
                _info.Operation.ToString(),
                _info.Provider,
                _info.RequestModel,
                result.ResponseModel,
                _startedAtUtc,
                result.DurationUs,
                result.TtftUs,
                result.Status.ToString(),
                _usage,
                _segments.ToArray(),
                _info.Tags is null ? null : new Dictionary<string, string>(_info.Tags));

            try
            {
                _store.WriteAsync(record).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Collection must never break the business request.
            }
        }

        public void Dispose()
        {
            if (_completed)
            {
                return;
            }

            Complete(new RunesvanerRequestResult(
                RequestStatus.Unknown,
                Microseconds(Stopwatch.GetElapsedTime(_startTimestamp)),
                null));
        }

        private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
    }
}