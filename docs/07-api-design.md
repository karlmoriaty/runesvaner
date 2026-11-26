# Public API Design

| Field | Value |
| --- | --- |
| Document status | Draft / API not frozen |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Engineering, early contributors |
| Related documents | `05-technical-design.md`, `06-observability-model.md` |

---

## 1. Design principles

1. **Minimal surface**: the public API exposes only what users need to compose; internal types stay `internal`.
2. **Composable**: middleware, storage, export, and price tables are all replaceable.
3. **AOT-safe**: the public API does not require reflection; no `Type` parameters for dynamic registration.
4. **Failure isolation**: collection APIs do not throw; configuration errors fail fast at startup.
5. **Version stability**: `Runesvaner.Abstractions` is the most stable; experimental APIs are marked `[Experimental]`.
6. **No dependency on a specific AI SDK**: the core does not reference MEAI; MEAI integration lives in a separate package.

---

## 2. Namespaces

| Namespace | Contents |
| --- | --- |
| `Runesvaner` | Core entry points, options, scopes |
| `Runesvaner.Abstractions` | Public models, enums, interfaces |
| `Runesvaner.AI` | AI-specific models (Usage, Cost, Stream) |
| `Runesvaner.Storage` | Storage interfaces and implementations |
| `Runesvaner.Exporters` | Export interfaces |
| `Runesvaner.MicrosoftExtensionsAI` | MEAI extension methods |
| `Runesvaner.SemanticKernel` | SK extension methods (M3) |

---

## 3. Core entry points

### 3.1 DI extensions

    namespace Runesvaner;

    public static class RunesvanerServiceCollectionExtensions
    {
        public static IServiceCollection AddRunesvaner(
            this IServiceCollection services,
            Action<RunesvanerOptions>? configure = null);

        public static IServiceCollection AddRunesvaner(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<RunesvanerOptions>? configure = null);
    }

Behavior:

- Registers `IRunesvanerRecorder` (singleton).
- Registers `IRunesvanerStore` (default SQLite).
- Registers `IPriceTableProvider` (built-in default + user override).
- Registers `IRunesvanerClock` (default system clock).
- Registers `RunesvanerOptions` (`IOptions`).
- Does not scan assemblies; all registrations are explicit.

### 3.2 Options

    namespace Runesvaner;

    public sealed class RunesvanerOptions
    {
        public StorageOptions Storage { get; } = new();
        public SamplingOptions Sampling { get; } = new();
        public ContentCaptureOptions Content { get; } = new();
        public PricingOptions Pricing { get; } = new();
        public BudgetOptions Budgets { get; } = new();
        public ExportOptions Export { get; } = new();
        public PrivacyOptions Privacy { get; } = new();
        public string ApplicationName { get; set; } = "";
        public string Environment { get; set; } = "";
    }

    // Default is Sqlite in libraries/apps; overridden to Jsonl when the CLI is published with Native AOT.
    // The CLI can explicitly select `--storage sqlite`; falls back on failure.
    public sealed class StorageOptions
    {
        public StorageMode Mode { get; set; } = StorageMode.Sqlite;
        public string Path { get; set; } = ".runesvaner";
        public long MaxFileBytes { get; set; } = 64 * 1024 * 1024;
        public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
        public long MaxTotalBytes { get; set; } = 1024L * 1024 * 1024;
    }

    public sealed class SamplingOptions
    {
        public double HeadProbability { get; set; } = 1.0;
        public bool TailSamplingEnabled { get; set; } = false;
        public TimeSpan SlowThreshold { get; set; } = TimeSpan.FromSeconds(5);
        public decimal? CostThreshold { get; set; }
    }

    public sealed class ContentCaptureOptions
    {
        public ContentCapture Capture { get; set; } = ContentCapture.None;
        public int MaxContentBytes { get; set; } = 64 * 1024;
        public TimeSpan FullCaptureRetention { get; set; } = TimeSpan.FromDays(7);
    }

    public sealed class PricingOptions
    {
        public PricingMode Mode { get; set; } = PricingMode.BuiltInWithUserOverride;
        public string? UserPriceTablePath { get; set; }
        public string Currency { get; set; } = "USD";
        public bool AllowRemoteUpdate { get; set; } = false;
    }

    public sealed class BudgetOptions
    {
        public bool Enabled { get; set; }
        public decimal? PerRequestUsd { get; set; }
        public decimal? PerTenantDailyUsd { get; set; }
        public decimal? PerFeatureDailyUsd { get; set; }
    }

    public sealed class ExportOptions
    {
        public bool EnableOtlp { get; set; }
        public string? OtlpEndpoint { get; set; }
        public string? OtlpHeaders { get; set; }
        public bool RedactBeforeExport { get; set; } = true;
    }

    public sealed class PrivacyOptions
    {
        public bool HashUserIds { get; set; } = true;
        public bool HashTenantIds { get; set; } = true;
        public string[] RedactedHeaders { get; set; } =
        [
            "Authorization",
            "api-key",
            "x-api-key",
            "cookie",
            "set-cookie"
        ];
    }

