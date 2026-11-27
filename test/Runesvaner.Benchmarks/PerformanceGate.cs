using System.Diagnostics;
using System.Globalization;
using Runesvaner;

namespace Runesvaner.Benchmarks;

public static class PerformanceGate
{
    private const double DefaultP95BudgetMicroseconds = 500;
    private const double DefaultAllocationBudgetBytes = 2 * 1024;
    private const int DefaultWarmup = 2_000;
    private const int DefaultIterations = 20_000;

    public static int Run(int warmup = DefaultWarmup, int iterations = DefaultIterations)
    {
        var p95Budget = ReadBudget("RUNESVANER_PERF_P95_US", DefaultP95BudgetMicroseconds);
        var allocationBudget = ReadBudget("RUNESVANER_PERF_ALLOC_BYTES", DefaultAllocationBudgetBytes);

        var recorder = new RunesvanerRecorder(
            new NullStore(),
            new RunesvanerOptions { Enabled = true });

        for (var i = 0; i < warmup; i++)
        {
            Capture(recorder);
        }

        var samples = new double[iterations];
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < iterations; i++)
        {
            var started = Stopwatch.GetTimestamp();
            Capture(recorder);
            samples[i] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);

        var p50 = Percentile(samples, 0.50);
        var p95 = Percentile(samples, 0.95);
        var p99 = Percentile(samples, 0.99);
        var allocatedPerOperation = (double)allocatedBytes / iterations;

        Console.WriteLine("Performance gate: recorder, no storage (B02)");
        Console.WriteLine($"Iterations: {iterations}; warmup: {warmup}");
        Console.WriteLine($"P50: {p50:F2} us");
        Console.WriteLine($"P95: {p95:F2} us (budget {p95Budget:F2} us)");
        Console.WriteLine($"P99: {p99:F2} us");
        Console.WriteLine(
            $"Allocated: {allocatedPerOperation / 1024:F2} KB/op (budget {allocationBudget / 1024:F2} KB/op)");

        var failures = new List<string>();

        if (p95 > p95Budget)
        {
            failures.Add($"P95 {p95:F2} us exceeds {p95Budget:F2} us");
        }

        if (allocatedPerOperation > allocationBudget)
        {
            failures.Add(
                $"allocation {allocatedPerOperation / 1024:F2} KB/op exceeds {allocationBudget / 1024:F2} KB/op");
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("Result: PASS");
            return 0;
        }

        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"Result: FAIL - {failure}");
        }

        return 1;
    }

    private static void Capture(IRunesvanerRecorder recorder)
    {
        using var request = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "gate",
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
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static double ReadBudget(string name, double fallback)
        => double.TryParse(
            Environment.GetEnvironmentVariable(name),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var value) && value > 0
            ? value
            : fallback;

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