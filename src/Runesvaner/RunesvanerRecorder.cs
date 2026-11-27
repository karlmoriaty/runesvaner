using System.Diagnostics;

namespace Runesvaner;

public sealed class RunesvanerRecorder : IRunesvanerRecorder
{
    private readonly IRunesvanerStore _store;
    private readonly RunesvanerOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly string _sessionId;

    public RunesvanerRecorder(
        IRunesvanerStore store,
        RunesvanerOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _store = store;
        _options = options ?? new RunesvanerOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sessionId = _options.SessionId ?? Guid.NewGuid().ToString("N");

        if (_options.SamplingRate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.SamplingRate,
                "SamplingRate must be between 0 and 1.");
        }
    }

    public string SessionId => _sessionId;

    public bool IsEnabled => _options.Enabled;

    public IRunesvanerRequest StartRequest(RunesvanerRequestInfo info)
    {
        if (!_options.Enabled ||
            (_options.SamplingRate < 1 && Random.Shared.NextDouble() >= _options.SamplingRate))
        {
            return NoOpRequest.Instance;
        }

        return new RunesvanerRequestScope(
            RequestId(),
            _sessionId,
            info,
            _store,
            _options,
            _timeProvider);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => _store.FlushAsync(cancellationToken);

    private static string RequestId() => Guid.NewGuid().ToString("N");

    private sealed class NoOpRequest : IRunesvanerRequest
    {
        public static NoOpRequest Instance { get; } = new();

        public string RequestId { get; } = Guid.NewGuid().ToString("N");

        public void RecordSegment(RunesvanerSegment segment)
        {
        }

        public void RecordUsage(RunesvanerUsage usage)
        {
        }

        public void RecordContent(RunesvanerContentCapture content)
        {
        }

        public void Complete(RunesvanerRequestResult result)
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class RunesvanerRequestScope : IRunesvanerRequest
    {
        private readonly RunesvanerRequestInfo _info;
        private readonly IRunesvanerStore _store;
        private readonly RunesvanerOptions _options;
        private readonly string _sessionId;
        private readonly DateTimeOffset _startedAtUtc;
        private readonly long _startTimestamp;
        private readonly object _segmentGate = new();
        private readonly List<RunesvanerSegment> _segments = new(16);
        private RunesvanerUsage? _usage;
        private RunesvanerContentCapture? _content;
        private bool _completed;

        public RunesvanerRequestScope(
            string requestId,
            string sessionId,
            RunesvanerRequestInfo info,
            IRunesvanerStore store,
            RunesvanerOptions options,
            TimeProvider timeProvider)
        {
            RequestId = requestId;
            _sessionId = sessionId;
            _info = info;
            _store = store;
            _options = options;
            _startedAtUtc = timeProvider.GetUtcNow();
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public string RequestId { get; }

        public void RecordSegment(RunesvanerSegment segment)
        {
            lock (_segmentGate)
            {
                if (!_completed)
                {
                    _segments.Add(segment);
                }
            }
        }

        public void RecordUsage(RunesvanerUsage usage)
        {
            lock (_segmentGate)
            {
                if (!_completed)
                {
                    _usage = usage;
                }
            }
        }

        public void RecordContent(RunesvanerContentCapture content)
        {
            lock (_segmentGate)
            {
                if (!_completed)
                {
                    _content = content;
                }
            }
        }

        public void Complete(RunesvanerRequestResult result)
        {
            RunesvanerUsage? usage;
            RunesvanerContentCapture? content;
            RunesvanerSegment[] segments;

            lock (_segmentGate)
            {
                if (_completed)
                {
                    return;
                }

                _completed = true;
                usage = _usage;
                content = _content;
                segments = _segments.ToArray();
            }

            var cost = CalculateCost(result.ResponseModel, usage);

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
                usage,
                segments,
                _info.Tags is null ? null : new Dictionary<string, string>(_info.Tags),
                cost,
                result.Stream,
                _sessionId,
                content,
                result.Attempt,
                result.Attempt > 1);

            try
            {
                _store.WriteAsync(record).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Collection must never break the business request.
            }
        }

        private CostBreakdown? CalculateCost(
            string? responseModel,
            RunesvanerUsage? usage)
        {
            if (usage is not { } currentUsage || _options.PricingTable is not { } table)
            {
                return null;
            }

            var provider = _info.Provider ?? _options.Provider;
            var model = _info.RequestModel ?? responseModel;
            PriceEntry? entry = null;

            if (!string.IsNullOrEmpty(provider) &&
                !string.IsNullOrEmpty(model) &&
                table.TryGet(provider, model, out var found))
            {
                entry = found;
            }

            return RunesvanerCostEngine.Calculate(
                currentUsage,
                entry,
                table.Version,
                table.Currency);
        }

        public void Dispose()
            => Complete(new RunesvanerRequestResult(
                RequestStatus.Unknown,
                Microseconds(Stopwatch.GetElapsedTime(_startTimestamp)),
                null));

        private static long Microseconds(TimeSpan elapsed) => elapsed.Ticks / 10;
    }
}