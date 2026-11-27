using System.Diagnostics;

namespace Runesvaner;

/// <summary>Creates request scopes, applies sampling, tracks budgets, and persists records.</summary>
public sealed class RunesvanerRecorder : IRunesvanerRecorder
{
    private readonly IRunesvanerStore _store;
    private readonly RunesvanerOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly RunesvanerBudgetTracker? _budget;
    private readonly string _sessionId;

    /// <summary>Creates a recorder over the supplied store, options, clock, and budget tracker.</summary>
    public RunesvanerRecorder(
        IRunesvanerStore store,
        RunesvanerOptions? options = null,
        TimeProvider? timeProvider = null,
        RunesvanerBudgetTracker? budget = null)
    {
        _store = store;
        _options = options ?? new RunesvanerOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _budget = budget;
        _sessionId = _options.SessionId ?? Guid.NewGuid().ToString("N");

        if (_options.SamplingRate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.SamplingRate,
                "SamplingRate must be between 0 and 1.");
        }

        if (_options.TailSampling.SampleRate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.TailSampling.SampleRate,
                "TailSampling.SampleRate must be between 0 and 1.");
        }

        if (_options.TailSampling.SlowThresholdUs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.TailSampling.SlowThresholdUs,
                "TailSampling.SlowThresholdUs must not be negative.");
        }
    }

    /// <summary>Gets the session identifier used by this recorder.</summary>
    public string SessionId => _sessionId;

    /// <summary>Gets whether collection is enabled.</summary>
    public bool IsEnabled => _options.Enabled;

    /// <summary>Starts a request scope, or a no-op scope when disabled or sampled out.</summary>
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
            _timeProvider,
            _budget);
    }

    /// <summary>Flushes pending records to the underlying store.</summary>
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
        private readonly RunesvanerBudgetTracker? _budget;
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
            TimeProvider timeProvider,
            RunesvanerBudgetTracker? budget)
        {
            RequestId = requestId;
            _sessionId = sessionId;
            _info = info;
            _store = store;
            _options = options;
            _budget = budget;
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
            var (keep, weight) = EvaluateTailSampling(result, cost);

            if (!keep)
            {
                return;
            }

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
                result.Attempt > 1,
                weight);

            try
            {
                _budget?.Observe(record);
            }
            catch (Exception)
            {
                // Budget reporting must never break the business request.
            }

            try
            {
                _store.WriteAsync(record).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Collection must never break the business request.
            }
        }

        private (bool Keep, double Weight) EvaluateTailSampling(
            RunesvanerRequestResult result,
            CostBreakdown? cost)
        {
            var tail = _options.TailSampling;

            if (!tail.Enabled)
            {
                return (true, 1);
            }

            if (result.Status != RequestStatus.Ok)
            {
                return (true, 1);
            }

            if (tail.SlowThresholdUs > 0 && result.DurationUs >= tail.SlowThresholdUs)
            {
                return (true, 1);
            }

            if (tail.OverBudgetUsd is { } limit &&
                cost?.Total is { } total &&
                total >= limit)
            {
                return (true, 1);
            }

            if (tail.SampleRate >= 1)
            {
                return (true, 1);
            }

            if (tail.SampleRate <= 0)
            {
                return (false, 1);
            }

            return Random.Shared.NextDouble() < tail.SampleRate
                ? (true, 1 / tail.SampleRate)
                : (false, 1);
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