# Native AOT 与 Trimming 策略

| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 工程、发布、平台团队 |
| 关联文档 | `05-technical-design.md`、`09-security-privacy.md`、`12-testing-and-benchmarks.md` |

---

## 1. 为什么 AOT 是一等约束

Runesvaner 的主赛道是 AI 负载诊断，不是 AOT 诊断器。但 AOT 必须作为**硬约束**，原因有三：

1. **用户场景真实存在**：越来越多 .NET AI 服务用 Native AOT 降低冷启动与内存占用。
2. **传统方案会失效或变重**：部分 Profiler 依赖 DAC/ClrMD；OTel SDK 在 AOT 下有体积与兼容成本。
3. **约束带来质量**：无反射、无动态代码的核心库，通常更小、更快、更可预测，对所有用户都有益。

> 重要事实：Native AOT 自 .NET 8 起支持 EventPipe（需 `<EventSourceSupport>true</EventSourceSupport>`），`dotnet-trace` / `dotnet-counters` 可用；但**托管堆分析不支持**。Runesvaner 不依赖 EventPipe，也不做堆分析，因此不受这些限制影响。

---

## 2. 能力边界声明

Runesvaner 在 Native AOT 下**提供**：

- 进程内 AI 请求延迟分段。
- Token 用量与成本计算。
- 流式 TTFT / ITL / TPS。
- 本地 SQLite 库默认存储（AOT 失败回退 JSONL）与 CLI 报告。
- 可选 OTLP 导出（受 OTel SDK 的 AOT 状态约束）。

Runesvaner 在 Native AOT 下**不提供**：

- 托管堆对象图、分配采样、GC 代际分析。
- CPU 采样 Profiler。
- 动态附加（attach）到已运行进程。
- 运行时方法 JIT 分析（AOT 无 JIT）。
- 依赖反射的自动发现。

这些边界必须在 README 与文档中明确，避免用户误解。

---

## 3. 核心设计约束

### 3.1 禁止清单

| 禁止项 | 原因 | 替代 |
| --- | --- | --- |
| `Type.GetType` / `Assembly.GetType` | 依赖运行时类型解析 | 显式注册 |
| `Activator.CreateInstance` | 反射构造 | 工厂委托 |
| `Assembly.Load` / `LoadFrom` | AOT 不支持动态加载 | 编译期引用 |
| `Reflection.Emit` | 无运行时代码生成 | 源生成 |
| `MakeGenericType` / `MakeGenericMethod` | AOT 泛型实例化受限 | 封闭泛型 / 显式实现 |
| `JsonSerializer` 反射模式 | Trim 会裁剪类型信息 | `JsonSerializerContext` |
| `IConfiguration.Get<T>()` 反射绑定 | 同上 | Source-generated binder |
| 程序集扫描 DI | 反射 + 裁剪风险 | 显式 `AddRunesvaner()` |
| `dynamic` | 运行时绑定 | 强类型 |
| `System.Linq.Expressions` 编译 | AOT 下为解释执行，慢且可能不支持 | 手写代码 |
| 动态代理（DispatchProxy/Castle） | 运行时代码生成 | 手写装饰器 |
| `Enum.Parse` 热路径 | 反射/字符串 | 显式映射 |
| `string.Format` 热路径 | 分配 | `StringBuilder` / 插值处理器 |
| 依赖 `AppDomain` | AOT 不支持 | 无 |

### 3.2 允许清单

| 能力 | 使用方式 |
| --- | --- |
| `System.Text.Json` Source Generator | 所有 JSON 序列化 |
| `Microsoft.Extensions.Configuration.Binder` Source Generator | 配置绑定 |
| `FrozenDictionary` / `FrozenSet` | 价格表、映射表 |
| `SearchValues<char>` | 文本扫描 |
| `System.Diagnostics.Metrics` | 指标 |
| `ActivitySource` | 可选追踪 |
| `Stopwatch.GetTimestamp` | 单调时钟 |
| `Channel<T>` | 异步队列 |
| `ValueTask` | 低分配异步 |
| `ArrayPool<T>` | 仅在基准证明必要时 |
| `UnsafeAccessor` | 谨慎；仅用于无法避免的 BCL 内部访问，需单独评审 |

