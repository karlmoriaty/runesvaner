# 产品需求文档（PRD）：Runesvaner

| 字段 | 值 |
| --- | --- |
| 文档状态 | Accepted（D-01–D-25 已决议） |
| 版本 | 1.0 |
| 日期 | 2026-11-25 |
| 产品阶段 | M0 设计 |
| 受众 | 产品、工程、设计、早期贡献者 |
| 关联文档 | `00-project-charter.md`、`02-problem-and-personas.md`、`05-technical-design.md` |

---

## 1. 产品概述

Runesvaner 是一个 .NET 原生的 AI 请求剖析器。它通过 Microsoft.Extensions.AI 中间件采集一次 AI 请求的内部阶段、Token 用量、成本与流式行为，输出可读的延迟瀑布与成本账单，并可选择导出为 OpenTelemetry。

### 1.1 产品目标

1. 让开发者在 5 分钟内回答"这次请求慢在哪、贵在哪"。
2. 让成本归因可追溯、可解释、可核对。
3. 在 Native AOT / Trimming 下保持可用。
4. 默认保护内容隐私，不强制依赖云端。

### 1.2 成功指标

| 指标 | M2 目标 | 1.0 目标 |
| --- | --- | --- |
| 首次看到瀑布的时间 | ≤ 5 分钟 | ≤ 3 分钟 |
| 采集开销 P95 | ≤ 1 ms | ≤ 0.5 ms |
| 成本核对误差 | ≤ 5% | ≤ 2% |
| 外部试用项目 | 100 | 1,000 |
| AOT Smoke 通过率 | 100% | 100% |

---

## 2. 用户故事与 Epic

| Epic | 用户故事 |
| --- | --- |
| E1 接入 | 作为 .NET 开发者，我想用 3 行代码接入，以便不改变现有架构 |
| E2 延迟 | 作为后端工程师，我想看到请求内部分段，以便定位慢点 |
| E3 成本 | 作为 FinOps，我想按维度归因成本，以便解释账单 |
| E4 流式 | 作为 AI 工程师，我想看到 TTFT 与 Token 速率，以便优化体验 |
| E5 本地诊断 | 作为独立开发者，我想在本机看到结果，以便不部署平台 |
| E6 AOT | 作为平台工程师，我想在 AOT 服务中使用，以便不牺牲构建 |
| E7 互操作 | 作为平台团队，我想导出 OTLP，以便复用现有后端 |

---

## 3. 功能需求清单

优先级：Must（M）、Should（S）、Could（C）、Won't（W）。

### 3.1 接入与配置

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-001 | 提供 `UseRunesvaner()` MEAI 中间件 | M | 支持 `IChatClient` 与 `IEmbeddingGenerator` |
| FR-002 | 提供 `AddRunesvaner()` DI 扩展 | M | 注册采集器、存储、选项 |
| FR-003 | 支持代码配置与 `appsettings.json` | M | 配置节 `Runesvaner` |
| FR-004 | 支持按请求覆盖关键选项 | S | 通过 `Activity` 标签或上下文 |
| FR-005 | 支持无 DI 的独立使用 | S | 控制台/脚本场景 |
| FR-006 | 提供接入诊断命令 | S | `runesvaner doctor` 检查版本与配置 |

### 3.2 采集与延迟分解

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-010 | 采集 Chat 请求根 Span | M | 包含 provider、model、operation |
| FR-011 | 采集 Embedding 请求根 Span | M | 包含批量大小、维度 |
| FR-012 | 生成延迟瀑布分段 | M | 至少 6 段：Queue、Connect、TLS、Upload、TTFT、Generation |
| FR-013 | 标注估算分段 | M | `estimated=true` + 测量方法 |
| FR-014 | 计算未解释时间 | M | `unaccounted` 与 `overlap` |
| FR-015 | 采集向量检索 Span | S | 通过 MEVD 或自定义 Instrumentation |
| FR-016 | 采集工具调用 Span | S | Function Calling / MCP |
| FR-017 | 采集序列化耗时 | S | 请求/响应序列化 |
| FR-018 | 采集重试链 | M | attempt、退避、错误类型 |
| FR-019 | 采集 DNS/TCP/TLS 事件 | S | 基于 `HttpClient` 诊断事件 |

