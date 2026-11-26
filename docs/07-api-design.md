# 公共 API 设计

| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft / API 未冻结 |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 工程、早期贡献者 |
| 关联文档 | `05-technical-design.md`、`06-observability-model.md` |

---

## 1. 设计原则

1. **最小表面**：公共 API 只暴露用户需要组合的东西；内部类型 `internal`。
2. **可组合**：中间件、存储、导出、价格表都可替换。
3. **AOT 安全**：公共 API 不要求反射；不使用 `Type` 参数做动态注册。
4. **失败隔离**：采集 API 不抛异常；配置错误在启动时快速失败。
5. **版本稳定**：`Runesvaner.Abstractions` 最稳定；实验性 API 标注 `[Experimental]`。
6. **不依赖特定 AI SDK**：核心不引用 MEAI；MEAI 集成在独立包。

---

## 2. 命名空间

| 命名空间 | 内容 |
| --- | --- |
| `Runesvaner` | 核心入口、选项、作用域 |
| `Runesvaner.Abstractions` | 公共模型、枚举、接口 |
| `Runesvaner.AI` | AI 专用模型（Usage、Cost、Stream） |
| `Runesvaner.Storage` | 存储接口与实现 |
| `Runesvaner.Exporters` | 导出接口 |
| `Runesvaner.MicrosoftExtensionsAI` | MEAI 扩展方法 |
| `Runesvaner.SemanticKernel` | SK 扩展方法（M3） |

---

## 3. 核心入口

### 3.1 DI 扩展

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

行为：

- 注册 `IRunesvanerRecorder`（单例）。
- 注册 `IRunesvanerStore`（默认 SQLite）。
- 注册 `IPriceTableProvider`（默认内置 + 用户覆盖）。
- 注册 `IRunesvanerClock`（默认系统时钟）。
- 注册 `RunesvanerOptions`（`IOptions`）。
- 不扫描程序集；所有注册显式。

### 3.2 选项

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

    // 库/应用内默认 Sqlite；CLI 以 Native AOT 发布时覆盖为 Jsonl。
    // CLI 可用 `--storage sqlite` 显式选择，失败时回退。
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

### 3.3 枚举

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

## 4. 作用域与标签

### 4.1 静态入口

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

语义：

- `BeginScope` 使用 `AsyncLocal`，支持异步流。
- 嵌套 scope 合并标签，内层优先。
- `Tenant`/`User` 默认哈希后再存储（取决于 `PrivacyOptions`）。
- 静态 API 仅为便利；测试或多租户场景可用 `IRunesvanerScopeFactory` 注入。

### 4.2 作用域工厂

    public interface IRunesvanerScopeFactory
    {
        IRunesvanerScope BeginScope(RunesvanerTags tags);
    }

---

## 5. 记录器

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

设计意图：

- `IRunesvanerRecorder` 是低层 API，供自定义 Instrumentation 使用。
- 正常用户只需要 `UseRunesvaner()` 与 `BeginScope()`。
- 所有方法保证不抛异常；失败通过自监控计数。

### 5.1 公共模型

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

## 6. 存储扩展

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

SQLite 实现两者（默认）；JSONL 实现写入与导入（回退/交换）。Studio 依赖 `IRunesvanerQueryStore`。

---

## 7. 价格表扩展

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

规则：

- `GetAsync` 找不到时返回 `null` 或抛 `PriceNotFoundException`？设计选择：返回 `PriceLookupResult.NotFound`，避免异常控制流。
- 价格表不可变；更新通过替换 provider。
- 用户覆盖优先级高于内置。

---

## 8. 导出扩展

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

- `IRunesvanerSink`：逐事件，适合实时转发。
- `IRunesvanerExporter`：批量，适合 OTLP。
- 默认没有 sink；OTLP 包注册 exporter。
- 导出失败不得影响业务。

---

## 9. MEAI 集成 API

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

使用：

    services.AddChatClient(builder => builder
        .UseRunesvaner()
        .UseOpenTelemetry());

    services.AddEmbeddingGenerator(builder => builder
        .UseRunesvaner());

注意：

- `UseRunesvaner` 不改变 `IChatClient` 的 `GetService` 语义；装饰器需转发 `GetService`。
- 流式与非流式方法都必须实现。
- 装饰器 `Dispose` 转发给 inner。
- 不捕获 `ChatOptions` 中的敏感字段（如 API Key）。

