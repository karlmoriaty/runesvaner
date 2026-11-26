using System.Diagnostics;
using Runesvaner;
using Runesvaner.Storage.Jsonl;

var options = new RunesvanerOptions
{
    ApplicationName = "sample-rag",
    StorageMode = StorageMode.Jsonl,
    StoragePath = ".runesvaner"
};

var store = new JsonlRunesvanerStore(options.StoragePath);
var recorder = new RunesvanerRecorder(store, options);

using var request = recorder.StartRequest(new RunesvanerRequestInfo(
    RunesvanerOperation.Rag,
    "sample-rag",
    "echo-1",
    null,
    new Dictionary<string, string> { ["feature"] = "rag.sample" }));

var started = Stopwatch.GetTimestamp();
var offsetUs = 0L;

var embedding = await MeasureAsync(() => Task.Delay(12));
request.RecordSegment(new RunesvanerSegment(
    SegmentKind.Embedding,
    offsetUs,
    ToMicroseconds(embedding),
    false,
    SegmentMeasurement.Stopwatch));
offsetUs += ToMicroseconds(embedding);

var retrieval = await MeasureAsync(() => Task.Delay(8));
request.RecordSegment(new RunesvanerSegment(
    SegmentKind.VectorSearch,
    offsetUs,
    ToMicroseconds(retrieval),
    false,
    SegmentMeasurement.Stopwatch));
offsetUs += ToMicroseconds(retrieval);

var generation = await MeasureAsync(() => Task.Delay(20));
request.RecordSegment(new RunesvanerSegment(
    SegmentKind.Generation,
    offsetUs,
    ToMicroseconds(generation),
    false,
    SegmentMeasurement.Stopwatch));

request.RecordUsage(new RunesvanerUsage(128, 24, 96, null, UsageSource.Estimated));
request.Complete(new RunesvanerRequestResult(
    RequestStatus.Ok,
    ToMicroseconds(Stopwatch.GetElapsedTime(started)),
    null));

await recorder.FlushAsync();

Console.WriteLine("RAG sample captured: embedding + vector search + generation.");
Console.WriteLine($"Data: {Path.GetFullPath(options.StoragePath)}");
Console.WriteLine("Run: dotnet run --project src/Runesvaner.Cli -c Release -- report");

static async Task<TimeSpan> MeasureAsync(Func<Task> action)
{
    var stopwatch = Stopwatch.StartNew();
    await action();
    stopwatch.Stop();
    return stopwatch.Elapsed;
}

static long ToMicroseconds(TimeSpan elapsed) => elapsed.Ticks / 10;