### 3.3 Enums

    public enum StorageMode { Jsonl, Sqlite, Memory }

    public enum ContentCapture { None, MetadataOnly, Full }

    public enum PricingMode { BuiltInOnly, BuiltInWithUserOverride, UserOnly }

    public enum CostStatus { Known, Partial, Unknown }

    public enum BillingStatus { Billed, NotBilled, Unknown }

    public enum RunesvanerOperation
    {
        Chat,
        Embeddings,
        Tool,
        VectorSearch,
        Rerank,
        Unknown
    }

    public enum SegmentKind
    {
        Queue,
        Auth,
        Connect,
        Tls,
        Upload,
        ProviderQueue,
        Ttft,
        Generation,
        Tool,
        VectorSearch,
        Serialize,
        RetryWait,
        CacheLookup,
        Unaccounted,
        Overlap
    }

---

## 4. Scopes and tags

### 4.1 Static entry points

    namespace Runesvaner;

    public static class Runesvaner
    {
        public static IRunesvanerScope BeginScope(RunesvanerTags tags);
        public static RunesvanerTags Current { get; }
    }

    public readonly struct RunesvanerTags
    {
        public string? Feature { get; init; }
        public string? Tenant { get; init; }
        public string? User { get; init; }
        public string? PromptVersion { get; init; }
        public string? Deployment { get; init; }
        public IReadOnlyDictionary<string, string>? Extra { get; init; }
    }

    public interface IRunesvanerScope : IDisposable
    {
        void SetTag(string key, string value);
    }

Semantics:

- `BeginScope` uses `AsyncLocal` and supports asynchronous flows.
- Nested scopes merge tags; the inner scope takes precedence.
- `Tenant`/`User` are hashed before storage by default (depending on `PrivacyOptions`).
- The static API is for convenience only; tests or multi-tenant scenarios can inject `IRunesvanerScopeFactory`.

### 4.2 Scope factory

    public interface IRunesvanerScopeFactory
    {
        IRunesvanerScope BeginScope(RunesvanerTags tags);
    }

---

## 5. Recorder

    namespace Runesvaner.Abstractions;

    public interface IRunesvanerRecorder
    {
        bool IsEnabled { get; }

        IRunesvanerRequest StartRequest(RunesvanerRequestInfo info);

        void RecordSegment(string requestId, RunesvanerSegment segment);

        void RecordUsage(string requestId, RunesvanerUsage usage);

        void RecordCost(string requestId, RunesvanerCost cost);

        void RecordError(string requestId, RunesvanerError error);

        void CompleteRequest(string requestId, RunesvanerRequestResult result);
    }

    public interface IRunesvanerRequest : IDisposable
    {
        string RequestId { get; }
        void RecordSegment(RunesvanerSegment segment);
        void RecordUsage(RunesvanerUsage usage);
        void Complete(RunesvanerRequestResult result);
    }

Design intent:

- `IRunesvanerRecorder` is a low-level API for custom instrumentation.
- Normal users only need `UseRunesvaner()` and `BeginScope()`.
- All methods guarantee not to throw; failures are counted via self-monitoring.

### 5.1 Public models

    public readonly record struct RunesvanerRequestInfo(
        RunesvanerOperation Operation,
        string? Provider,
        string? RequestModel,
        string? Endpoint);

    public readonly record struct RunesvanerSegment(
        SegmentKind Kind,
        long StartOffsetUs,
        long DurationUs,
        bool Estimated,
        SegmentMeasurement Measurement,
        int Lane = 0);

    public readonly record struct RunesvanerUsage(
        long? InputTokens,
        long? OutputTokens,
        long? CachedInputTokens,
        long? ReasoningTokens,
        UsageSource Source);

    public readonly record struct RunesvanerCost(
        string Currency,
        decimal? Input,
        decimal? CachedInput,
        decimal? Output,
        decimal? Reasoning,
        decimal? Total,
        CostStatus Status,
        string PriceTableVersion);

    public readonly record struct RunesvanerError(
        string Type,
        string? MessageHash,
        string? StackHash,
        BillingStatus BillingStatus);

    public readonly record struct RunesvanerRequestResult(
        RequestStatus Status,
        long DurationUs,
        long? TtftUs,
        int Attempt);

    public enum SegmentMeasurement
    {
        Stopwatch,
        HttpDiagnostic,
        Derived,
        Residual
    }

    public enum UsageSource { Provider, Estimated, Mixed }

    public enum RequestStatus { Ok, Error, Cancelled, Timeout }

