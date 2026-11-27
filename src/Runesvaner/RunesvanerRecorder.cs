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
    }

    public string SessionId => _sessionId;

    public bool IsEnabled => _options.Enabled;

    public IRunesvanerRequest StartRequest(RunesvanerRequestInfo info)
        => new RunesvanerRequestScope(RequestId(), _sessionId, info, _store, _options, _timeProvider);

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => _store.FlushAsync(cancellationToken);

    private static string RequestId() => Guid.NewGuid().ToString("N");

    private sealed class RunesvanerRequestScope : IRunesvanerRequest
    {
        private readonly RunesvanerRequestInfo _info;
        private readonly IRunesvanerStore _store;
        private readonly RunesvanerOptions _options;
        private readonly string _sessionId;
        private readonly DateTimeOffset _startedAtUtc;
        private readonly long _startTimestamp;
        private readonly List<RunesvanerSegment> _segments = new(16);
        private RunesvanerUsage? _usage;
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

            var cost = CalculateCost(result.ResponseModel);

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
                _info.Tags is null ? null : new Dictionary<string, string>(_info.Tags),
                cost,
                result.Stream,
                _sessionId);

            try
            {
                _store.WriteAsync(record).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Collection must never break the business request.
            }
        }

        private CostBreakdown? CalculateCost(string? responseModel)
        {
            if (_usage is not { } usage || _options.PricingTable is not { } table)
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

            return RunesvanerCostEngine.Calculate(usage, entry, table.Version, table.Currency);
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