### 3.3 成本

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-020 | 记录 Input/Output Token | M | 来自 provider usage |
| FR-021 | 记录 Cached/Reasoning Token | M | 若 provider 提供 |
| FR-022 | 内置版本化价格表 | M | 至少覆盖 OpenAI、Azure OpenAI、Anthropic |
| FR-023 | 支持用户覆盖价格 | M | JSON/YAML 文件 |
| FR-024 | 计算分项与总成本 | M | decimal 精度，USD 存储 |
| FR-025 | 未知模型标记 | M | 不猜测，`cost.status=unknown` |
| FR-026 | 缓存折扣归因 | S | cached input 单价 |
| FR-027 | 批量/分层计价 | S | tiered pricing、batch discount |
| FR-028 | 重试重复计费检测 | M | 标记可能重复的 attempt |
| FR-029 | 成本核对视图 | S | 与 provider 账单 CSV 对比 |
| FR-030 | 预算与告警 | S | 请求/租户/功能维度阈值 |

### 3.4 流式

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-031 | 计算 TTFT | M | 首个非空 delta |
| FR-032 | 计算 Token 间延迟（ITL） | M | P50/P95 |
| FR-033 | 计算 Tokens/s | M | 输出 Token / 生成时长 |
| FR-034 | 记录取消与中断 | M | 区分用户取消、超时、provider 错误 |
| FR-035 | 记录部分响应用量 | S | 尽力而为，标记估算 |

### 3.5 存储与 CLI

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-040 | SQLite 默认存储（库/应用内） | M | 查询、聚合、索引；Studio 数据源 |
| FR-041 | JSONL（CLI AOT 默认、回退与交换） | M | CLI 默认、AOT 回退、导入导出、崩溃恢复 |
| FR-042 | 数据保留策略 | M | 时间 + 大小双阈值 |
| FR-043 | CLI `runesvaner run` | M | 启动应用并采集 |
| FR-044 | CLI `runesvaner report` | M | 终端输出摘要 |
| FR-045 | CLI `runesvaner top` | S | 慢请求/高成本排行 |
| FR-046 | CLI `runesvaner export` | M | JSONL/OTLP |
| FR-047 | CLI `runesvaner open` | M | 启动本地 Studio |
| FR-048 | 离线导入 | C | 从 OTLP/JSONL 导入分析 |

### 3.6 本地 Studio

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-050 | 会话列表 | M | 按时间、状态、模型筛选 |
| FR-051 | 请求瀑布图 | M | 分段、并行 lane、估算标记 |
| FR-052 | 请求详情面板 | M | 属性、Token、成本、错误 |
| FR-053 | 成本账本页 | M | 分项、维度汇总、异常 |
| FR-054 | 对比视图 | S | 两次请求/两个版本 |
| FR-055 | 键盘导航 | S | Cmd/Ctrl+K、J/K、Enter |
| FR-056 | 导出报告 | S | 单请求 HTML/JSON |
| FR-057 | 本地令牌与回环绑定 | M | 默认 `127.0.0.1` |
| FR-058 | 主题 | C | 深色默认，浅色可选 |

### 3.7 导出与互操作

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-060 | OTLP 导出（可选包） | M | Trace + Metrics |
| FR-061 | GenAI 语义约定对齐 | M | `gen_ai.*` 字段 |
| FR-062 | Langfuse 兼容验证 | C | 按需，不承诺 |
| FR-063 | Phoenix 兼容验证 | C | 按需，不承诺 |
| FR-064 | Prometheus 指标端点 | C | 可选 |

### 3.8 采样与可靠性

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-070 | 头采样 | M | 概率 + 规则 |
| FR-071 | 尾采样 | S | 错误/慢/超预算保留 |
| FR-072 | 采样加权统计 | S | 避免成本统计失真 |
| FR-073 | 采集熔断 | M | 队列满/落盘失败时降级 |
| FR-074 | 自监控指标 | M | 队列长度、丢弃数、落盘延迟 |

