# 路线图与里程碑

| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 发起人、维护者、贡献者 |
| 关联文档 | `03-prd.md`、`12-testing-and-benchmarks.md`、`15-decision-log.md` |

> 本文档不给出日历工期，只定义**顺序、范围与退出标准**。每个里程碑必须满足退出标准才能进入下一个，避免"功能堆完但不可用"。

---

## 1. 里程碑总览

| 里程碑 | 主题 | 核心交付 | 退出标准 |
| --- | --- | --- | --- |
| M0 | 设计冻结 | 文档、决策、治理、仓库骨架 | D-01–D-25 决议；空解决方案可构建；CI 跑通 |
| M1 | Walking Skeleton | MEAI Chat + SQLite（库）/ JSONL（CLI）+ CLI + AOT smoke | 一条真实会话端到端可读；AOT 0 警告 |
| M2 | MVP | 成本、流式、Studio、OTLP、隐私 | 内测 10 项目；开销 ≤ 1 ms |
| M3 | Beta | SK、VectorData、尾采样、预算、文档站 | NuGet 预览；外部贡献者 ≥ 3 |
| M4 | 1.0 | API 冻结、安全审计、Windows/Linux AOT | 1.0 发布；P0 清零 |
| v2 | 扩展 | Agent/MCP、多模态、AOT 实验 | 单独 RFC |

---

## 2. M0：设计冻结

### 2.1 目标

在写第一行业务代码前，把产品、技术、治理的关键决策固化，避免返工。

### 2.2 范围

| 工作项 | 交付物 |
| --- | --- |
| 需求与竞品 | `00`–`03` 文档 |
| 技术设计 | `05`–`08` 文档 |
| 安全与治理 | `09`–`10` 文档 |
| 决策 | `15-decision-log.md`，D-01–D-25 已决议 |
| 命名 | `16-brand-naming.md`，完成 NuGet/GitHub 初筛；商标暂缓 |
| 仓库骨架 | `Runesvaner.sln`、`Directory.Build.props`、`global.json`、`.editorconfig` |
| 治理文件 | `LICENSE`、`CONTRIBUTING.md`、`CODE_OF_CONDUCT.md`、`SECURITY.md`、`CLA.md` |
| CI 草案 | build + test + markdown lint |
| 环境勘测 | .NET SDK、Windows/Linux runner、AOT 工具链可用性 |

### 2.3 退出标准

1. D-01–D-25 已全部决议（见 `15-decision-log.md`）。
2. 仓库可 `dotnet build` 空解决方案。
3. CI 在 Windows/Linux 跑通 hello world。
4. LICENSE、CLA、SECURITY 文件就位（CLA 经法律审阅或明确标注"待审阅"）。
5. 命名初筛完成：NuGet 根 ID 可用；GitHub 仓库名可用；商标检索暂缓（用户决定，风险已记录）。
6. 至少 5 个潜在用户访谈完成，验证核心假设（见 `02`）。

### 2.4 不做什么

- 不写业务代码。
- 不做 UI 高保真。
- 不发布 NuGet 包。
- 不承诺日期。

---

## 3. M1：Walking Skeleton

### 3.1 目标

用最小端到端路径证明架构可行：一次 MEAI Chat 请求 → 采集 → SQLite（库默认）/ JSONL（CLI AOT 默认） → CLI 报告 → AOT 运行。

### 3.2 范围

| 模块 | 内容 |
| --- | --- |
| `Runesvaner.Abstractions` | 核心模型、枚举、接口 |
| `Runesvaner` | Recorder、Clock、Segmenter、RingBuffer、SelfMonitor |
| `Runesvaner.MicrosoftExtensionsAI` | Chat 非流式 + 流式装饰器（最小） |
| `Runesvaner.Storage.Sqlite` | 库/应用内默认存储、WAL、保留策略 |
| `Runesvaner.Storage.Jsonl` | CLI AOT 默认、回退与交换 |
| `Runesvaner.Cli` | `run`、`report`、`doctor` |
| 测试 | 单元 + 集成 + AOT smoke |
| 示例 | Console + RAG（mock provider） |
| 基准 | 基线采集开销 |

### 3.3 退出标准