### 3.3 关于 `UnsafeAccessor`

默认禁止。若未来必须使用：

- 必须有 ADR 记录。
- 必须通过 AOT smoke。
- 必须接受"未来运行时可能破坏"的风险。
- 不得用于业务逻辑，仅用于诊断基础设施。

---

## 4. 包级 AOT 矩阵

| 包 | AOT 目标 | 验证方式 | 备注 |
| --- | --- | --- | --- |
| `Runesvaner.Abstractions` | 完全兼容 | 编译 + smoke | 零依赖，仅 BCL |
| `Runesvaner` | 完全兼容 | 编译 + smoke + 基准 | 核心采集与成本 |
| `Runesvaner.MicrosoftExtensionsAI` | 完全兼容 | 编译 + smoke | 装饰器，无反射 |
| `Runesvaner.Storage.Jsonl` | 完全兼容 | 编译 + smoke | 纯 BCL I/O；AOT 回退路径 |
| `Runesvaner.Storage.Sqlite` | 库/应用内默认；需验证 | 独立 smoke + 回退测试 | 依赖 `Microsoft.Data.Sqlite` 与 native `e_sqlite3`；受 `dotnet/efcore#36068` 影响 |
| `Runesvaner.Exporters.OpenTelemetry` | 条件兼容 | 独立 smoke + 体积 | 取决于 OTel SDK 版本 |
| `Runesvaner.Cli` | 完全兼容 | `PublishAot` + 运行 | 默认 JSONL；`--storage sqlite` 可选；失败回退 |
| `Runesvaner.Studio` | 不要求 | — | 本地开发工具，可框架依赖 |

### 4.1 SQLite 的特殊处理

风险：

- `Microsoft.Data.Sqlite` 依赖原生 `e_sqlite3`，AOT 发布需要正确的 native 资产。
- 不同 RID 的打包与符号可能出问题。
- Trim 可能裁剪 SQLite 提供程序的某些元数据。

策略：

1. 库/应用内默认 SQLite；CLI 以 Native AOT 发布时默认 JSONL。
2. SQLite 的 AOT 兼容性受 `dotnet/efcore#36068` 影响：`e_sqlite3` 可能找不到，且项目未启用 `IsAotCompatible`。
3. CLI `--storage sqlite` 时尝试初始化 SQLite；失败自动回退 JSONL 并记录原因。
4. 库在 AOT 宿主中同样回退；不阻断业务。
5. M1 验证 workaround：native 资产复制、`SQLitePCLRaw` 初始化、WAL、查询；结果写入 ADR。
6. M2 评估框架依赖 CLI（默认 SQLite）作为补充发布。
7. Studio 可读取 SQLite 与 JSONL（非 AOT）。

### 4.2 OpenTelemetry 的特殊处理

风险：

- OTel SDK 在 AOT 下可能有 Trim 警告。
- 体积增量明显（社区曾通过移除 Regex 减少约 734 KB）。
- 不同 exporter（OTLP/gRPC、HTTP）对 AOT 支持不同。

策略：

1. 核心不依赖 OTel。
2. Exporter 包独立版本线，AOT 状态单独标注。
3. 提供"JSONL → 外部 Collector"的替代路径。
4. AOT 用户若只需本地诊断，不引用 Exporter。

---

## 5. 构建配置

### 5.1 库项目

    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <IsAotCompatible>true</IsAotCompatible>
      <EnableAotAnalyzer>true</EnableAotAnalyzer>
      <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
      <EnableSingleFileAnalyzer>true</EnableSingleFileAnalyzer>
      <VerifyReferenceAotCompatibility>true</VerifyReferenceAotCompatibility>
      <TrimmerSingleWarn>false</TrimmerSingleWarn>
      <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
      <WarningsAsErrors>IL2026;IL2067;IL2070;IL2072;IL2075;IL3050;IL3051;IL3053;IL3058</WarningsAsErrors>
    </PropertyGroup>

说明：

