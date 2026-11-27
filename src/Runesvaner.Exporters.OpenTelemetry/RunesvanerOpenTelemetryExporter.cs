using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Runesvaner.Exporters.OpenTelemetry;

/// <summary>Exports Runesvaner records as OpenTelemetry spans and metrics.</summary>
public sealed class RunesvanerOpenTelemetryExporter : IAsyncDisposable
{
    /// <summary>The ActivitySource and Meter name used by the exporter.</summary>
    public const string SourceName = "Runesvaner.AI";

    private readonly TracerProvider? _tracerProvider;
    private readonly MeterProvider? _meterProvider;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _ttft;
    private readonly Counter<long> _tokens;
    private readonly Counter<double> _cost;
    private readonly Counter<long> _requests;

    /// <summary>Creates an exporter over custom ActivitySource and Meter instances.</summary>
    public RunesvanerOpenTelemetryExporter(ActivitySource? activitySource = null, Meter? meter = null)
    {
        _activitySource = activitySource ?? new ActivitySource(SourceName);
        _meter = meter ?? new Meter(SourceName);
        _requestDuration = _meter.CreateHistogram<double>("runesvaner.ai.request.duration", "s");
        _ttft = _meter.CreateHistogram<double>("runesvaner.ai.ttft", "s");
        _tokens = _meter.CreateCounter<long>("runesvaner.ai.tokens", "{token}");
        _cost = _meter.CreateCounter<double>("runesvaner.ai.cost", "{usd}");
        _requests = _meter.CreateCounter<long>("runesvaner.ai.requests", "{request}");
    }

    private RunesvanerOpenTelemetryExporter(
        TracerProvider tracerProvider,
        MeterProvider meterProvider)
        : this()
    {
        _tracerProvider = tracerProvider;
        _meterProvider = meterProvider;
    }

