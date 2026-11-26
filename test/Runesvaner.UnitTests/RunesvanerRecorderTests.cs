using Runesvaner;

namespace Runesvaner.UnitTests;

public sealed class RunesvanerRecorderTests
{
    [Fact]
    public void Complete_WritesRecordWithSegmentsAndUsage()
    {
        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions { Enabled = true });

        using var request = recorder.StartRequest(new RunesvanerRequestInfo(
            RunesvanerOperation.Chat,
            "test",
            "echo-1",
            null));

        request.RecordSegment(new RunesvanerSegment(
            SegmentKind.Generation,
            0,
            1234,
            false,
            SegmentMeasurement.Stopwatch));

        request.RecordUsage(new RunesvanerUsage(3, 4, null, null, UsageSource.Provider));
        request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 1234, null));

        var record = Assert.Single(store.Records);
        Assert.Equal("runesvaner.request.v1", record.Schema);
        Assert.Equal("Chat", record.Operation);
        Assert.Equal("Ok", record.Status);
        Assert.Equal(1234L, record.DurationUs);
        Assert.Single(record.Segments);
        Assert.True(record.Usage?.InputTokens == 3);
        Assert.True(record.Usage?.OutputTokens == 4);
    }

    [Fact]
    public void Dispose_WithoutComplete_WritesUnknownRecord()
    {
        var store = new InMemoryStore();
        var recorder = new RunesvanerRecorder(store, new RunesvanerOptions());

        using (recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   null,
                   null,
                   null)))
        {
        }

        var record = Assert.Single(store.Records);
        Assert.Equal("Unknown", record.Status);
    }

    private sealed class InMemoryStore : IRunesvanerStore
    {
        public List<RunesvanerRequestRecord> Records { get; } = new();

        public ValueTask WriteAsync(
            RunesvanerRequestRecord record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return ValueTask.CompletedTask;
        }

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}