### 3.9 隐私与安全

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-080 | 默认不采集内容 | M | `ContentCapture.None` |
| FR-081 | 元数据模式 | M | 长度、哈希、Token 估算 |
| FR-082 | 完整内容模式 | S | 显式开启，醒目警告 |
| FR-083 | 敏感 Header 黑名单 | M | Authorization、api-key、cookie |
| FR-084 | 导出脱敏 | S | 可配置规则 |
| FR-085 | 本地数据权限 | M | 文件权限 0600 / 用户目录 |

### 3.10 AOT 与兼容

| ID | 需求 | 优先级 | 说明 |
| --- | --- | --- | --- |
| FR-090 | 核心库 `IsAotCompatible` | M | 无 AOT/Trim 警告 |
| FR-091 | CLI Native AOT 发布 | M | Windows/Linux |
| FR-092 | JSON Source Generator | M | 不使用反射序列化 |
| FR-093 | 配置绑定 Source Generator | M | 避免反射绑定 |
| FR-094 | AOT Smoke 测试 | M | 每次 CI |

---

## 4. Must 功能验收标准

采用 Given / When / Then。`AC-xxx` 可在测试中直接映射。

### 4.1 FR-001 MEAI 中间件

- **AC-001** Given 一个 `IChatClient` 管线，When 调用 `UseRunesvaner()`，Then 不改变原有调用语义与返回值。
- **AC-002** Given 一个流式请求，When 枚举 `IAsyncEnumerable`，Then 每个 chunk 按原顺序、原内容返回。
- **AC-003** Given 采集器抛出异常，When 业务请求继续，Then 请求成功且记录一条自监控错误。
- **AC-004** Given 未配置存储，When 调用，Then 使用默认本地路径且不阻断请求。

### 4.2 FR-012 延迟瀑布

- **AC-010** Given 一次完成的 Chat 请求，When 生成报告，Then 至少包含 Queue、Connect、TLS、Upload、TTFT、Generation 六段。
- **AC-011** Given 无法精确测量的分段，When 展示，Then `estimated=true` 且提供 `measurement.method`。
- **AC-012** Given 分段存在并行重叠，When 展示，Then 显示 `overlap`，不强行归一化。
- **AC-013** Given 分段之和与总耗时差异 > 5%，When 展示，Then 显示 `unaccounted` 并标注差异。

### 4.3 FR-022 / FR-024 / FR-025 成本

- **AC-020** Given 已知 provider、model 与价格表版本，When 请求完成，Then 输出 input/output/cached/reasoning 分项与总计。
- **AC-021** Given 未知模型，When 请求完成，Then `cost.status=unknown`，总成本为 null，不猜测。
- **AC-022** Given 价格表更新，When 查看历史会话，Then 使用该会话记录的 `price_table_version` 回放。
- **AC-023** Given 金额计算，When 存储与展示，Then 使用 decimal，保留至少 6 位小数，展示四舍五入到 4 位。

### 4.4 FR-028 重试

- **AC-030** Given 首次请求返回 429、第二次成功，When 生成账单，Then 记录 `attempt=2`，成功成本只计一次。
- **AC-031** Given provider 对失败请求计费，When 响应头或错误体表明，Then 记录 `billing.status=billed`。
- **AC-032** Given 无法判断失败请求是否计费，When 生成账单，Then 标记 `billing.status=unknown`，不计入总计但在报告中提示。

### 4.5 FR-031 流式

- **AC-040** Given 流式响应，When 首个非空 delta 到达，Then TTFT = 请求开始到该 delta 的单调时钟差。
- **AC-041** Given 流式响应完成，When 生成报告，Then 输出 chunk 数、ITL P50/P95、Tokens/s。
- **AC-042** Given 用户取消，When 流关闭，Then 记录 `cancelled=true` 与已接收 chunk 数。
- **AC-043** Given 流中断，When 异常抛出，Then 保留已接收数据并记录错误类型。

### 4.6 FR-040 存储与 FR-042 保留