- `VerifyReferenceAotCompatibility` 是 .NET 10 引入的 IL3058 检查；对第三方依赖可能产生噪音，可在单独项目开启。
- `TrimmerSingleWarn=false` 确保每个警告可见。
- 将 IL 警告提升为错误，防止回归。

### 5.2 CLI 项目

    <PropertyGroup>
      <OutputType>Exe</OutputType>
      <TargetFramework>net10.0</TargetFramework>
      <PublishAot>true</PublishAot>
      <TrimMode>full</TrimMode>
      <InvariantGlobalization>false</InvariantGlobalization>
      <UseSystemResourceKeys>false</UseSystemResourceKeys>
      <StackTraceSupport>true</StackTraceSupport>
      <EventSourceSupport>false</EventSourceSupport>
      <MetadataUpdaterSupport>false</MetadataUpdaterSupport>
    </PropertyGroup>

说明：

- `InvariantGlobalization=false`：保留中文与本地化；体积增加需测量。
- `EventSourceSupport=false`：Runesvaner 不依赖 EventPipe；如需 OTel/EventSource，单独评估。
- `StackTraceSupport=true`：诊断工具自身需要可读堆栈。
- CLI 默认 JSONL，不引用 SQLite；`--storage sqlite` 需可选包或框架依赖版。

### 5.3 Sample AOT 项目

    <PropertyGroup>
      <PublishAot>true</PublishAot>
      <TrimMode>full</TrimMode>
      <IsAotCompatible>true</IsAotCompatible>
    </PropertyGroup>

Sample 必须覆盖：

- Chat 非流式
- Chat 流式
- Embedding
- 错误路径
- 取消路径

---

## 6. 验证流水线

### 6.1 CI 矩阵

| 维度 | 值 |
| --- | --- |
| OS | `ubuntu-latest`、`windows-latest` |
| RID | `linux-x64`、`win-x64`；`linux-arm64` 可选；`win-arm64` 非目标（D-24） |
| 配置 | Release |
| 项目 | Sample.Aot（库 SQLite 默认）、CLI（JSONL 默认） |
| 检查 | 构建警告、运行 smoke、输出 schema 校验、体积记录、SQLite AOT 尝试记录 |

### 6.2 Smoke 内容

1. 启动 AOT 二进制。
2. 执行一次 mock Chat 请求（本地 HTTP mock，不调用真实 provider）。
3. 生成一条会话文件。
4. 用 `runesvaner report --json` 读取。
5. 校验关键字段：operation、duration、segments、usage、cost。
6. 执行一次流式请求，校验 TTFT 与 chunk 计数。
7. 执行一次错误请求，校验错误记录。
8. CLI 默认 JSONL smoke；库 SQLite smoke（若 AOT 可用）。
9. 退出码为 0。

### 6.3 失败标准

- 任一 IL2026/IL3050 等警告 → 失败。
- Smoke 字段缺失 → 失败。
- 启动时间或体积超出基线 20% → 警告；超出 50% → 失败（阈值待基准确定）。
- 运行期出现 `NotSupportedException` / `MissingMethodException` → 失败。

---

## 7. 体积与启动预算

不设绝对体积目标（受运行时版本影响），设**增量预算**：

| 项目 | 预算 | 测量方式 |
| --- | --- | --- |
| Runesvaner 核心对 AOT 二进制的增量 | ≤ 500 KB | 对比启用/禁用 |
| CLI 二进制 | 基线 + 2 MB | JSONL 默认，不含 SQLite native 资产 |
| 启动时间增量 | ≤ 50 ms | 冷启动 10 次中位数 |
| 运行时内存增量 | ≤ 8 MB | 空闲 + 100 请求后 |

> 数值为初始预算，M1 用真实测量校准。若超标，优先裁剪依赖与字符串，不牺牲功能正确性。

---

## 8. 与 EventPipe / 诊断端口的关系

