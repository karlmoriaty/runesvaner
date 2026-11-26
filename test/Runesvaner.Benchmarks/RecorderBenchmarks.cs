using BenchmarkDotNet.Attributes;
using Runesvaner;

namespace Runesvaner.Benchmarks;

[MemoryDiagnoser]
public class RecorderBenchmarks
{
    private RunesvanerRecorder _recorder = null!;

    [GlobalSetup]
    public void Setup()
        => _recorder = new RunesvanerRecorder(
            new NullStore(),
            new RunesvanerOptions { Enabled = true });

    [Benchmark]
    public void RecordRequest()
    {
        using var request = _recorder.StartRequest(new RunesvanerRequestInfo(
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