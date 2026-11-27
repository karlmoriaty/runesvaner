using OpenTelemetry.Exporter;

namespace Runesvaner.Exporters.OpenTelemetry;

/// <summary>OTLP exporter configuration.</summary>
public sealed class RunesvanerOpenTelemetryOptions
{
    /// <summary>Gets or sets the OTLP endpoint.</summary>
    public Uri Endpoint { get; set; } = new("http://localhost:4317");

    /// <summary>Gets or sets the OTLP protocol.</summary>
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;

    /// <summary>Gets or sets the OpenTelemetry resource service name.</summary>
    public string ServiceName { get; set; } = "runesvaner";
}