---

## 6. Storage extensions

    namespace Runesvaner.Storage;

    public interface IRunesvanerStore
    {
        ValueTask WriteAsync(
            RunesvanerEvent evt,
            CancellationToken cancellationToken = default);

        ValueTask FlushAsync(CancellationToken cancellationToken = default);
    }

    public interface IRunesvanerQueryStore
    {
        ValueTask<RunesvanerSession?> GetSessionAsync(string sessionId, CancellationToken ct = default);
        ValueTask<IReadOnlyList<RunesvanerRequestSummary>> QueryRequestsAsync(
            RunesvanerRequestQuery query,
            CancellationToken ct = default);
        ValueTask<RunesvanerRequestDetail?> GetRequestAsync(string requestId, CancellationToken ct = default);
    }

SQLite implements both (default); JSONL implements write and import (fallback/interchange). Studio depends on `IRunesvanerQueryStore`.

---

## 7. Price table extensions

    namespace Runesvaner.Abstractions;

    public interface IPriceTableProvider
    {
        ValueTask<PriceTable> GetAsync(
            string provider,
            string model,
            CancellationToken cancellationToken = default);

        string Version { get; }
    }

    public sealed record PriceTable(
        string Version,
        string Currency,
        IReadOnlyDictionary<string, PriceEntry> Entries);

    public sealed record PriceEntry(
        string Provider,
        string Model,
        decimal InputPerMillion,
        decimal CachedInputPerMillion,
        decimal OutputPerMillion,
        decimal? ReasoningPerMillion,
        IReadOnlyList<PriceTier>? Tiers,
        decimal BatchDiscount);

Rules:

- When `GetAsync` finds nothing, does it return `null` or throw `PriceNotFoundException`? Design decision: return `PriceLookupResult.NotFound` to avoid exception-based control flow.
- Price tables are immutable; updates are made by replacing the provider.
- User overrides take precedence over built-in entries.

---

## 8. Export extensions

    namespace Runesvaner.Exporters;

    public interface IRunesvanerSink
    {
        ValueTask OnEventAsync(RunesvanerEvent evt, CancellationToken ct = default);
    }

    public interface IRunesvanerExporter
    {
        ValueTask ExportAsync(
            IReadOnlyList<RunesvanerEvent> events,
            CancellationToken ct = default);
    }

- `IRunesvanerSink`: per-event, suitable for real-time forwarding.
- `IRunesvanerExporter`: batched, suitable for OTLP.
- No sink by default; the OTLP package registers an exporter.
- Export failures must not affect business.

---

## 9. MEAI integration API

    namespace Runesvaner.MicrosoftExtensionsAI;

    public static class RunesvanerChatClientBuilderExtensions
    {
        public static ChatClientBuilder UseRunesvaner(
            this ChatClientBuilder builder,
            Action<RunesvanerOptions>? configure = null);
    }

    public static class RunesvanerEmbeddingBuilderExtensions
    {
        public static EmbeddingGeneratorBuilder<TInput, TEmbedding> UseRunesvaner<TInput, TEmbedding>(
            this EmbeddingGeneratorBuilder<TInput, TEmbedding> builder,
            Action<RunesvanerOptions>? configure = null)
            where TEmbedding : Embedding;
    }

Usage:

    services.AddChatClient(builder => builder
        .UseRunesvaner()
        .UseOpenTelemetry());

    services.AddEmbeddingGenerator(builder => builder
        .UseRunesvaner());

Notes:

- `UseRunesvaner` does not change the `GetService` semantics of `IChatClient`; the decorator must forward `GetService`.
- Both streaming and non-streaming methods must be implemented.
- The decorator forwards `Dispose` to the inner instance.
- Does not capture sensitive fields in `ChatOptions` (such as the API key).

---