1. 运行 sample，生成一条完整 `request.end` 事件。
2. `runesvaner report` 能输出总耗时、分段、Token（mock）。
3. 流式请求的 chunk 顺序、内容、取消语义与未接入时一致。
4. `dotnet publish -r linux-x64 /p:PublishAot=true` 0 个 AOT/Trim 警告。
5. AOT 二进制 smoke 通过（非流式 + 流式 + 错误路径）。
6. 采集失败注入测试：业务请求仍成功。
7. 开销基线：P95 ≤ 2 ms（M1 临时门槛，M2 收紧到 1 ms）。
8. SQLite（库）与 JSONL（CLI）schema 通过 golden 测试；记录 SQLite AOT 实测结果。
9. README 的"30 秒体验"可实际执行（除成本/Studio 外）。

### 3.4 风险

| 风险 | 缓解 |
| --- | --- |
| MEAI 流式包装破坏取消 | 专项测试；最小包装 |
| Activity/DI 反射警告 | 显式注册；AOT 分析 |
| SQLite AOT（#36068） | CLI 默认 JSONL；库失败回退；M1 验证 workaround |
| 分段估算不准 | 先只做实测段，估算段标 estimated |

### 3.5 不做什么

- 成本引擎。
- Studio。
- OTLP。
- SK / VectorData。

---

## 4. M2：MVP

### 4.1 目标

让外部用户可以在真实项目中完成"看到瀑布 + 核对成本 + 本地 UI"的闭环。

### 4.2 范围

| 模块 | 内容 |
| --- | --- |
| 成本引擎 | 价格表、缓存/推理/批量、未知模型、版本回放 |
| 流式 | TTFT、ITL、TPS、取消、中断 |
| 重试 | attempt、退避、重复计费标记 |
| 存储 | SQLite 查询优化 + 框架依赖 CLI 评估 |
| CLI | `top`、`export`、`open` |
| Studio | 会话列表、请求瀑布、成本账本、Health |
| 导出 | OTLP（可选包） |
| 隐私 | 内容捕获级别、哈希、脱敏、本地 token |
| 采样 | 头采样 + 自监控 |
| 测试 | 基准门槛、AOT 矩阵、Studio 安全测试 |

### 4.3 退出标准

1. 成本与至少 2 家 provider 的官方账单样例核对，误差 ≤ 5%。
2. 未知模型不显示 0，显示 `—` 与原因。
3. 流式 TTFT 与人工测量误差 ≤ 10 ms（本地 mock）。
4. Studio 首屏 ≤ 1 s（1 万请求数据集）。
5. Studio 仅绑定 `127.0.0.1`，无 token 无法访问 API。
6. 开销 P95 ≤ 1 ms，CPU ≤ 1%（基准场景）。
7. AOT Windows/Linux smoke 全绿。
8. 内测 10 个项目，至少 6 个完成一次真实诊断。
9. 隐私测试：默认存储不含 Prompt/Completion 原文。
10. OTLP 导出到本地 collector 成功，字段映射通过 golden 测试。

### 4.4 风险

| 风险 | 缓解 |
| --- | --- |
| 成本价格表错误 | 版本化、来源、核对视图、用户覆盖 |
| Studio 安全漏洞 | 本地 token、Host 校验、CSP、渗透测试 |
| SQLite AOT 失败 | 自动回退 JSONL + ADR |
| 性能回归 | CI 基准门槛；采样；熔断 |
| 范围膨胀 | 严格按 Must 清单；Should 延后 |

### 4.5 不做什么

- SK / VectorData。
- 尾采样。
- 预算告警。
- 分布式合并。
- Agent 工具树。
- 商业支持。

---

## 5. M3：Beta

### 5.1 目标

扩展生态覆盖，建立社区与文档，准备 1.0。

### 5.2 范围

| 模块 | 内容 |
| --- | --- |
| Semantic Kernel | Filter 集成、工具调用 |
| VectorData | 检索 Span、collection 维度 |
| 尾采样 | 错误/慢/超预算保留 + 加权 |
| 预算 | 请求/租户/功能阈值 + 告警 |
| 文档站 | 快速开始、概念、API、故障排查、迁移 |
| 社区 | Issue/PR 模板、good first issue、Discussions |
| 兼容 | MEAI 10.x/11.x 契约测试 |
| 导出 | OTLP 验证；第三方按需（D-20） |

### 5.3 退出标准