| 机制 | Runesvaner 是否使用 | 说明 |
| --- | --- | --- |
| EventPipe | 否 | 进程内采集，不依赖 runtime 诊断管道 |
| DiagnosticServer / 诊断端口 | 否 | 不做 attach |
| EventSource | 否（核心） | 可选导出由 OTel/用户决定 |
| ClrMD / DAC | 否 | 不做堆/线程分析 |
| ETW / LTTng | 否 | 不在 v1 |
| `System.Diagnostics.Metrics` | 是 | 标准指标，AOT 可用 |
| `ActivitySource` | 可选 | 与 OTel 互操作 |

这意味着：

- Runesvaner 可以在关闭 EventPipe 的 AOT 应用中工作。
- Runesvaner 不会与 `dotnet-trace` 竞争，也不会替代它。
- 若用户需要 CPU/GC 级诊断，仍应使用 `dotnet-trace` / PerfView / 平台工具。

---

## 9. 运行时能力检测

Runesvaner 在启动时检测：

| 检测项 | 行为 |
| --- | --- |
| 是否 AOT | 记录 `runtime.aot=true`，用于报告 |
| 是否支持 `ActivitySource` | 是（BCL） |
| 是否支持 SQLite native | 若启用 SQLite，尝试打开；失败则回退 JSONL 并警告 |
| 是否支持 OTLP exporter | 若启用，初始化失败则禁用并警告 |
| 文件系统权限 | 检查存储目录可写；失败则内存模式 |

检测结果写入 `session.start` 与 `runesvaner doctor`。

---

## 10. AOT 下的已知限制

1. **无堆分析**：不提供对象分配、代际、泄漏。
2. **无动态 provider**：不能运行时加载自定义价格表插件 DLL；只能 JSON 配置。
3. **无动态序列化**：自定义事件类型需源生成上下文；第三方 sink 需自行处理。
4. **SQLite 库默认、CLI 默认 JSONL**：AOT 下受 `dotnet/efcore#36068` 影响；失败回退 JSONL；见 4.1。
5. **OTel 可选**：见 4.2。
6. **反射型配置**：不提供；必须代码或 Source Generator。
7. **符号与堆栈**：AOT 堆栈可能缺少方法名；`StackTraceSupport=true` 缓解，但不保证完整。

---

## 11. 未来：AOT 专项诊断（v2 研究）

如果主产品站稳，可探索：

| 方向 | 价值 | 风险 | 决策 |
| --- | --- | --- | --- |
| 启动时间分解 | 中高 | 需 ILC/ETW/perf 深度集成 | D-18 |
| 二进制体积归因 | 中 | 与 Sizoscope/ILC 日志重叠 | D-18 |
| AOT 兼容性 Roslyn 分析器 | 中高 | 与官方 analyzer 重叠 | D-19 |
| 托管堆分析 | 高 | 需要 runtime 级工程 | 保留研究分支（D-18），不承诺交付 |

原则：这些方向必须作为**独立实验分支**，不能拖慢主产品；D-18/D-19 已决议保留研究/v2 投入，欢迎社区维护者。

---

## 12. 风险登记

| 风险 | 概率 | 影响 | 缓解 |
| --- | --- | --- | --- |
| MEAI 依赖引入 AOT 警告 | 中 | 高 | 契约测试；必要时隔离 |
| SQLite AOT 兼容性（#36068） | 高 | 高 | CLI 默认 JSONL；库失败回退；M1 验证 workaround |
| OTel exporter AOT 失败 | 中 | 中 | 可选包；替代路径 |
| 体积超标 | 中 | 中 | 增量预算；裁剪字符串 |
| 警告升级导致构建阻塞 | 高 | 中 | 分阶段启用；白名单 + ADR |
| `UnsafeAccessor` 被滥用 | 低 | 高 | 默认禁止；评审 |

---

## 13. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md` 与 `19-open-implementation-decisions.md`。与本策略直接相关：

- D-05：`net10.0`，M3 评估 `net11.0`。
- D-07：库/应用内 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
- D-13：核心零依赖，OTLP 可选。
- D-15：Windows/Linux；macOS 非目标。
- D-24：`win-arm64` 非目标；`linux-arm64` 可选。
- D-18：AOT 堆分析保留研究分支，不做主线。
- D-19：Roslyn AOT 分析器 v2 投入。