---

## 10. Semantic Kernel 集成（M3）

    namespace Runesvaner.SemanticKernel;

    public static class RunesvanerKernelBuilderExtensions
    {
        public static IKernelBuilder AddRunesvaner(
            this IKernelBuilder builder,
            Action<RunesvanerOptions>? configure = null);
    }

实现方式：

- 优先通过 SK 的 Filter（Function Invocation Filter）记录工具调用。
- Chat 仍通过 MEAI 中间件记录，避免重复。
- 若 SK 版本不支持 filter，则用装饰器。

---

## 11. VectorData 集成（M3）

    namespace Runesvaner.VectorData;

    public static class RunesvanerVectorStoreExtensions
    {
        public static IVectorStore UseRunesvaner(
            this IVectorStore store,
            Action<RunesvanerOptions>? configure = null);
    }

记录：

- collection 名称（低基数）
- top_k
- 检索耗时
- 返回条数
- 是否 rerank

不记录：

- 向量内容
- 文档正文
- 原始 query（默认）

---

## 12. CLI API（内部）

CLI 不承诺公共 API 稳定，但命令与退出码属于用户契约：

| 命令 | 稳定性 |
| --- | --- |
| `run` / `report` / `top` / `export` / `open` | 稳定 |
| `doctor` / `pricing` | 稳定 |
| `index` | 实验性 |

---

## 13. 异常契约

| 场景 | 行为 |
| --- | --- |
| 采集期间异常 | 捕获、计数、不抛 |
| 配置非法（如负采样率） | 启动时 `OptionsValidationException` |
| 存储不可写 | 启动时警告；运行期熔断 |
| 价格表格式错误 | 启动时 `InvalidDataException`（若显式加载）；否则回退内置 |
| Studio 端口占用 | 自动换端口；不抛 |
| 导出失败 | 计数；不抛 |

原则：**用户代码路径上不因 Runesvaner 抛异常。**

---

## 14. 版本与兼容

| 包 | 稳定性 | 说明 |
| --- | --- | --- |
| `Runesvaner.Abstractions` | 高 | 模型与接口；破坏性变更需主版本 |
| `Runesvaner` | 高 | 核心行为 |
| `Runesvaner.MicrosoftExtensionsAI` | 中 | 受 MEAI 版本影响 |
| `Runesvaner.Storage.*` | 中 | 受底层依赖影响 |
| `Runesvaner.Exporters.*` | 中 | 受 OTel 影响 |
| `Runesvaner.Cli` | 中 | 命令稳定，输出格式可变 |

规则：

- 公共 API 变更需 API Review。
- 新增接口成员优先用默认实现（C# DIM），但需验证 AOT/Trim。
- 实验性 API 使用 `[Experimental("APERTURE001")]`。
- 不在补丁版本删除或重命名公共成员。

---

## 15. API 示例

### 15.1 最小接入

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

### 15.2 手动标签

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

### 15.3 自定义导出

    public sealed class MySink : IRunesvanerSink
    {
        public ValueTask OnEventAsync(RunesvanerEvent evt, CancellationToken ct = default)
        {
            // 转发到内部系统；必须自行保证不抛异常或由 Runesvaner 捕获
            return ValueTask.CompletedTask;
        }
    }

    services.AddRunesvaner();
    services.AddSingleton<IRunesvanerSink, MySink>();

### 15.4 自定义价格表

    services.AddRunesvaner(o =>
    {
        o.Pricing.Mode = PricingMode.BuiltInWithUserOverride;
        o.Pricing.UserPriceTablePath = "pricing/my-prices.json";
    });

---

## 16. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。与本 API 直接相关：

- D-02：命名空间与包前缀使用 `Runesvaner`。
- D-05：`net10.0`，MEAI 10.x/11.x 契约测试。
- D-07：库默认 `StorageMode.Sqlite`；CLI AOT 默认 `Jsonl`；失败回退。
- D-13：`ActivitySource` 作为可选导出模式，默认不创建额外 Activity。
- D-17：LTS + 最新 STS。
- D-20：不承诺第三方语义兼容；OTel GenAI 为主。