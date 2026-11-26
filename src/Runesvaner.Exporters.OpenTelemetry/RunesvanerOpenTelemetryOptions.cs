using OpenTelemetry.Exporter;

namespace Runesvaner.Exporters.OpenTelemetry;

public sealed class RunesvanerOpenTelemetryOptions
{
    public Uri Endpoint { get; set; } = new("http://localhost:4317");

    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc;

    public string ServiceName { get; set; } = "runesvaner";
}