1. SK 与 MEAI 同时启用时不产生重复请求记录。
2. VectorData 检索 Span 的 collection、top_k、耗时正确。
3. 尾采样在错误/慢请求上 100% 保留；聚合使用加权。
4. 预算超限产生事件与 UI 提示，不阻断请求。
5. 文档站可搜索、可导航、含 5 个端到端示例。
6. NuGet 预览包发布，安装成功率 ≥ 95%（内测）。
7. 外部贡献者 ≥ 3，合并 PR ≥ 10。
8. 兼容性矩阵全绿（TFM × OS × AOT）。
9. 至少 1 篇技术文章/演讲发布。

### 5.4 风险

| 风险 | 缓解 |
| --- | --- |
| SK API 变更 | 适配器隔离；契约测试 |
| 尾采样内存膨胀 | 只缓冲轻量记录；上限 |
| 文档滞后 | 文档与代码同 PR |
| 社区冷启动 | 主动 outreach；good first issue |
| 兼容矩阵爆炸 | 只支持 LTS + 最新 STS |

---

## 6. M4：1.0

### 6.1 目标

稳定公共 API、完成安全与性能审计、建立可持续治理。

### 6.2 范围

| 工作 | 内容 |
| --- | --- |
| API 冻结 | 公共 API Review、兼容性基线 |
| 安全 | 依赖审计、Studio 渗透测试、威胁模型复核 |
| 性能 | Windows/Linux 基准、开销报告 |
| 质量 | P0/P1 清零、文档完整 |
| 治理 | CLA 流程、维护者 ≥ 2、RFC 模板 |
| 发布 | 1.0 NuGet、迁移指南、发行说明 |
| 运营 | 官网/文档站、支持渠道 |

### 6.3 退出标准

1. 公共 API 冻结，`PublicApiAnalyzers` 基线通过。
2. 破坏性变更清单为空（或已文档化）。
3. 安全审计无高危未修复项。
4. Windows/Linux AOT smoke + 基准全绿。
5. P0 = 0，P1 ≤ 3 且有计划。
6. 文档覆盖所有 Must 功能与故障排查。
7. CLA 自动检查生效；至少 2 名维护者可发布。
8. 1.0 发布公告与迁移指南。
9. 至少 1,000 个活跃项目或 5,000 Stars（二者取一，作为参考）。

### 6.4 风险

| 风险 | 缓解 |
| --- | --- |
| 过早 1.0 | 以退出标准为准，不按日期 |
| API 设计缺陷 | M2/M3 预览期收集反馈 |
| 安全审计预算 | 提前预留；社区审计 |
| 维护者不足 | 贡献者培养；自动化 |

---

## 7. v2 候选

| 方向 | 价值 | 依赖 | 决策 |
| --- | --- | --- | --- |
| Agent / MCP 工具树 | 高 | SK/MCP 稳定 | D-20 |
| 多模态 Token | 中 | provider 支持 | D-08 |
| 分布式会话合并 | 中 | OTel 上下文 | D-13 |
| Native AOT 启动/体积 | 中高 | 独立研究分支 | D-18：社区维护，不承诺交付 |
| Roslyn AOT 分析器 | 中高 | Roslyn | D-19：v2 投入，欢迎社区维护 |
| 评估互操作 | 中 | 按需 | D-20：非必须 |
| 托管服务 | 低（与定位冲突） | — | 不建议 |

---

## 8. 里程碑治理

- 每个里程碑开始前，在 GitHub Milestone 建立任务清单。
- 每两周更新进度与风险。
- 退出标准未满足不得进入下一里程碑；可缩减范围，但需记录决策。
- 任何新增 Must 功能必须替换一个现有 Must，或推迟里程碑。

---

## 9. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md` 与 `19-open-implementation-decisions.md`。与本路线图直接相关：

- D-07：库/应用内 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
- D-14：本地 100%，生产建议 10%；尾采样 M3。
- D-15：Windows/Linux；macOS 非目标。
- D-17：LTS + 最新 STS。
- D-18/D-19：研究分支与 v2 投入。
- D-23：GitHub Pages；M1 不买域名。
- D-24：win-arm64 非目标；linux-arm64 可选。
- D-25：商标暂缓；NuGet M0 占位；CLA 首次外部 PR 前审阅。