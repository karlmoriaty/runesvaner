namespace Runesvaner.Exporters.OpenTelemetry;

/// <summary>Store decorator that exports every record and isolates export failures.</summary>
public sealed class RunesvanerOpenTelemetryStore : IRunesvanerStore
{
    private readonly IRunesvanerStore _inner;
    private readonly RunesvanerOpenTelemetryExporter _exporter;

    /// <summary>Creates the decorating store.</summary>
    public RunesvanerOpenTelemetryStore(
        IRunesvanerStore inner,
        RunesvanerOpenTelemetryExporter exporter)
    {
        _inner = inner;
        _exporter = exporter;
    }

    /// <summary>Exports the record, then writes to the local store.</summary>
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

    /// <summary>Flushes the local store.</summary>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => _inner.FlushAsync(cancellationToken);
}