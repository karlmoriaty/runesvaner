# 技术设计：Runesvaner

> **中文版（Legacy full version）** — 英文主文档见 [`../05-technical-design.md`](../05-technical-design.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 工程、架构、安全 |
| 关联文档 | `03-prd.md`、`06-observability-model.md`、`07-api-design.md`、`08-native-aot-strategy.md` |

---

## 1. 设计目标

1. **低开销**：单请求 P95 增量 ≤ 1 ms，CPU ≤ 1%，分配 ≤ 2 KB。
2. **不干扰业务**：采集链路任何失败都不能让请求失败。
3. **可解释**：每个分段标注测量方法与 `estimated`。
4. **可组合**：核心零依赖；OTel、SQLite、Studio 都是可选层。
5. **AOT 安全**：核心路径无反射、无动态代码、无运行时类型加载。
6. **本地优先**：默认只写本机；默认不采集内容。
7. **可演进**：数据 schema 与公共 API 都有版本策略。

## 2. 关键约束

| 约束 | 影响 |
| --- | --- |
| 个人项目维护带宽 | 模块边界清晰，避免一次性铺开 |
| MEAI 版本演进 | 集成层隔离，核心不依赖 MEAI 类型 |
| AOT / Trim | 禁止反射序列化、动态 DI、运行时代码生成 |
| 流式 API | 包装器必须保持原语义、取消与异常传播 |
| 多 provider 差异 | 用量字段缺失时标记 unknown，不推断 |
| 隐私 | 默认无内容；哈希与脱敏在写入前完成 |

---

## 3. 架构总览

    +-----------------------------+
    |  Application / AI Pipeline  |
    |  IChatClient / IEmbedding   |
    +--------------+--------------+
                   |
                   v
    +-----------------------------+
    | Runesvaner.MicrosoftExtensionsAI
    |  ChatClientDecorator         |
    |  EmbeddingGeneratorDecorator |
    |  StreamingWrapper            |
    +--------------+--------------+
                   |
                   v
    +-----------------------------+
    | Runesvaner (Core)              |
    |  Recorder / Clock / Sampler  |
    |  Segmenter / CostEngine      |
    |  Redactor / RingBuffer       |
    |  SelfMonitor                 |
    +------+-----------+-----------+
           |           |
           v           v
    +-----------+  +------------------+
    | Storage   |  | Exporters        |
    | SQLite def|  | OTLP (optional)  |
    | JSONL fb  |  | Custom Sink      |
    +-----+-----+  +------------------+
          |
          v
    +-----------------------------+
    | Runesvaner.Cli / Studio        |
    |  read-only query + UI        |
    +-----------------------------+

### 3.1 依赖方向

| 层 | 允许依赖 | 禁止依赖 |
| --- | --- | --- |
| `Runesvaner.Abstractions` | BCL | 任何第三方包 |
| `Runesvaner` | Abstractions、DI Abstractions、Options | MEAI、OTel、SQLite |
| `Runesvaner.MicrosoftExtensionsAI` | Core、MEAI Abstractions | OTel、SQLite |
| `Runesvaner.Storage.*` | Core | MEAI、OTel |
| `Runesvaner.Exporters.*` | Core、OTel | MEAI、Storage |
| `Runesvaner.Cli` | Core、Storage、Studio host | MEAI（除示例） |

核心不引用 MEAI，意味着未来接入 Semantic Kernel、其他 SDK 时不需要改动核心。

---

## 4. 解决方案结构

    Runesvaner.sln
    src/
      Runesvaner.Abstractions/
      Runesvaner/
      Runesvaner.MicrosoftExtensionsAI/
      Runesvaner.SemanticKernel/            # M3
      Runesvaner.VectorData/                # M3
      Runesvaner.Storage.Jsonl/            # 回退/交换
      Runesvaner.Storage.Sqlite/            # 默认
      Runesvaner.Exporters.OpenTelemetry/   # M2
      Runesvaner.Cli/
      Runesvaner.Studio/                    # ASP.NET Core host + React build
    test/
      Runesvaner.UnitTests/
      Runesvaner.IntegrationTests/
      Runesvaner.AotSmokeTests/
      Runesvaner.Benchmarks/
      Runesvaner.ContractTests/             # MEAI 版本契约
    samples/
      Runesvaner.Sample.Console/
      Runesvaner.Sample.Rag/
      Runesvaner.Sample.Aot/
    docs/
    eng/
      build/
      ci/

### 4.1 包与发布

| 包 | 内容 | 依赖 |
| --- | --- | --- |
| `Runesvaner.Abstractions` | 公共模型、枚举、接口 | 无 |
| `Runesvaner` | 核心采集与成本引擎 | Abstractions、Options |
| `Runesvaner.MicrosoftExtensionsAI` | MEAI 中间件 | Core、MEAI.Abstractions |
| `Runesvaner.Storage.Jsonl` | JSONL 回退与交换 | Core |
| `Runesvaner.Storage.Sqlite` | 库/应用内默认存储与查询 | Core、Microsoft.Data.Sqlite |
| `Runesvaner.Exporters.OpenTelemetry` | OTLP 导出 | Core、OTel SDK |
| `Runesvaner.Cli` | `dotnet tool`，AOT | Core、Storage.Sqlite（失败回退 Jsonl） |
| `Runesvaner.Studio` | 本地 UI host | ASP.NET Core、Storage |

---

## 5. 采集模型

### 5.1 一次请求的生命周期

| 阶段 | 事件 | 记录内容 |
| --- | --- | --- |
| 进入中间件 | `request.start` | trace_id、operation、provider、model、start_ts |
| 建立连接 | `segment` | connect、tls（来自 HttpClient 诊断事件，若可用） |
| 上传完成 | `segment` | upload |
| 首字节 | `event` | ttfb |
| 首 Token | `event` | ttft（首个非空 delta） |
| 每个 chunk | `stream.chunk` | timestamp、delta 长度（不存内容） |
| 工具调用 | `segment` | tool name、duration、status |
| 向量检索 | `segment` | collection、top_k、duration |
| 完成 | `request.end` | usage、cost、status、segments、error |
| 落盘 | `storage.write` | 异步批量 |

### 5.2 分段模型

分段不是"猜"，而是分层：

1. **实测段**：能直接拿到起止时间（本地排队、序列化、工具、向量检索）。
2. **事件推导段**：由 `HttpClient` 诊断事件推导（连接、TLS、上传）。
3. **估算段**：只能由首字节/首 Token 时间差推导（Provider 排队、网络等待）。
4. **残差段**：总耗时减去已解释区间（`unaccounted`、`overlap`）。

每段必须带：

- `kind`
- `start_offset_us`
- `duration_us`
- `estimated`（bool）
- `measurement`（`stopwatch` / `http_diagnostic` / `derived` / `residual`）
- `lane`（并行分组）

### 5.3 时间源

- 单调时钟：`Stopwatch.GetTimestamp()`，转换为 `TimeSpan` 用 `Stopwatch.GetElapsedTime`。
- 墙钟：`TimeProvider.GetUtcNow()`，仅用于展示与跨进程关联。
- 测试：`IRunesvanerClock` 抽象，允许注入假时钟。
- 不在请求路径调用 `DateTime.UtcNow`。

### 5.4 流式包装

流式是最高风险区。包装器必须：

1. 不缓冲整个流；逐 chunk 透传。
2. 不改变 `CancellationToken`。
3. 不吞异常；记录后原样抛出。
4. `DisposeAsync` 时 flush 并记录 `cancelled`（若未完成）。
5. 对 `IAsyncEnumerable` 使用 `[EnumeratorCancellation]` 语义，避免取消失效。
6. 记录首个非空 delta 的时间；空 delta 不计入 TTFT。
7. 记录最后一个 chunk 的时间；不依赖 `usage` 一定在最后。

伪代码（设计示意）：

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var state = _recorder.BeginStreaming(...);
        try
        {
            await foreach (var update in _inner.GetStreamingResponseAsync(messages, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                state.OnChunk(update);
                yield return update;
            }
            state.OnCompleted();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            state.OnCancelled();
            throw;
        }
        catch (Exception ex)
        {
            state.OnError(ex);
            throw;
        }
        finally
        {
            await state.FlushAsync().ConfigureAwait(false);
        }
    }

### 5.5 与 OTel 的关系

- Runesvaner 自己使用 `ActivitySource` 与 `Meter`，名称 `Runesvaner.AI`。
- 若用户同时启用 `UseOpenTelemetry()`，会产生两套 Span。必须避免重复：
  - 默认：Runesvaner 只生成诊断事件，不创建额外 Activity；通过 `IRunesvanerSink` 输出。
  - 可选：`ActivitySource` 模式，供 OTel 用户使用；此时需文档说明 Span 嵌套关系。
- OTLP 导出包把 Runesvaner 事件转换为 OTel Span/Metric，避免双重导出。

---

## 6. 低开销策略

| 策略 | 说明 |
| --- | --- |
| 采样前置 | 未采样请求只做一次概率判断，不创建记录 |
| 结构体优先 | `Segment`、`Usage`、`Cost` 使用 `readonly struct` |
| 预分配 | 每请求 `Segment[16]`，超出才扩容 |
| 对象池 | 仅在基准证明必要时引入 `ArrayPool` |
| 异步落盘 | `Channel<Event>` + 后台批处理 |
| 源生成 JSON | `JsonSerializerContext`，避免反射 |
| 字符串复用 | provider/model 使用常量或池化 |
| 避免 LINQ | 热路径禁止 LINQ |
| 避免日志 | 热路径不调用 `ILogger`，失败时计数 |
| 关闭内容 | 默认不复制 Prompt/Completion |
| 熔断 | 队列满/落盘失败时快速降级 |

### 6.1 开销预算

| 操作 | 目标 |
| --- | ---: |
| 采样判断 | < 50 ns |
| 创建请求记录 | < 500 ns |
| 每分段写入 | < 100 ns |
| 每 chunk 记录 | < 200 ns |
| 成本计算 | < 5 µs |
| 总增量 P95 | < 1 ms |

---

## 7. 成本引擎

### 7.1 输入

- `provider`、`request_model`、`response_model`
- `usage`：input、output、cached_input、reasoning、image、audio
- `price_table_version`
- `batch`、`tier`、`cache` 标志

### 7.2 价格表

    {
      "version": "2026-11-01",
      "currency": "USD",
      "source": "https://example.com/pricing",
      "retrieved_at": "2026-11-01T00:00:00Z",
      "entries": [
        {
          "provider": "openai",
          "model": "gpt-5",
          "aliases": ["gpt-5-2026-08"],
          "input_per_million": 1.25,
          "cached_input_per_million": 0.125,
          "output_per_million": 10.00,
          "reasoning_per_million": 10.00,
          "tiers": []
        }
      ]
    }

规则：

- `decimal` 存储与计算，不用 `double`。
- 单位统一为"每百万 Token"。
- 未知模型：`CostStatus.Unknown`，不猜测。
- 价格表版本随请求记录；历史回放使用当时版本。
- 别名解析：请求模型 → 响应模型 → 别名 → 未知。
- 阶梯计价：若 `tiers` 非空，按区间累加。
- 批量折扣：`discount` 字段，默认 0。
- 多模态：按 provider 的 image/audio 单位计算，无法确定则 unknown。

### 7.3 计算顺序

1. 解析价格条目。
2. 输入 Token 分层。
3. 拆分 cached / non-cached。
4. 输出 Token。
5. 推理 Token（若与输出重叠，按 provider 定义，不重复计）。
6. 多模态附加。
7. 批量折扣。
8. 四舍五入到 6 位小数（`MidpointRounding.ToEven`，待 D-08 确认）。
9. 生成 `CostBreakdown` 与 `CostStatus`。

### 7.4 重试与计费

| 场景 | 处理 |
| --- | --- |
| 429 后成功 | 只计成功一次；失败尝试标 `not_billed` 或 `unknown` |
| 5xx 后成功 | 同上；若响应表明计费，标 `billed` |
| 超时后成功 | 可能双重计费；标 `possible_double_billing` |
| 用户取消 | 若 provider 已生成部分输出，标 `partial` |
| 流中断 | 尽力读取 usage；缺失则 unknown |

---

## 8. 采样

### 8.1 头采样

- 默认本地 100%，生产建议 10%（文档建议，不强制）。
- 规则：概率 + 始终保留错误/慢请求（需尾采样）。
- 记录 `sampling.probability`，聚合时加权。

### 8.2 尾采样

- 缓冲一个 trace 的轻量记录，直到结束。
- 保留条件：错误、P95 超阈值、成本超预算、显式标记。
- 风险：内存与延迟；默认关闭，M3 引入。

### 8.3 统计偏差

- 成本汇总必须使用加权：`sum(cost / probability)`。
- UI 显示"采样模式"与"置信度"。
- 不对未采样请求做成本外推时，明确标注"仅采样请求"。

---

## 9. 存储

库/应用内默认 SQLite；CLI AOT 默认 JSONL；SQLite 失败时回退，二者可交换。

### 9.1 JSONL（CLI AOT 默认、回退与交换）

目录：

    .runesvaner/
      sessions/
        20261125T143012Z-7f3a.jsonl
      pricing/
        builtin-2026-11-01.json
        user.json
      config.json
      studio.token

行格式：

    {"schema":"runesvaner.event.v1","session_id":"...","event_id":"...","type":"request.end","timestamp":"...","monotonic_ns":123,"payload":{...}}

写入：

- 追加模式，UTF-8，无 BOM。
- 每行一个完整 JSON；写入后 flush（可配置）。
- 读取时忽略最后不完整行。
- 文件滚动：单文件 ≤ 64 MB 或 10 万事件。

### 9.2 SQLite（库/应用内默认）

文件：`.runesvaner/runesvaner.db`，WAL 模式。

表：

| 表 | 主要列 |
| --- | --- |
| `sessions` | id, started_at, app, environment, version |
| `requests` | id, session_id, trace_id, operation, provider, model, started_at, duration_us, ttft_us, status |
| `segments` | request_id, kind, start_offset_us, duration_us, estimated, lane |
| `usage` | request_id, input, output, cached, reasoning, image, audio |
| `costs` | request_id, currency, input_cost, output_cost, cached_cost, reasoning_cost, total, status, price_version |
| `tags` | request_id, key, value |
| `errors` | request_id, type, message, stack_hash |
| `budgets` | scope, window, limit, spent |

索引：`started_at`、`model`、`feature`、`tenant_hash`、`status`、`cost_total`。

### 9.3 一致性

- 库/应用内 SQLite 是默认 source of truth；CLI AOT 默认 JSONL；两者可交换。
- 导入：`runesvaner index --path .runesvaner/sessions` 从 JSONL 构建 SQLite 索引。
- CLI：`runesvaner run` 默认 JSONL；`--storage sqlite` 显式选择 SQLite。
- 回退：任何宿主下 SQLite 初始化失败时写 JSONL，并记录 `storage.fallback=jsonl`。
- 崩溃恢复：SQLite 使用 WAL；JSONL 读取时忽略最后不完整行。

---

## 10. Studio 技术设计

- Host：ASP.NET Core Minimal API（框架引用，不额外依赖）。
- 前端：React 19 + TypeScript + Vite + Redux Toolkit + RTK Query（D-06 已决议）。
- 静态资源：构建后嵌入 CLI 或放在 `wwwroot`。
- 绑定：`127.0.0.1`，随机端口；`Host` 头校验；CORS 关闭。
- 认证：启动时生成 32 字节 token，URL `?token=`；API 使用 `Authorization: Bearer`。
- 只读：不写会话文件；Settings 写入独立配置文件。
- API：
  - `GET /api/sessions`
  - `GET /api/requests?session=&model=&status=&minCost=`
  - `GET /api/requests/{id}`
  - `GET /api/costs/summary`
  - `GET /api/health`
  - `POST /api/export`（可选）
- 大结果集：分页 + 游标；默认 100 条。
- 图表：uPlot（时间序列）+ 自绘瀑布（SVG/Canvas）。
- 性能：10 万请求下单页加载 ≤ 1 s（分页与索引）。

---

## 11. CLI 技术设计

| 命令 | 作用 | 退出码 |
| --- | --- | --- |
| `runesvaner run -- <cmd>` | 启动应用并采集 | 子进程退出码 |
| `runesvaner report` | 终端摘要 | 0/3 |
| `runesvaner top` | 慢/贵排行 | 0/3 |
| `runesvaner export` | JSONL/OTLP | 0 |
| `runesvaner open` | 启动 Studio | 0 |
| `runesvaner doctor` | 环境与配置检查 | 0/2 |
| `runesvaner pricing update` | 更新价格表（需显式） | 0/2 |

- 参数解析：自写轻量解析器，避免 `System.CommandLine` 的 AOT 风险。
- 输出：人类可读表格 + `--json`。
- 颜色：仅在 TTY 启用；`NO_COLOR` 支持。
- 全局配置：`.runesvaner/config.json` + 环境变量 `RUNESVANER_*`。

---

## 12. 错误处理与熔断

| 故障 | 检测 | 动作 |
| --- | --- | --- |
| 采集器异常 | try/catch | 计数、继续 |
| 队列满 | `Channel.Writer.TryWrite` 失败 | 丢弃最旧、计数 |
| 落盘失败 | IO 异常 | 熔断 30s、降级内存 |
| 序列化失败 | 异常 | 丢弃事件、计数 |
| 价格表损坏 | 校验失败 | 使用内置快照、警告 |
| Studio 端口占用 | bind 失败 | 尝试下一个端口 |
| 时钟异常 | 不可能（单调） | 记录 `invalid_timing` |

原则：**采集器的错误不能成为业务的错误。**

---

## 13. 配置

### 13.1 优先级

1. 代码配置（最高）
2. 环境变量 `RUNESVANER_*`
3. `.runesvaner/config.json`
4. 默认值

### 13.2 AOT 配置绑定

- 主推代码配置。
- JSON 配置使用 `Microsoft.Extensions.Configuration.Binder` 的 Source Generator（`EnableConfigurationBindingGenerator`）。
- 避免 `IConfiguration.Get<T>()` 反射路径。
- 所有 options 使用简单类型、数组、字典；避免多态。

---

## 14. AOT 策略摘要

完整见 `08-native-aot-strategy.md`。

- 所有库 `<IsAotCompatible>true</IsAotCompatible>`。
- 禁止：`Type.GetType`、`Activator.CreateInstance`、`Assembly.Load`、`Reflection.Emit`、`MakeGenericType`。
- JSON：`JsonSerializerContext`。
- 配置：Source Generator。
- DI：显式注册；不使用程序集扫描。
- CLI：`PublishAot=true`，`TrimMode=full`。
- SQLite：库/应用内默认存储；CLI AOT 默认 JSONL；AOT 必须验证 native `e_sqlite3`；失败自动回退。
- OTel：可选包；若 AOT 警告，文档给出替代导出方式（JSONL → 外部 collector）。

---

## 15. 可测试性

| 关注点 | 设计 |
| --- | --- |
| 时间 | `IRunesvanerClock`，假时钟 |
| 随机采样 | `ISampler`，可注入 |
| 存储故障 | `FaultInjectingStore` |
| 流式语义 | 自定义 `IAsyncEnumerable` 测试源 |
| 价格 | 固定价格表 + golden tests |
| AOT | 发布后 smoke |
| 性能 | BenchmarkDotNet + CI 门槛 |

---

## 16. 技术风险

| 风险 | 影响 | 缓解 |
| --- | --- | --- |
| MEAI API 变更 | 集成层破坏 | 多版本契约测试；适配器隔离 |
| 流式包装破坏取消 | 业务 bug | 专项测试；最小包装 |
| 分段估算不可信 | 用户不信任 | 标记 estimated；文档解释 |
| 价格表错误 | 成本错误 | 版本化 + 用户覆盖 + 核对视图 |
| AOT 下 SQLite 失败 | 库默认存储不可用 | 自动回退 JSONL；CLI 默认 JSONL；AOT smoke |
| OTel 双 Span | 数据重复 | 默认不创建 Activity；显式模式 |
| 开销超标 | 用户移除 | 基准门槛；采样；熔断 |
| 范围蔓延 | 无法交付 | 非目标清单；里程碑退出标准 |

---

## 17. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。与本设计直接相关：

- D-05：`net10.0`，M3 评估 `net11.0`。
- D-06：React 19 + Redux Toolkit + RTK Query。
- D-07：库/应用内 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
- D-08：内置价格表 + 用户覆盖；decimal + ToEven；USD。
- D-13：核心零依赖，OTLP 可选。
- D-14：本地 100%，生产建议 10%；尾采样 M3。
- D-15：Windows/Linux；macOS 非目标。
