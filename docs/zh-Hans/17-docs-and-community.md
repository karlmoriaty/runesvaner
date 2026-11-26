# 文档与社区运营

> **中文版（Legacy full version）** — 英文主文档见 [`../17-docs-and-community.md`](../17-docs-and-community.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 发起人、维护者、文档贡献者 |
| 关联文档 | `10-open-source-governance.md`、`11-roadmap.md`、`15-decision-log.md` |

---

## 1. 目标

1. 让新用户在 **5 分钟**内看到第一条诊断会话。
2. 让工程师在 **30 分钟**内理解数据模型与接入方式。
3. 让贡献者在 **1 小时**内完成本地构建与第一个 PR。
4. 让文档与代码**同源、同版本、可验证**。

---

## 2. 文档站技术选型

| 选项 | 优点 | 缺点 | 推荐 |
| --- | --- | --- | --- |
| Astro Starlight | 轻、Markdown 优先、可定制、构建快 | 生态较新 | **推荐** |
| VitePress | Vue 生态、快 | 与 React UI 分离 | 备选 |
| Docusaurus | 成熟、插件多 | 重、React 版本耦合 | 备选 |
| DocFX | .NET 原生、API 参考强 | 视觉老旧、定制难 | 仅 API 参考 |

已决议（D-21）：**Astro Starlight** 作为文档站；API 参考用 DocFX 生成后嵌入，或直接用 XML 文档 + 手写摘要。

已决议（D-23）：托管使用 **GitHub Pages**；M1 暂不购买自定义域名；默认 URL `https://karlmoriaty.github.io/runesvaner/`。

> 决策 D-21 / D-23。

---

## 3. 信息架构

    docs site
    ├── Getting Started
    │   ├── 5-minute quickstart
    │   ├── Install
    │   └── First diagnosis
    ├── Concepts
    │   ├── Diagnostic session
    │   ├── Request & segments
    │   ├── Cost ledger
    │   ├── Sampling
    │   └── Privacy model
    ├── Guides
    │   ├── Microsoft.Extensions.AI
    │   ├── Semantic Kernel
    │   ├── VectorData / RAG
    │   ├── Streaming
    │   ├── Retries & caching
    │   ├── Native AOT
    │   └── OTLP export
    ├── Reference
    │   ├── Configuration
    │   ├── Public API
    │   ├── Event schema
    │   ├── Metrics
    │   └── CLI
    ├── Operations
    │   ├── Storage & retention
    │   ├── Troubleshooting
    │   ├── Security
    │   └── Upgrade guide
    ├── Contributing
    │   ├── Build & test
    │   ├── Architecture
    │   ├── RFC process
    │   └── CLA
    └── Project
        ├── Roadmap
        ├── Governance
        └── Changelog

---

## 4. 页面清单与要求

### 4.1 Getting Started

| 页面 | 必须包含 | 验收 |
| --- | --- | --- |
| 5-minute quickstart | 安装、sample、`runesvaner open`、截图/GIF | 干净机器 5 分钟完成 |
| Install | TFM、包列表、AOT 说明 | 版本矩阵链接 |
| First diagnosis | 一次请求的瀑布解读 | 读者能说出"慢在哪" |

### 4.2 Concepts

| 页面 | 必须包含 |
| --- | --- |
| Diagnostic session | 会话、请求、事件的关系 |
| Request & segments | 分段类型、estimated、overlap、unaccounted |
| Cost ledger | 价格表、缓存、推理、重试、未知 |
| Sampling | 头/尾采样、加权、偏差 |
| Privacy model | 内容级别、哈希、导出、保留 |

### 4.3 Guides

每个 Guide 必须：

1. 有可运行的最小示例。
2. 说明前置条件与版本。
3. 有"验证成功"的步骤。
4. 有"常见错误"。
5. 代码片段可编译（CI 检查）。

### 4.4 Reference

| 页面 | 来源 |
| --- | --- |
| Configuration | `RunesvanerOptions` + 默认值表 |
| Public API | XML 文档 + DocFX |
| Event schema | `06-observability-model.md` + JSON Schema |
| Metrics | 指标表 + 桶 |
| CLI | 命令、参数、退出码 |

### 4.5 Operations

| 页面 | 内容 |
| --- | --- |
| Storage & retention | 目录、权限、清理、磁盘满 |
| Troubleshooting | 症状 → 原因 → 解决 |
| Security | 威胁模型、漏洞披露、Studio 安全 |
| Upgrade guide | 版本差异、破坏性变更、迁移 |

---

## 5. 写作规范

### 5.1 原则

- **可执行**：每个步骤都能照做。
- **可验证**：每篇有"你应该看到什么"。
- **版本化**：标注适用版本；过时页面归档。
- **不夸张**：不写"极致性能""革命性"。
- **不 AI 腔**：避免"让我们深入探索""在当今快速发展的时代"。
- **语言策略（D-16）**：英文完整；简体中文与标准德语按 `18-localization-scope.md` 分级（L1 全译、L2 简版、L3 跳转）；UI 三语言完整。

### 5.2 代码片段

- 使用真实可编译代码，不用伪代码。
- 标注文件名与上下文。
- 依赖版本固定。
- 避免 `...` 省略关键逻辑。
- CI 用 `dotnet build` + snippet 提取验证。

### 5.3 截图与 GIF

- 使用真实数据（合成），不用 mock 假图。
- 深色主题，1280×800 或 1440×900。
- 标注版本与日期。
- GIF ≤ 5 MB，附文字步骤。

### 5.4 术语表

| 术语 | 定义 |
| --- | --- |
| Diagnostic Session | 一次采集会话 |
| Request | 一次逻辑 AI 操作 |
| Segment | 请求内的时间区间 |
| TTFT | 首个非空输出 Token 的时间 |
| TTFB | 首字节时间 |
| Unaccounted | 未被分段覆盖的时间 |
| Estimated | 由推导而非直接测量得到 |
| Price Table | 版本化价格表 |

术语表需中英对照，避免同一概念多种译法。

---

## 6. 文档质量指标

| 指标 | 目标 | 采集方式 |
| --- | --- | --- |
| 快速开始完成率 | ≥ 80% | 试用问卷 |
| 文档搜索失败率 | ≤ 10% | 站点分析（隐私友好） |
| "文档"标签 Issue 比例 | ≤ 5% | GitHub 标签 |
| 断链数 | 0 | CI link checker |
| 代码片段编译失败 | 0 | CI |
| 过时页面（> 2 个版本） | 0 | 季度审查 |
| L1 三语言同步率 | 100% | 发布检查 |

---

## 7. 社区渠道

| 渠道 | 用途 | 是否启用 |
| --- | --- | --- |
| GitHub Issues | Bug、功能请求 | 是 |
| GitHub Discussions | 问答、用例、RFC 讨论 | 是 |
| GitHub Security Advisories | 漏洞 | 是 |
| Discord / Slack | 即时交流 | 已决议不启用（D-22） |
| 邮件列表 | 公告 | 暂不 |
| X / Mastodon / LinkedIn | 发布公告 | 是（低频） |

原则：**渠道越少越好**，避免支持碎片化。所有问答尽量回流到 Discussions 以便搜索。

---

## 8. Issue 与 PR 运营

### 8.1 Issue 分级

| 标签 | 响应目标 |
| --- | --- |
| `bug` + `P0` | 48 小时 |
| `bug` + `P1` | 7 天 |
| `enhancement` | 14 天 |
| `question` | 尽力 |

### 8.2 PR 审查

- 首次贡献：48 小时内给首次反馈。
- 审查意见具体、可执行。
- 使用 `good first issue` 引导。
- 不要求贡献者做无关重构。
- 合并后致谢。

### 8.3 发布公告

| 渠道 | 内容 |
| --- | --- |
| GitHub Releases | 变更、迁移、贡献者 |
| Discussions | 讨论帖 |
| 博客 | 技术深度 |
| 社交 | 短公告 + 链接 |

---

## 9. 内容营销

| 内容 | 目标 | 频率 |
| --- | --- | --- |
| 技术文章："一次 RAG 请求的 8 秒去哪了" | 传播核心价值 | M2 起 |
| Demo 视频（3 分钟） | 降低理解成本 | M2 |
| 基准报告 | 建立可信度 | 每个里程碑 |
| 与 Langfuse/Phoenix 互操作指南 | 借势 | M3 |
| 会议演讲 | 社区 | M3–M4 |
| Awesome .NET / AI 列表 | 曝光 | M2 |

原则：**先给结论与数据，再讲产品**。不做标题党。

---

## 10. 支持策略

| 类型 | 承诺 |
| --- | --- |
| 社区支持 | 尽力，无 SLA |
| 安全漏洞 | 48 小时响应 |
| 商业支持 | 暂不提供 |
| 旧版本 | 仅安全修复，LTS + 最新 STS |
| 数据迁移 | 提供工具与文档 |

---

## 11. 贡献者体验

### 11.1 本地构建

目标：一条命令。

    git clone https://github.com/<owner>/runesvaner-dotnet.git
    cd runesvaner-dotnet
    dotnet build
    dotnet test

要求：

- `global.json` 固定 SDK。
- `Directory.Build.props` 统一分析器。
- 不需要 Docker（可选）。
- 不需要真实 API Key（mock provider）。
- devcontainer 可选。

### 11.2 第一个 PR

- `good first issue` 列表。
- 每个 issue 有文件定位、验收标准、mentor。
- PR 模板包含检查清单。
- CI 快速反馈（≤ 10 分钟）。

### 11.3 贡献者认可

- `CONTRIBUTORS.md` 或 Release 致谢。
- 重大贡献邀请为 Reviewer。
- 不设"贡献者等级"游戏化。

---

## 12. 文档与代码同步

| 机制 | 说明 |
| --- | --- |
| 同 PR 更新 | 行为/API 变更必须更新文档 |
| 链接检查 | CI 检查内部链接 |
| 代码片段编译 | 从 Markdown 提取并构建 |
| 版本标注 | 每页顶部标注适用版本 |
| 季度审查 | 过时内容归档 |
| 单一事实来源 | 数据模型以 `06` 为准，API 以 XML 为准 |

---

## 13. 反模式

- 文档与代码不同步。
- 快速开始需要 10 个步骤。
- 截图过期。
- 过度承诺性能。
- 用 AI 生成大段空话。
- 把设计文档当用户文档。
- 社区渠道过多，无人维护。
- 对 Issue 不回应。
- 发布无变更日志。

---

## 14. 里程碑对应

| 里程碑 | 文档交付 |
| --- | --- |
| M0 | 设计文档、治理文件、README |
| M1 | 快速开始、MEAI 指南、CLI 参考；L1 三语言；GitHub Pages |
| M2 | 成本、隐私、Studio、故障排查 |
| M3 | 文档站、SK/VectorData、AOT、迁移 |
| M4 | API 参考、升级指南、安全白皮书 |

---

## 15. 决议状态

D-16、D-21、D-22、D-10、D-23 已决议：

- D-16：英文完整；中文/德语按 `18-localization-scope.md` 分级；UI 三语言完整。
- D-21：Astro Starlight。
- D-22：GitHub Issues + Discussions。
- D-23：GitHub Pages；M1 不买域名。
- D-10：暂不提供商业支持。
