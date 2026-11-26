# 项目章程：Runesvaner

> **中文版（Legacy full version）** — 英文主文档见 [`../00-project-charter.md`](../00-project-charter.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Accepted（D-01–D-25 已决议） |
| 版本 | 1.0 |
| 日期 | 2026-11-25 |
| 作者 | 项目发起人 + CodeArts 设计代理 |
| 受众 | 项目发起人、潜在维护者、早期贡献者 |
| 关联文档 | `01-market-research.md`、`03-prd.md`、`15-decision-log.md` |

---

## 1. 一句话定义

Runesvaner 是一个面向 .NET AI 应用的**进程内性能与成本剖析器**：把一次 LLM / Embedding / 检索请求拆解成可解释的延迟瀑布和成本账单，并在 Native AOT、Trimming、本地优先的约束下保持可用。

## 2. 愿景

让 .NET 开发者面对 AI 负载时，不再只能看到"一个很慢的 Span"或"一张月底账单"，而是能在一次请求的粒度上回答三个问题：

1. **慢在哪一段？**
2. **贵在哪一项？**
3. **下一次改什么？**

## 3. 问题陈述

### 3.1 现有工具的能力边界

.NET 通用诊断工具链解决的是进程级与运行时级问题：

- `dotnet-trace` / `dotnet-counters` / `dotnet-dump` 面向 CPU、GC、线程池、锁、异常、内存转储。
- OpenTelemetry 面向分布式追踪与指标，能给出 Span 起止时间与标准 GenAI 属性。
- Langfuse、Arize Phoenix、OpenLLMetry 面向 LLM 可观测平台，强于 Trace 聚合、评估、Prompt 管理。

它们共同留下三个缝隙：

| 缝隙 | 具体表现 |
| --- | --- |
| 延迟分解 | 只有一个总耗时 Span，无法区分网络、排队、首 Token、生成、工具、向量检索、序列化的占比 |
| 成本归因 | 只有 Token 计数，缺少价格表版本、缓存折扣、重试重复计费、按功能/租户/ Prompt 版本的归因 |
| 运行形态 | .NET AI 服务越来越多使用 Native AOT / Trimming，通用 Profiler 与部分 OTel 组件在此形态下受限或体积昂贵 |

### 3.2 为什么现在

- `Microsoft.Extensions.AI` 已形成 .NET AI 的统一抽象，并在 10.x / 11.x 线提供 `UseOpenTelemetry` 中间件；这为"在中间件层做深度剖析"提供了稳定抓手。
- `Microsoft.Extensions.VectorData` 已进入 10.x / 11.x，RAG 链路可以被统一观测。
- Native AOT 自 .NET 8 起支持 EventPipe，但**托管堆分析仍不支持**，且 EventPipe 是可选组件；AOT 诊断仍是明确的工程缺口。
- 主流 LLM 可观测平台以 Python / TypeScript 为主，.NET 侧只有零散、低下载量的尝试，尚无主导者。

## 4. 目标与非目标

### 4.1 目标（v1）

1. 为 `IChatClient` 与 `IEmbeddingGenerator` 提供低开销、可组合的剖析中间件。
2. 输出请求级延迟瀑布，明确标注每段的测量方式与估算误差。
3. 输出可追溯的成本账单，价格表带版本、来源与生效时间。
4. 支持流式响应诊断：TTFT、Token 间延迟、取消、中断。
5. 默认不采集内容；本地存储；可选 OTLP 导出。
6. 核心包 AOT / Trim 安全，CLI 可 Native AOT 发布。
7. 让"安装 → 运行 → 看到一次请求的瀑布图"在 5 分钟内完成。

### 4.2 非目标（v1）

- 通用 CPU / 内存 Profiler。
- LLM 评估（Eval）、数据集管理、Prompt 实验平台。
- 代理网关或请求转发。
- 托管 SaaS。
- 自动修改业务代码或自动调优。
- 捕获并默认上传 Prompt / Completion 正文。
- 替代 OpenTelemetry、Langfuse、Phoenix、Grafana。

### 4.3 明确暂缓

| 方向 | 原因 | 去向 |
| --- | --- | --- |
| Native AOT 托管堆分析 | 需要深入 runtime / GC，个人项目风险高 | 作为 v2 研究项，不作为首发卖点 |
| 通用 AOT 启动/体积归因 | 与 ILC 工具链耦合深 | 作为独立实验分支 |
| 分布式跨服务 Trace 合并 | OTel 已覆盖 | 通过 OTLP 互操作 |
| AI 辅助根因分析 | 隐私与可信度风险 | v2 可选，本地优先 |

## 5. 目标用户

| 优先级 | 用户 | 核心诉求 |
| --- | --- | --- |
| P0 | .NET 后端工程师 | 定位 RAG/Agent 请求慢在哪、贵在哪 |
| P0 | AI 平台/基础设施团队 | 统一接入、低开销、可导出、AOT 可用 |
| P1 | 独立开发者 / 初创团队 | 零门槛、本地、不要 SaaS 账单 |
| P1 | SRE / FinOps | 成本归因、预算告警、重试重复计费 |
| P2 | 技术负责人 | 模型/版本/功能维度的对比与决策依据 |

## 6. 成功指标

### 6.1 北极星指标

**WADP（Weekly Active Diagnosed Projects）**：每周至少产生一次有效诊断会话的独立项目数。

不以 GitHub Stars 作为唯一成功标准，但设定参考目标：

| 阶段 | 参考目标 |
| --- | --- |
| M1 | 10 个外部试用项目 |
| M2 | 100 个外部试用项目；NuGet 月下载 1,000 |
| M3 | 1,000 个活跃项目；外部贡献者 ≥ 5 |
| 1.0 后 12 个月 | GitHub Stars ≥ 5,000；WADP ≥ 500 |

> Stars 受传播影响大，仅作为滞后指标；WADP 与"诊断后采取行动"的比例才是领先指标。

### 6.2 护栏指标

| 指标 | 门槛 |
| --- | --- |
| 采集开销（无 I/O、采样开启） | 单请求 P95 增量 ≤ 1 ms，CPU ≤ 1% |
| 采集失败对业务影响 | 0（熔断后静默降级） |
| 成本计算可追溯率 | 100% 的账单带价格表版本 |
| AOT Smoke | Windows/Linux 双平台全绿 |
| 内容泄露事件 | 0 |
| P0 缺陷平均修复 | 一个发布周期内 |

## 7. 范围与版本

### 7.1 v1 范围

- Microsoft.Extensions.AI：Chat + Embedding。
- Semantic Kernel：作为 Should。
- Microsoft.Extensions.VectorData：作为 Should。
- 库/应用内 SQLite 默认；CLI AOT 默认 JSONL；失败回退与交换。
- CLI：`run`、`report`、`top`、`export`、`open`。
- 本地 Studio。
- OTLP 导出（可选包）。
- 尾采样、预算告警。

### 7.2 v2 候选

- Agent / MCP 工具调用树。
- 多模态 Token 估算。
- 分布式诊断会话合并。
- Native AOT 启动/体积归因研究分支（社区维护，不承诺交付）。
- Roslyn AOT 分析器（`Runesvaner.Analyzers`）。
- 与 `dotnet-counters` 的进程级指标联动。

## 8. 关键假设与验证

| 假设 | 验证方式 | 失败信号 |
| --- | --- | --- |
| .NET 团队需要请求级延迟分解，而不只是 OTel Span | 10 个深度访谈 + M1 试用反馈 | 用户认为 OTel + Grafana 已足够 |
| 成本归因是刚需，且愿意维护价格表 | 观察价格表更新 Issue / PR 数量 | 无人提交价格修正 |
| AOT 兼容是差异化，而非伪需求 | AOT 用户访谈 + 下载包 TFM 分布 | AOT 用户占比 < 5% |
| 本地优先能打动用户 | 试用转化率 | 用户坚持必须云端协作 |
| MEAI 中间件是正确接入点 | 接入代码行数与留存 | 用户要求直接集成各家 SDK |

## 9. 约束

1. **个人项目**：非隶属组织；维护带宽有限，必须控制范围。
2. **CLA**：允许更多维护者，但需要贡献者签署；CLA 文本需法律审阅。
3. **AOT**：核心路径不得引入反射与动态代码。
4. **零依赖核心**：避免把 OTel SDK 变成强制依赖。
5. **隐私默认**：默认不采集内容，不做隐式上传。
6. **成本可信**：宁可不显示，也不猜测未知模型价格。
7. **文档先行**：开工前所有设计以 Markdown 固化，关键决策需发起人拍板。
8. **平台范围**：Windows 10/11 与 Linux；macOS 非目标（D-15）。

## 10. 治理原则

- **BDFL + 维护者**：发起人拥有最终决策权；日常合并由维护者按 lazy consensus 处理。
- **RFC 流程**：影响公共 API、数据模型、许可证、治理的变更必须走 RFC。
- **公开路线图**：里程碑与退出标准公开，避免"黑箱优先级"。
- **贡献者体验**：一键构建、可复现基准、清晰的 good first issue。
- **安全优先**：漏洞私下披露，修复后再公开细节。

## 11. 与两个初始切口的关系

发起人提出两个候选切口：

1. Native AOT 兼容诊断器。
2. 面向 AI 负载的诊断工具。

发起人已于 2026-11-25 决议（D-01）：**以 AI 负载诊断为主赛道，把 Native AOT 作为一等公民约束与第二阶段差异化**。

理由摘要（完整论证见 `01-market-research.md`）：

- Native AOT 的 EventPipe 已在 .NET 8 起可用，传统工具并非完全失效；真正空白是托管堆分析，技术风险与个人项目不匹配。
- AI 负载诊断有明确、可验证、可收费（或可赞助）的用户价值，且 .NET 侧竞争薄弱。
- AOT 兼容是 AI 诊断产品的"护城河"而非"主战场"：它让 Runesvaner 在 AOT 部署的 AI 服务中仍然可用，而多数 OTel 方案在此形态下体积与兼容性吃紧。

## 12. 终止 / 转向条件（Kill Criteria）

出现以下任一情况，应暂停或转向，而不是继续堆功能：

1. M2 结束后，外部试用项目 < 10，且访谈中"愿意推荐"比例 < 30%。
2. 采集开销连续两个里程碑不达标（P95 > 2 ms 或 CPU > 2%）。
3. 成本计算被证明不可信（同一请求重复计算差异 > 5% 且无法解释）。
4. Microsoft 官方在 MEAI 中直接提供等价的延迟分解与成本归因，且开源免费。
5. 维护者数量长期为 0，发起人连续两个季度无法投入。

转向选项：

- 收缩为"成本账本"单一功能库。
- 收缩为"Native AOT 启动/体积归因"工具。
- 转为纯 OTLP Processor，寄生在现有 OTel 生态中。

## 13. 决议状态

D-01–D-25 已全部决议，记录见 `15-decision-log.md`。与本章程直接相关：

- D-01 主赛道：AI 负载诊断首发，AOT 为约束与 v2 分支。
- D-02 项目名：Runesvaner。
- D-03 许可证：Apache-2.0。
- D-04 CLA：ICLA + CCLA。
- D-05 目标框架：`net10.0`，M3 评估 `net11.0`。
- D-07 存储：库 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
- D-10 商业化：暂不商业化，接受无捆绑赞助。

## 14. 参考

- [.NET Native AOT diagnostics](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/diagnostics)
- [Microsoft.Extensions.AI libraries](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [OpenTelemetry GenAI semantic conventions](https://github.com/open-telemetry/semantic-conventions-genai)
- [.NET diagnostics overview](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)
