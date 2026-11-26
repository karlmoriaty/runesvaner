using System.Diagnostics;
using Runesvaner;

namespace Runesvaner.Benchmarks;

public static class P95Runner
{
    public static void Run(int iterations = 10_000)
    {
        var recorder = new RunesvanerRecorder(
            new NullStore(),
            new RunesvanerOptions { Enabled = true });

        var samples = new double[iterations];

        for (var i = 0; i < iterations; i++)
        {
            var started = Stopwatch.GetTimestamp();

            using var request = recorder.StartRequest(new RunesvanerRequestInfo(
                RunesvanerOperation.Chat,
                "bench",
                "echo-1",
                null));

            request.RecordSegment(new RunesvanerSegment(
                SegmentKind.Generation,
                0,
                1000,
                false,
                SegmentMeasurement.Stopwatch));

            request.RecordUsage(new RunesvanerUsage(10, 5, null, null, UsageSource.Provider));
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1000, null));

            samples[i] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
        }

        Array.Sort(samples);

        Console.WriteLine($"Recorder P50: {Percentile(samples, 0.50):F2} us");
        Console.WriteLine($"Recorder P95: {Percentile(samples, 0.95):F2} us");
        Console.WriteLine($"Recorder P99: {Percentile(samples, 0.99):F2} us");
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private sealed class NullStore : IRunesvanerStore
    {
        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}