- **AC-050** Given 进程崩溃，When 重启读取 JSONL，Then 已完整写入的行可读，半行被安全忽略。
- **AC-051** Given 超过保留时间或大小，When 清理执行，Then 删除最旧会话且不删除当前活跃会话。
- **AC-052** Given 磁盘满，When 写入失败，Then 采集熔断，业务请求不受影响。

### 4.7 FR-050 / FR-051 Studio

- **AC-060** Given 本地会话数据，When 打开 Studio，Then 首屏显示最近会话与总成本。
- **AC-061** Given 选择一个请求，When 打开瀑布，Then 每段显示名称、耗时、占比与估算标记。
- **AC-062** Given 无数据，When 打开 Studio，Then 显示空状态与"如何生成第一条会话"的命令。
- **AC-063** Given 默认配置，When 访问 Studio，Then 仅绑定 `127.0.0.1`，并要求本地令牌。

### 4.8 FR-080 隐私

- **AC-070** Given 默认配置，When 请求完成，Then 存储中不包含 prompt/completion 原文。
- **AC-071** Given `ContentCapture.Full`，When 开启，Then UI 与日志显示醒目警告，默认保留 7 天。
- **AC-072** Given 请求头包含 `Authorization`，When 采集，Then 该头被丢弃或脱敏。

### 4.9 FR-090 / FR-091 AOT

- **AC-080** Given `dotnet publish -r linux-x64 /p:PublishAot=true`，When 构建 sample，Then 0 个 AOT/Trim 警告。
- **AC-081** Given AOT 二进制，When 运行 smoke 请求，Then 生成一条完整会话且字段完整。
- **AC-082** Given CLI AOT 发布，When 执行 `runesvaner report`，Then 正常输出且无反射异常。

---

## 5. 非功能需求

### 5.1 性能

| ID | 需求 | 门槛 |
| --- | --- | --- |
| NFR-001 | 采集增量延迟 | P95 ≤ 1 ms（不含 provider） |
| NFR-002 | CPU 开销 | ≤ 1%（采样开启、无导出） |
| NFR-003 | 分配 | ≤ 2 KB/请求（不含 provider 与用户对象） |
| NFR-004 | 落盘 | 异步，不阻塞请求线程 |
| NFR-005 | 内存 | 环形缓冲上限可配置，默认 ≤ 16 MB |

### 5.2 可靠性

| ID | 需求 |
| --- | --- |
| NFR-010 | 采集失败不得导致业务失败 |
| NFR-011 | 队列满时丢弃最旧事件并计数 |
| NFR-012 | 存储损坏时可启动并提示修复 |
| NFR-013 | 支持优雅关闭与 flush |

### 5.3 隐私与安全

| ID | 需求 |
| --- | --- |
| NFR-020 | 默认不采集内容 |
| NFR-021 | 本地 UI 仅回环绑定 |
| NFR-022 | 本地令牌随机生成，进程退出失效 |
| NFR-023 | 导出前支持脱敏 |
| NFR-024 | 不记录密钥、Cookie、Authorization |

### 5.4 兼容性

| ID | 需求 |
| --- | --- |
| NFR-030 | TFM：`net10.0`；评估 `net11.0` |
| NFR-031 | MEAI：10.x 与 11.x |
| NFR-032 | OS：Windows 10/11 x64/arm64、Linux x64/arm64；macOS 非目标（D-15） |
| NFR-033 | Native AOT / Trimming |
| NFR-034 | 不强制依赖 OpenTelemetry SDK |

### 5.5 可用性与可访问性

| ID | 需求 |
| --- | --- |
| NFR-040 | 首次体验 ≤ 5 分钟 |
| NFR-041 | 错误信息包含原因与下一步 |
| NFR-042 | WCAG 2.2 AA |
| NFR-043 | 键盘可完成主要操作 |
| NFR-044 | 深色默认，浅色可选；UI 支持 en-US / zh-Hans / de-DE |

### 5.6 可维护性

