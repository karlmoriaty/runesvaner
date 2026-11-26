# 市场与竞品调研：.NET AI 诊断 vs Native AOT 诊断

> **中文版（Legacy full version）** — 英文主文档见 [`../01-market-research.md`](../01-market-research.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 调研日期 | 2026-11-25 |
| 数据快照 | GitHub API、NuGet API、Microsoft Learn（均为当日抓取） |
| 关联文档 | `00-project-charter.md`、`03-prd.md`、`15-decision-log.md` |

---

## 1. 方法与局限

### 1.1 方法

1. 官方文档核对：Native AOT 诊断能力、`dotnet-counters`、`Microsoft.Extensions.AI`、OpenTelemetry GenAI 语义约定。
2. GitHub 仓库数据：Stars、Forks、License、最近提交时间（API 快照）。
3. NuGet 搜索：`.NET LLM observability` 相关包的下载量与维护状态。
4. 竞品许可证核对：读取仓库 `LICENSE` 原文。
5. 技术可行性推演：基于官方能力边界，不进行代码原型（本阶段仅文档）。

### 1.2 局限

- GitHub Stars 受传播、语言生态、发布时间影响，不等于产品质量。
- NuGet 下载量包含 CI 与镜像流量，不等于真实用户数。
- 未进行用户访谈；用户需求判断基于公开 Issue、竞品功能与工程经验，需在 M1 用访谈校验。
- 价格、许可证、API 均可能变化；本文档标注日期，后续需定期复核。

---

## 2. 切口 A：Native AOT 兼容诊断器

### 2.1 官方能力边界（关键事实）

根据 Microsoft Learn 的 [Native AOT diagnostics](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics)（页面更新于 2025-04）：

| 能力 | 支持程度 | 说明 |
| --- | --- | --- |
| 可观测性与遥测 | 部分支持 | 自 .NET 8 起支持 EventPipe；需设置 `<EventSourceSupport>true</EventSourceSupport>` |
| `dotnet-trace` / `dotnet-counters` / `dotnet-monitor` | 可用 | 通过 EventPipe 与诊断端口工作，但并非所有 runtime events 都支持 |
| 开发期诊断 | 完全支持 | `build` / `run` / `test` 仍使用完整运行时；只有 `publish` 产生 AOT 二进制 |
| 原生调试 | 部分支持 | 可用 WinDbg / gdb / lldb；托管调试器不可用；需符号文件 |
| CPU Profiling | 部分支持 | 可用 PerfView、`perf` 等平台工具 |
| 托管堆分析 | **不支持** | `dotnet-gcdump`、PerfView 堆分析、Visual Studio 堆分析均不可用 |

同时，[Native AOT 概览](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) 明确指出限制包括：无动态加载、无运行时代码生成、必须裁剪、单文件、部分运行时库未完全标注 AOT 兼容、诊断支持有限。

### 2.2 由此推导的真实空白

| 空白 | 价值 | 难度 | 个人项目适配 |
| --- | --- | --- | --- |
| 托管堆分析（对象分配、代际、泄漏） | 高 | 极高：无 DAC，需要自建 GC 堆遍历 | 低 |
| 启动时间分解（runtime init、模块加载、静态构造、ILC 代码） | 中高 | 中高：需 ILC / ETW / perf 深度集成 | 中 |
| 二进制体积归因（泛型实例化、裁剪残留、依赖） | 中 | 中：已有 ILC 日志、Sizoscope 可参考 | 中高 |
| EventPipe 默认关闭导致的"生产不可观测" | 中 | 中：需要打包策略与体积权衡 | 中 |
| AOT 兼容性静态诊断（Roslyn 分析器） | 中高 | 中：Roslyn 分析器 + ILLink 警告整合 | 高（符合发起人技术偏好） |
| AOT 下的低开销 always-on 指标 | 中 | 低：基于 `Meter` + 可选 EventPipe | 高 |

**关键判断**：Native AOT 诊断并非"完全空白"。EventPipe 的存在让 `dotnet-trace` / `dotnet-counters` 在 AOT 下可用，这削弱了"另一个通用 AOT Profiler"的卖点。真正空白集中在**托管堆**与**启动/体积归因**，前者技术风险极高，后者更像工具链而不是运行时诊断器。

### 2.3 竞争格局

| 工具/方向 | 与 AOT 的关系 | 备注 |
| --- | --- | --- |
| `dotnet-trace` / `dotnet-counters` | 可用（EventPipe） | 官方维护，通用能力强 |
| PerfView / `perf` | CPU 可用 | 非托管视角，学习曲线陡 |
| `dotnet-gcdump` | 不可用 | 托管堆空白 |
| Sizoscope | 二进制体积分析 | 社区工具，偏离线 |
| ILC 日志 | 启动/体积线索 | 需要专业知识解读 |
| OpenTelemetry .NET | 可用但有体积/Trim 成本 | 社区已有 AOT 体积优化 PR（如去除 Regex，节省约 734 KB） |

### 2.4 结论

- **不作为首发主赛道**。
- 可作为 v2 的"Native AOT 实验分支"：启动/体积归因、AOT 兼容性 Roslyn 分析器。
- 托管堆分析不作为主线，保留研究分支与 issue（D-18）。

---

## 3. 切口 B：面向 AI 负载的诊断工具

### 3.1 市场背景

| 事实 | 含义 |
| --- | --- |
| `Microsoft.Extensions.AI` 最新版本到 11.0.1（NuGet 快照） | .NET AI 抽象已成熟，存在统一接入点 |
| MEAI 内置 `UseOpenTelemetry` 中间件 | 基础 tracing 已被官方覆盖，单纯"发 Span"没有差异化 |
| `Microsoft.Extensions.VectorData.Abstractions` 到 11.0.1 | RAG 链路可被统一观测 |
| Semantic Kernel 28.8k Stars，持续活跃 | 企业级 .NET AI 用户基数大 |
| OpenTelemetry GenAI 语义约定独立成仓，指标仍为 Development | 标准在快速演进，存在适配与解释成本 |

### 3.2 竞品格局（GitHub 快照，2026-11-25）

| 项目 | Stars | 主语言 | 许可证 | 定位 | 对 .NET 的覆盖 |
| --- | ---: | --- | --- | --- | --- |
| [Langfuse](https://github.com/langfuse/langfuse) | 37,839 | TypeScript | MIT Expat + EE 目录 | AI 工程平台：网关、可观测、评估、Prompt 管理 | 以 OTLP / SDK 为主，非 .NET 原生 |
| [Arize Phoenix](https://github.com/Arize-ai/phoenix) | 12,423 | Python | Elastic License 2.0 | AI 可观测与评估 | 非 .NET 原生；ELv2 限制托管服务 |
| [OpenLLMetry](https://github.com/traceloop/openllmetry) | 7,554 | Python | Apache-2.0 | 基于 OTel 的 GenAI 可观测 | Python / TS 为主，.NET 非重点 |
| [Semantic Kernel](https://github.com/microsoft/semantic-kernel) | 28,807 | C# | MIT | .NET AI 编排框架 | 本身不是诊断工具 |
| [dotnet/extensions](https://github.com/dotnet/extensions) | 3,300 | C# | MIT | MEAI 与诊断/遥测基础设施 | 提供基础中间件，不做成本/瀑布 |

### 3.3 .NET 侧的直接/邻近竞品（NuGet 搜索快照）

搜索 `llm observability` 返回 70 个结果，其中与 .NET AI 诊断直接相关的包括：

| 包 | 下载量（总） | 说明 | 判断 |
| --- | ---: | --- | --- |
| `qyl` | 13,588 | 本地可观测栈，含 collector 与诊断 collector | 工具形态，非库；与 Runesvaner 可互补 |
| `NovaCore.Agents.Observability.OpenTelemetry` | 12,671 | Agent 运行时的 OTel 导出 | 绑定自有 Agent 框架 |
| `MemoryIndexer.Sdk` | 16,822 | 记忆库 SDK，含 OTel | 非诊断专用 |
| `Progress.Observability.Instrumentation` | 3,103 | Telerik AgentClarity，MEAI 客户端可观测 | 商业厂商，验证了 MEAI 接入点 |
| `RagObservability` | 314 | Blazor 本地 RAG 仪表盘，含 token/cost | 功能接近但形态窄、早期 |
| `SmartLLM.Telemetry` | 1 star | AI-native observability for .NET | 极早期，未形成影响 |

**关键判断**：

1. .NET 侧并非无人尝试，但**没有主导者**，且多数包下载量低、功能单一。
2. MEAI 内置 OTel 中间件解决了"有没有 Span"，没有解决"延迟分解、成本归因、流式剖析、AOT 低开销"。
3. 商业厂商（Progress）已进入 MEAI 可观测，说明赛道被验证，但也意味着需要明确差异化，避免与其正面竞争。

### 3.4 竞品功能缺口矩阵

| 能力 | MEAI `UseOpenTelemetry` | Langfuse | Phoenix | OpenLLMetry | Runesvaner 目标 |
| --- | --- | --- | --- | --- | --- |
| GenAI Span | 是 | 是 | 是 | 是 | 是（兼容） |
| Token 计数 | 部分 | 是 | 是 | 是 | 是 |
| 延迟瀑布分解 | 否 | 部分（Span 树） | 部分（Span 树） | 部分 | **是（显式分段）** |
| 成本账本 | 否 | 是（平台侧） | 部分 | 部分 | **是（本地 + 版本化）** |
| 流式 TTFT / ITL | 否 | 部分 | 部分 | 部分 | **是** |
| 重试重复计费检测 | 否 | 否 | 否 | 否 | **是** |
| 缓存折扣归因 | 否 | 部分 | 否 | 部分 | **是** |
| AOT 安全核心 | 未知 | 不适用 | 不适用 | 不适用 | **是** |
| 本地优先 / 零 SaaS | 是（进程内） | 自托管 | 自托管 | 是 | **是** |
| OTLP 互操作 | 是 | 是 | 是 | 是 | **是（可选）** |

> "部分"表示平台具备 Span 树或成本字段，但没有针对 .NET AI 请求的显式分段与归因模型。

---

## 4. 两个切口的评分

评分 1–5，5 为最优。权重体现个人开源项目约束。

| 维度 | 权重 | A: Native AOT 诊断 | B: AI 负载诊断 |
| --- | ---: | ---: | ---: |
| 市场需求明确度 | 20% | 3 | 5 |
| 竞争强度（越低越好） | 15% | 4 | 3 |
| 技术可行性（个人项目） | 20% | 2 | 5 |
| 差异化可持续性 | 15% | 4 | 3 |
| 万星潜力 | 10% | 2 | 4 |
| 与发起人技术偏好匹配 | 10% | 5 | 4 |
| 生态协同（MEAI/SK/OTel） | 10% | 2 | 5 |
| **加权总分** | 100% | **3.1** | **4.2** |

计算（A）：0.2×3 + 0.15×4 + 0.2×2 + 0.15×4 + 0.1×2 + 0.1×5 + 0.1×2 = 0.6 + 0.6 + 0.4 + 0.6 + 0.2 + 0.5 + 0.2 = 3.10（四舍五入 3.1）
计算（B）：0.2×5 + 0.15×3 + 0.2×5 + 0.15×3 + 0.1×4 + 0.1×4 + 0.1×5 = 1.0 + 0.45 + 1.0 + 0.45 + 0.4 + 0.4 + 0.5 = 4.20

> 差额主要来自技术可行性与生态协同，而不是"AI 更热"。

---

## 5. 结论与建议

### 5.1 已决议

**发起人已决议：以切口 B 为主赛道，切口 A 作为一等约束与第二阶段差异化（D-01，2026-11-25）。**

具体表述：

> Runesvaner 是一个 .NET 原生的 AI 请求剖析器。它首先解决"一次 AI 请求的延迟与成本分解"，同时把 Native AOT / Trimming 作为一等公民约束；当主产品站稳后，再以独立实验分支探索 AOT 启动/体积归因。

### 5.2 不推荐的路线

| 路线 | 不推荐原因 |
| --- | --- |
| 直接做"另一个通用 LLM 可观测平台" | Langfuse/Phoenix 已强，正面竞争无胜算 |
| 直接做"Native AOT Profiler" | 堆分析需要 runtime 级工程；EventPipe 已覆盖部分场景 |
| 做"AI 网关 + 可观测" | 需要网络代理、鉴权、配额、运维，超出个人项目带宽 |
| 做"Prompt 管理 + Eval" | 与诊断主线偏离，且平台竞争激烈 |

### 5.3 差异化护城河

1. **显式延迟分段**：不是"Span 树看起来很深"，而是"网络 1.2s、TTFT 2.1s、生成 4.0s、序列化 0.2s"的可读结论。
2. **成本可追溯**：价格表版本、来源、生效时间、未知模型不猜测。
3. **AOT 安全**：核心零反射，CLI AOT；这是 Python/TS 平台无法复制的 .NET 特性。
4. **本地优先**：默认不出机器，适合对数据敏感的企业与个人。
5. **流式与重试语义**：TTFT/ITL、部分响应、重复计费，这些是"平台侧 Span 树"容易忽略的细节。

### 5.4 进入策略

| 阶段 | 动作 |
| --- | --- |
| M1 | 用 MEAI 做一个真实 RAG 示例，录制 30 秒 demo；在 README 首屏展示瀑布图 |
| M2 | 发布 NuGet 预览；在 r/dotnet、Awesome .NET AI、Hacker News 发布技术文章 |
| M3 | 与 Semantic Kernel 社区合作；提交 OTel GenAI 语义约定的 .NET 实现反馈 |
| M4 | 争取 .NET 社区 KOL 评测；发布 1.0 与迁移指南 |

---

## 6. 风险与反证

| 反证 | 回应 |
| --- | --- |
| "MEAI 已经内置 OTel，为什么还需要你？" | 内置只发 Span；Runesvaner 做分段、成本、流式、重试与本地报告，并可导出到 OTel |
| "Langfuse 自托管就够了" | Langfuse 是平台，接入与运维成本高；Runesvaner 是开发者本机工具，5 分钟看到结果 |
| "成本算不准，不如不算" | 价格表版本化 + 未知模型标记 + 来源字段；不猜测，允许用户覆盖 |
| "AOT 用户很少" | AOT 是约束不是卖点；它保证产品在 AOT 服务中不失效，而不是只服务 AOT 用户 |
| "个人项目做不大" | 通过 CLA 引入维护者；范围克制；先做单点极致，再做平台互操作 |

---

## 7. 来源

- [Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Diagnostics and instrumentation for Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics)
- [.NET diagnostics tools overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)
- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters)
- [Microsoft.Extensions.AI libraries](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [OpenTelemetry GenAI semantic conventions](https://github.com/open-telemetry/semantic-conventions-genai)
- [GenAI metrics](https://github.com/open-telemetry/semantic-conventions-genai/blob/main/docs/gen-ai/gen-ai-metrics.md)
- [Langfuse repository](https://github.com/langfuse/langfuse)
- [Arize Phoenix repository](https://github.com/Arize-ai/phoenix)
- [OpenLLMetry repository](https://github.com/traceloop/openllmetry)
- [Semantic Kernel repository](https://github.com/microsoft/semantic-kernel)
- [OpenTelemetry .NET Native AOT size PR #6849](https://github.com/open-telemetry/opentelemetry-dotnet/pull/6849)
- [NuGet search: llm observability](https://azuresearch-usnc.nuget.org/query?q=llm+observability)
- [NuGet: Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI)
- [NuGet: Microsoft.Extensions.VectorData.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.VectorData.Abstractions)

---

## 8. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。与本文件直接相关：

- D-01：AI 负载诊断首发，AOT 为约束与 v2 分支。
- D-02：项目名 Runesvaner。
- D-13：核心零依赖，OTLP 可选。
- D-18：AOT 堆分析保留研究分支，不做主线。
- D-19：Roslyn AOT 分析器在 v2 投入。
