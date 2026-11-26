namespace Runesvaner.Exporters.OpenTelemetry;

public sealed class RunesvanerOpenTelemetryStore : IRunesvanerStore
{
    private readonly IRunesvanerStore _inner;
    private readonly RunesvanerOpenTelemetryExporter _exporter;

    public RunesvanerOpenTelemetryStore(
        IRunesvanerStore inner,
        RunesvanerOpenTelemetryExporter exporter)
    {
        _inner = inner;
        _exporter = exporter;
    }

    public ValueTask WriteAsync(
        RunesvanerRequestRecord record,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _exporter.Export(record);
        }
        catch (Exception)
        {
            // Export failure must not block the local store or the business request.
        }

        return _inner.WriteAsync(record, cancellationToken);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => _inner.FlushAsync(cancellationToken);
}