    /// <summary>Creates an OTLP exporter with the supplied endpoint and protocol.</summary>
    public static RunesvanerOpenTelemetryExporter Create(
        Action<RunesvanerOpenTelemetryOptions>? configure = null)
    {
        var options = new RunesvanerOpenTelemetryOptions();
        configure?.Invoke(options);

        var resource = ResourceBuilder.CreateDefault().AddService(options.ServiceName);

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resource)
            .AddSource(SourceName)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = options.Endpoint;
                otlp.Protocol = options.Protocol;
            })
            .Build()
            ?? throw new InvalidOperationException("Failed to create the OpenTelemetry TracerProvider.");

        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resource)
            .AddMeter(SourceName)
            .AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = options.Endpoint;
                otlp.Protocol = options.Protocol;
            })
            .Build()
            ?? throw new InvalidOperationException("Failed to create the OpenTelemetry MeterProvider.");

        return new RunesvanerOpenTelemetryExporter(tracerProvider, meterProvider);
    }

    /// <summary>Emits spans, segments, metrics, and usage for one record.</summary>
    public void Export(RunesvanerRequestRecord record)
    {
        using (var activity = _activitySource.StartActivity(record.Operation, ActivityKind.Client))
        {
            if (activity is not null)
            {
                activity.SetStartTime(record.StartedAtUtc.UtcDateTime);
                activity.SetEndTime(record.StartedAtUtc.UtcDateTime.AddTicks(record.DurationUs * 10));

                foreach (var tag in BuildTags(record))
                {
                    activity.SetTag(tag.Key, tag.Value);
                }

                foreach (var segment in record.Segments)
                {
                    var segmentTags = new ActivityTagsCollection
                    {
                        { "runesvaner.segment.kind", segment.Kind.ToString().ToLowerInvariant() },
                        { "runesvaner.segment.start_offset_us", segment.StartOffsetUs },
                        { "runesvaner.segment.duration_us", segment.DurationUs },
                        { "runesvaner.segment.estimated", segment.Estimated },
                        { "runesvaner.segment.measurement", segment.Measurement.ToString().ToLowerInvariant() },
                        { "runesvaner.segment.lane", segment.Lane }
                    };

                    activity.AddEvent(new ActivityEvent(
                        "segment",
                        record.StartedAtUtc.AddTicks(segment.StartOffsetUs * 10),
                        segmentTags));
                }

                activity.Stop();
            }
        }

        _requests.Add(1, BuildTags(record));
        _requestDuration.Record(record.DurationUs / 1_000_000.0, BuildTags(record));

        if (record.TtftUs is { } ttftUs)
        {
            _ttft.Record(ttftUs / 1_000_000.0, BuildTags(record));
        }

        if (record.Usage is { } usage)
        {
            AddTokens(usage.InputTokens, "input", record);
            AddTokens(usage.OutputTokens, "output", record);
            AddTokens(usage.CachedInputTokens, "cached", record);
            AddTokens(usage.ReasoningTokens, "reasoning", record);
        }

        if (record.Cost is { Status: CostStatus.Known, Total: { } total } cost)
        {
            var costTags = BuildTags(record);
            costTags.Add("runesvaner.cost.currency", cost.Currency);
            _cost.Add((double)total, costTags);
        }
    }

    /// <summary>Flushes and disposes the tracer and meter providers.</summary>
    public ValueTask DisposeAsync()
    {
        _tracerProvider?.Dispose();
        _meterProvider?.Dispose();
        _activitySource.Dispose();
        _meter.Dispose();
        return ValueTask.CompletedTask;
    }

    private void AddTokens(long? value, string type, RunesvanerRequestRecord record)
    {
        if (value is null)
        {
            return;
        }

        var tags = BuildTags(record);
        tags.Add("gen_ai.token.type", type);
        _tokens.Add(value.Value, tags);
    }

    private static TagList BuildTags(RunesvanerRequestRecord record)
    {
        var tags = new TagList
        {
            { "gen_ai.operation.name", record.Operation.ToLowerInvariant() },
            { "runesvaner.request_id", record.RequestId },
            { "runesvaner.status", record.Status },
            { "runesvaner.duration_us", record.DurationUs }
        };

        tags = AddIfPresent(tags, "gen_ai.provider.name", record.Provider);
        tags = AddIfPresent(tags, "gen_ai.request.model", record.RequestModel);
        tags = AddIfPresent(tags, "gen_ai.response.model", record.ResponseModel);

        if (record.TtftUs is { } ttftUs)
        {
            tags.Add("runesvaner.ttft_us", ttftUs);
        }

        if (record.Usage is { } usage)
        {
            if (usage.InputTokens is { } inputTokens)
            {
                tags.Add("gen_ai.usage.input_tokens", inputTokens);
            }

            if (usage.OutputTokens is { } outputTokens)
            {
                tags.Add("gen_ai.usage.output_tokens", outputTokens);
            }

            if (usage.CachedInputTokens is { } cachedInputTokens)
            {
                tags.Add("runesvaner.usage.cached_input_tokens", cachedInputTokens);
            }

            if (usage.ReasoningTokens is { } reasoningTokens)
            {
                tags.Add("runesvaner.usage.reasoning_tokens", reasoningTokens);
            }

            tags.Add("runesvaner.usage.source", usage.Source.ToString().ToLowerInvariant());
        }

        if (record.Cost is { } cost)
        {
            tags.Add("runesvaner.cost.status", cost.Status.ToString().ToLowerInvariant());
            tags.Add("runesvaner.cost.currency", cost.Currency);
            tags.Add("runesvaner.price_table.version", cost.PriceTableVersion);

            if (cost.Total is { } total)
            {
                tags.Add("runesvaner.cost.total", (double)total);
            }
        }

        if (record.Stream is { } stream)
        {
            tags.Add("runesvaner.stream.chunk_count", stream.ChunkCount);
            tags.Add("runesvaner.stream.itl_estimated", stream.ItlEstimated);
            tags.Add("runesvaner.stream.cancelled", stream.Cancelled);
            tags.Add("runesvaner.stream.interrupted", stream.Interrupted);

            if (stream.ItlP95Us is { } itlP95Us)
            {
                tags.Add("runesvaner.stream.itl_p95_us", itlP95Us);
            }

            if (stream.TokensPerSecond is { } tokensPerSecond)
            {
                tags.Add("runesvaner.stream.tokens_per_second", tokensPerSecond);
            }

            tags = AddIfPresent(tags, "runesvaner.stream.finish_reason", stream.FinishReason);
        }

        if (record.Tags is { } customTags)
        {
            foreach (var tag in customTags)
            {
                tags.Add($"runesvaner.tag.{tag.Key}", tag.Value);
            }
        }

        return tags;
    }

    private static TagList AddIfPresent(TagList tags, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            tags.Add(key, value);
        }

        return tags;
    }
}