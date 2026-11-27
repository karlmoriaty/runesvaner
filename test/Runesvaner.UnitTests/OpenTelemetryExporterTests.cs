using System.Diagnostics;
using System.Diagnostics.Metrics;
using Runesvaner;
using Runesvaner.Exporters.OpenTelemetry;

namespace Runesvaner.UnitTests;

public sealed class OpenTelemetryExporterTests
{
    [Fact]
    public async Task Export_EmitsSpanMetricsAndTags()
    {
        var sourceName = $"Runesvaner.Test.{Guid.NewGuid():N}";
        var meterName = sourceName;
        Activity? captured = null;

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => captured = activity
        };
        ActivitySource.AddActivityListener(activityListener);

        var metrics = new List<(string Name, double Value)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, value, _, _) => metrics.Add((instrument.Name, value)));
        meterListener.SetMeasurementEventCallback<long>((instrument, value, _, _) => metrics.Add((instrument.Name, value)));
        meterListener.Start();

        await using var exporter = new RunesvanerOpenTelemetryExporter(
            new ActivitySource(sourceName),
            new Meter(meterName));

        var record = new RunesvanerRequestRecord(
            "runesvaner.request.v1",
            "otel-1",
            "Chat",
            "openai",
            "gpt-5",
            "gpt-5-2026",
            DateTimeOffset.UtcNow,
            1_000,
            500,
            "Ok",
            new RunesvanerUsage(10, 5, 4, null, UsageSource.Provider),
            new[]
            {
                new RunesvanerSegment(SegmentKind.Generation, 0, 1_000, false, SegmentMeasurement.Stopwatch)
            },
            new Dictionary<string, string> { ["feature"] = "test" },
            new CostBreakdown("USD", 0.001m, 0.0001m, 0.002m, 0m, 0.0031m, CostStatus.Known, "test-1"),
            new RunesvanerStreamStats(500, null, 2, 100, 120, true, 25.0, false, false, "stop"));

        exporter.Export(record);

        Assert.NotNull(captured);
        Assert.Equal("chat", captured!.GetTagItem("gen_ai.operation.name"));
        Assert.Equal("openai", captured.GetTagItem("gen_ai.provider.name"));
        Assert.Equal("gpt-5", captured.GetTagItem("gen_ai.request.model"));
        Assert.Equal(10L, captured.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(5L, captured.GetTagItem("gen_ai.usage.output_tokens"));
        Assert.Equal(4L, captured.GetTagItem("runesvaner.usage.cached_input_tokens"));
        Assert.Equal("provider", captured.GetTagItem("runesvaner.usage.source"));
        Assert.Equal(1, captured.Events.Count(activityEvent => activityEvent.Name == "segment"));

        Assert.Contains(metrics, metric =>
            metric.Name == "runesvaner.ai.request.duration" && Math.Abs(metric.Value - 0.001) < 0.000001);
        Assert.Contains(metrics, metric =>
            metric.Name == "runesvaner.ai.ttft" && Math.Abs(metric.Value - 0.0005) < 0.000001);
        Assert.Contains(metrics, metric =>
            metric.Name == "runesvaner.ai.tokens" && metric.Value == 10);
        Assert.Contains(metrics, metric =>
            metric.Name == "runesvaner.ai.cost" && Math.Abs(metric.Value - 0.0031) < 0.000001);
    }
}