## 10. Semantic Kernel integration (M3)

    namespace Runesvaner.SemanticKernel;

    public static class RunesvanerKernelBuilderExtensions
    {
        public static IKernelBuilder AddRunesvaner(
            this IKernelBuilder builder,
            Action<RunesvanerOptions>? configure = null);
    }

Implementation approach:

- Prefer SK's Filter (Function Invocation Filter) to record tool calls.
- Chat is still recorded via MEAI middleware to avoid duplication.
- If the SK version does not support filters, use a decorator.

---

## 11. VectorData integration (M3)

    namespace Runesvaner.VectorData;

    public static class RunesvanerVectorStoreExtensions
    {
        public static IVectorStore UseRunesvaner(
            this IVectorStore store,
            Action<RunesvanerOptions>? configure = null);
    }

Records:

- collection name (low cardinality)
- top_k
- retrieval duration
- number of returned items
- whether rerank was performed

Does not record:

- vector contents
- document body
- original query (by default)

---

## 12. CLI API (internal)

The CLI does not promise a stable public API, but commands and exit codes are part of the user contract:

| Command | Stability |
| --- | --- |
| `run` / `report` / `top` / `export` / `open` | Stable |
| `doctor` / `pricing` | Stable |
| `index` | Experimental |

---

## 13. Exception contract

| Scenario | Behavior |
| --- | --- |
| Exception during collection | Caught, counted, not thrown |
| Invalid configuration (e.g., negative sampling rate) | `OptionsValidationException` at startup |
| Storage not writable | Warning at startup; circuit breaker at runtime |
| Malformed price table | `InvalidDataException` at startup (if explicitly loaded); otherwise fall back to built-in |
| Studio port already in use | Automatically switch ports; do not throw |
| Export failure | Counted; do not throw |

Principle: **Never throw on the user's code path because of Runesvaner.**

---

## 14. Versioning and compatibility

| Package | Stability | Notes |
| --- | --- | --- |
| `Runesvaner.Abstractions` | High | Models and interfaces; breaking changes require a major version |
| `Runesvaner` | High | Core behavior |
| `Runesvaner.MicrosoftExtensionsAI` | Medium | Affected by the MEAI version |
| `Runesvaner.Storage.*` | Medium | Affected by underlying dependencies |
| `Runesvaner.Exporters.*` | Medium | Affected by OTel |
| `Runesvaner.Cli` | Medium | Commands stable; output format may change |

Rules:

- Public API changes require API review.
- New interface members should prefer default implementations (C# DIM), but AOT/Trim must be verified.
- Experimental APIs use `[Experimental("APERTURE001")]`.
- Do not delete or rename public members in patch versions.

---

## 15. API examples

### 15.1 Minimal integration

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRunesvaner(o =>
    {
        o.ApplicationName = "support-api";
        o.Storage.Path = ".runesvaner";
        o.Content.Capture = ContentCapture.None;
    });

    builder.Services.AddChatClient(chat => chat
        .UseRunesvaner()
        .UseOpenTelemetry());

### 15.2 Manual tags

    using (Runesvaner.BeginScope(new RunesvanerTags
    {
        Feature = "support.answer",
        Tenant = tenantId,
        PromptVersion = "support-v12"
    }))
    {
        var response = await chatClient.GetResponseAsync(messages, options, ct);
        return response.Text;
    }

### 15.3 Custom export

    public sealed class MySink : IRunesvanerSink
    {
        public ValueTask OnEventAsync(RunesvanerEvent evt, CancellationToken ct = default)
        {
            // Forward to an internal system; the sink must guarantee no exceptions, or Runesvaner will catch them
            return ValueTask.CompletedTask;
        }
    }

    services.AddRunesvaner();
    services.AddSingleton<IRunesvanerSink, MySink>();

### 15.4 Custom price table

    services.AddRunesvaner(o =>
    {
        o.Pricing.Mode = PricingMode.BuiltInWithUserOverride;
        o.Pricing.UserPriceTablePath = "pricing/my-prices.json";
    });

---

## 16. Decision status

D-01–D-25 are all resolved; see `15-decision-log.md`. Directly related to this API:

- D-02: namespace and package prefix use `Runesvaner`.
- D-05: `net10.0`, with contract tests for MEAI 10.x/11.x.
- D-07: libraries default to `StorageMode.Sqlite`; CLI AOT defaults to `Jsonl`; fall back on failure.
- D-13: `ActivitySource` as an optional export mode; no additional Activity is created by default.
- D-17: LTS + latest STS.
- D-20: no commitment to third-party semantic compatibility; OTel GenAI is primary.