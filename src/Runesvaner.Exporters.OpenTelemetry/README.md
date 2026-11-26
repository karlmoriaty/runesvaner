# Runesvaner.Exporters.OpenTelemetry

Optional exporter that converts Runesvaner request records into OpenTelemetry spans and metrics, then sends them through the OpenTelemetry SDK's OTLP exporter.

The core package and the AOT CLI do **not** depend on this package.

## Usage

    await using var exporter = RunesvanerOpenTelemetryExporter.Create(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
        options.Protocol = OtlpExportProtocol.Grpc;
        options.ServiceName = "support-api";
    });

    var innerStore = new JsonlRunesvanerStore(".runesvaner");
    var store = new RunesvanerOpenTelemetryStore(innerStore, exporter);
    var recorder = new RunesvanerRecorder(store, new RunesvanerOptions
    {
        Provider = "openai",
        PricingTable = priceTable
    });

For OTLP over HTTP/protobuf use port `4318` and `OtlpExportProtocol.HttpProtobuf`.

## Signals

Spans:

- Name: request operation (`Chat`, `Rag`, ...).
- Tags: `gen_ai.operation.name`, `gen_ai.provider.name`, `gen_ai.request.model`, `gen_ai.response.model`, `gen_ai.usage.*`, `runesvaner.request_id`, `runesvaner.status`, `runesvaner.duration_us`, `runesvaner.ttft_us`, `runesvaner.cost.*`, `runesvaner.stream.*`.
- Events: one `segment` event per latency segment, including kind, offset, duration, estimated, measurement, and lane.

Metrics:

- `runesvaner.ai.requests` counter
- `runesvaner.ai.request.duration` histogram (seconds)
- `runesvaner.ai.ttft` histogram (seconds)
- `runesvaner.ai.tokens` counter, tagged by `gen_ai.token.type`
- `runesvaner.ai.cost` counter, only for `CostStatus.Known`

## Failure isolation

`RunesvanerOpenTelemetryStore` catches exporter failures and still writes to the inner local store. Export must never fail the business request.

## AOT note

This package uses the OpenTelemetry SDK. The Runesvaner core and CLI remain AOT-compatible; this optional exporter must be validated separately before use inside a Native AOT application. The test suite verifies signal mapping with `ActivityListener` and `MeterListener`; it does not require a running OTLP collector.