| ID | 需求 |
| --- | --- |
| NFR-050 | 公共 API 经 API Review |
| NFR-051 | SemVer 2.0 |
| NFR-052 | 实验性 API 使用 `[Experimental]` |
| NFR-053 | 每个公共包有 XML 文档 |
| NFR-054 | 基准与 AOT 回归在 CI |

---

## 6. 数据与隐私要求

| 数据类别 | 默认 | 可选 | 保留 |
| --- | --- | --- | --- |
| 时间与耗时 | 采集 | — | 7 天 / 1 GB |
| Token 用量 | 采集 | — | 7 天 / 1 GB |
| 成本 | 采集 | — | 7 天 / 1 GB |
| Provider / Model | 采集 | — | 7 天 / 1 GB |
| Tenant / User | 默认哈希 | 原文（显式） | 7 天 / 1 GB |
| Prompt / Completion | 不采集 | 元数据 / 完整 | 完整模式 7 天 |
| 请求头 | 黑名单过滤 | — | 不存储 |
| 异常堆栈 | 采集（可选） | — | 7 天 / 1 GB |

---

## 7. 错误处理与降级

| 场景 | 行为 | 用户可见性 |
| --- | --- | --- |
| 采集器异常 | 捕获、计数、继续 | Studio 自监控页 |
| 队列满 | 丢弃最旧、计数 | 顶部警告 |
| 落盘失败 | 熔断 30 秒后重试 | 顶部警告 |
| 未知模型价格 | 标记 unknown | 成本列显示 `—` + 提示 |
| 价格表缺失 | 使用内置快照 | 提示版本 |
| Studio 端口占用 | 自动选择空闲端口 | CLI 输出 URL |
| AOT 不兼容依赖 | 构建警告 | 文档列出替代方案 |
| SQLite 初始化失败 | 回退 JSONL 并计数 | Health 页警告 |

---

## 8. 依赖与假设

| 依赖 | 用途 | 风险 |
| --- | --- | --- |
| Microsoft.Extensions.AI | 接入点 | 版本演进快，需多版本测试 |
| Microsoft.Extensions.VectorData | 向量检索 | 仍在演进 |
| System.Text.Json | 序列化 | 需 Source Generator |
| Microsoft.Data.Sqlite | SQLite | 原生依赖，AOT 需验证 |
| OpenTelemetry SDK | 可选导出 | 体积与 AOT 成本 |
| 价格数据 | 成本计算 | 来源合法性与时效性 |

---

## 9. 发布标准（Definition of Done）

一个 Must 功能完成需满足：

1. 代码 + 单元测试 + 集成测试。
2. 公共 API 有 XML 文档与示例。
3. 无新增 AOT/Trim 警告。
4. 性能基准无 > 10% 回归。
5. 隐私影响评估完成。
6. 文档更新（README / docs）。
7. 变更日志条目。

1.0 额外要求：

1. 公共 API 冻结与兼容性测试。
2. 安全审计（依赖、密钥、路径穿越、DNS rebinding）。
3. Windows/Linux 双平台 AOT Smoke。
4. CLA 流程可用。
5. 迁移指南与故障排查手册。

---

## 10. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。本 PRD 的实现依据：

- 主赛道：AI 负载诊断首发（D-01）。
- 项目名：Runesvaner（D-02）。
- 许可证 / CLA：Apache-2.0 / ICLA+CCLA（D-03 / D-04）。
- 目标框架：`net10.0`，M3 评估 `net11.0`（D-05）。
- UI：React 19 + Redux Toolkit + RTK Query（D-06）。
- 存储：库/应用内 SQLite 默认；CLI AOT 默认 JSONL；失败回退（D-07）。
- 价格表：内置快照 + 用户覆盖，远端关闭（D-08）。
- 内容捕获：默认 `None`（D-09）。
- 商业化：暂不商业化（D-10）。
- OTel：核心零依赖，可选导出（D-13）。
- 采样：本地 100%，生产建议 10%（D-14）。
- 平台：Windows/Linux（D-15）。
- 语言：英文 / 简体中文 / 标准德语（D-16）。
| 是否做 AOT 堆分析 | D-18 |