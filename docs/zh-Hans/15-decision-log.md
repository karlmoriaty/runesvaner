# 决策文档：D-01–D-25 决议记录

> **中文版（Legacy full version）** — 英文主文档见 [`../15-decision-log.md`](../15-decision-log.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | **已决议（Resolved）** |
| 版本 | 1.3 |
| 决议日期 | 2026-11-25 |
| 决议人 | 项目发起人 |
| 记录人 | CodeArts 设计代理 |
| 关联文档 | 全部设计文档 |

> 本文档记录发起人对 D-01–D-25 的最终决议。所有条目均已从"待决议"转为"已决议"；实施时以本文件为准。
> 仍需外部完成的事项（商标检索、CLA 律师审阅、NuGet 占位、仓库名/域名）单列在第 4 节，不属于产品决策。

---

## 1. 决议摘要

| ID | 主题 | 最终决议 | 状态 |
| --- | --- | --- | --- |
| D-01 | 主赛道 | 首发 AI 负载诊断；Native AOT 作为一等约束与 v2 实验分支 | 已决议 |
| D-02 | 项目名 | **Runesvaner**；接受工作名阶段暂不注册商标 | 已决议 |
| D-03 | 许可证 | Apache-2.0 | 已决议 |
| D-04 | CLA | ICLA + CCLA；自动化工具作为实施建议 | 已决议 |
| D-05 | 目标框架 | `net10.0` 为主；M3 评估 `net11.0` | 已决议 |
| D-06 | 前端栈 | React 19 + TS + Vite + Redux Toolkit + RTK Query；CSS Modules + design tokens；Radix Primitives；uPlot + 自绘瀑布 | 已决议 |
| D-07 | 默认存储 | 库/应用内 **SQLite 默认**；CLI AOT 默认 JSONL；失败回退 | 已决议（2026-11-26 修订） |
| D-08 | 价格表 | 内置快照（版本/来源/校验和）+ 用户覆盖；远端默认关闭；decimal + ToEven；USD 存储 | 已决议 |
| D-09 | 内容捕获 | 默认 `None` | 已决议 |
| D-10 | 商业化 | 暂不商业化；接受无捆绑赞助；不做托管服务 | 已决议 |
| D-11 | 商标与继任 | 个人持有；连续 **36 个月**不活跃可经 RFC 推举继任并转让商标/域名 | 已决议 |
| D-12 | 浅色主题 | 同意；M2 深色优先，1.0 提供浅色；必须避免"AI 味" | 已决议 |
| D-13 | OTel 策略 | 核心零依赖；`Runesvaner.Exporters.OpenTelemetry` 可选；默认不创建额外 Activity | 已决议 |
| D-14 | 采样 | 本地 100%；生产文档建议 10%；尾采样 M3 引入，默认关闭 | 已决议 |
| D-15 | 平台 | Windows 10/11 + Linux；**macOS 非目标** | 已决议 |
| D-16 | 语言 | UI 默认英文，可切换简体中文与标准德语（`de-DE`）；文档英文为主，中文/德语简版 | 已决议 |
| D-17 | 测试与版本支持 | 接受 xUnit v3 + MEAI 10.x/11.x 契约测试；LTS + 最新 STS | 已决议 |
| D-18 | AOT 堆分析 | 不作为主线；保留研究分支与 issue，欢迎社区维护者 | 已决议 |
| D-19 | Roslyn AOT 分析器 | v2 投入；可开 issue 给愿意维护的开发者 | 已决议 |
| D-20 | 第三方语义 | 无必须兼容平台；以 OTel GenAI 为主，其他按社区需求 | 已决议 |
| D-21 | 文档站 | Astro Starlight | 已决议 |
| D-22 | 社区渠道 | GitHub Issues + Discussions | 已决议 |
| D-23 | 文档站托管 | GitHub Pages；M1 不买域名 | 已决议 |
| D-24 | Windows ARM64 | v1 不支持 win-arm64 | 已决议 |
| D-25 | 发布准备 | 商标暂缓；NuGet M0；CLA 首次外部 PR 前 | 已决议 |

---

## 2. 逐项记录

### D-01 主赛道

**决议**：首发 AI 负载诊断工具；完成后再补 Native AOT 兼容诊断器。AOT 作为一等约束与 v2 实验分支。

**理由**：

- Native AOT 自 .NET 8 起支持 EventPipe，传统工具并非完全失效；真正空白是托管堆分析，技术风险与个人项目不匹配。
- AI 负载诊断有可验证的用户价值，且 .NET 侧无主导者。
- AOT 兼容是差异化护城河，适合作为约束而非首发主战场。

**影响**：产品定位、PRD、技术设计、AOT 策略均按"AI 优先、AOT 约束"编写。

**后续**：v2 启动条件见 `11-roadmap.md`；AOT 研究 issue 见 D-18。

---

### D-02 项目名

**决议**：项目名为 **Runesvaner**；GitHub 仓库名为 **`runesvaner`**。接受工作名阶段暂不注册商标。

**可用性核验（2026-11-25 / 2026-11-26）**：

| 检查项 | 结果 |
| --- | --- |
| NuGet 13 个计划包 ID（root/core/abstractions/ai/cli/studio/storage.sqlite/storage.jsonl/exporters.opentelemetry/microsoft.extensions.ai/semantic.kernel/vectordata/analyzers） | 全部 404（当前无包） |
| GitHub 用户/组织/仓库 `runesvaner` | 404（未发现占用） |
| GitHub 同名仓库搜索 | 0 个 |
| 域名 `runesvaner.dev` / `.com` / `.io` / `.tools` / `getrunesvaner.dev` | 均无 DNS 解析（不代表可注册） |
| 商标检索 | **未执行，需律师** |

**命名规范**：

| 用途 | 值 |
| --- | --- |
| 产品名 | `Runesvaner` |
| 包前缀 | `Runesvaner.*` |
| 命名空间 | `Runesvaner` |
| CLI | `runesvaner` |
| 配置节 | `Runesvaner` |
| 环境变量前缀 | `RUNESVANER_` |
| 数据目录 | `.runesvaner/` |
| Meter / ActivitySource | `Runesvaner.AI` |
| 类型前缀 | `Runesvaner*`（如 `RunesvanerOptions`） |
| Studio | `Runesvaner Studio` |
| GitHub 仓库 | `runesvaner`（产品/包/文档站仍为 `Runesvaner`） |

**风险**：名称为生造词，独特但需确认发音；外部可用性核验通过（NuGet/GitHub/域名未发现占用），但商标与域名注册仍需律师/WHOIS 确认。工作名阶段可先用，正式预览前完成检索。

**影响**：所有文档、包名、命名空间、CLI 已统一改名；完整核验记录见 `16-brand-naming.md`。

---

### D-03 许可证

**决议**：Apache-2.0。

**理由**：明确专利授权、商业友好、与 OpenTelemetry 生态一致；比 Langfuse（MIT+EE）与 Phoenix（ELv2）更开放。

**后续**：M0 添加 `LICENSE` 与 `NOTICE`；发布前律师审阅。

---

### D-04 CLA

**决议**：ICLA + CCLA；使用 CLA Assistant（或等价 GitHub Action）自动化。

**理由**：权利清晰、支持再许可、便于维护者梯队；自动化降低摩擦。

**后续**：起草 ICLA/CCLA，律师审阅；配置机器人；在 `CONTRIBUTING.md` 说明。

---

### D-05 目标框架

**决议**：`net10.0` 为主；M3 评估 `net11.0`。

**理由**：.NET 8 已于 2026-11 结束支持；.NET 10 是 LTS；避免多目标拖垮个人项目 CI。

**影响**：所有库项目 TFM 为 `net10.0`；MEAI 以 10.x 为主，11.x 契约测试。

---

### D-06 前端栈

**决议**：

- React 19 + TypeScript + Vite
- Redux Toolkit + RTK Query
- CSS Modules + design tokens
- Radix Primitives（无样式、可访问）
- uPlot + 自绘瀑布
- 不使用 Tailwind、不使用 shadcn 默认外观

**理由**：多面板联动适合集中状态；RTK Query 简化本地 API；CSS Modules 保持设计控制力，避免"AI 味"。

**后续**：M2 建立组件库与设计 token；M3 Playwright E2E。

---

### D-07 默认存储

**决议（2026-11-26 修订）**：

- **库/应用内采集**：SQLite 默认；AOT 初始化失败自动回退 JSONL。
- **CLI**：Native AOT 发布，默认 JSONL；提供 `--storage sqlite` 显式选择；失败回退。
- **M2 评估**：框架依赖 CLI（默认 SQLite）作为补充发布，不替代 AOT CLI。

**依据（验证结果）**：

- `dotnet/efcore#36068` "Microsoft.Data.Sqlite and NativeAOT compatibility" 仍为 open。
- Issue 明确：Native AOT 控制台引用 `Microsoft.Data.Sqlite` 时 `e_sqlite3` 可能找不到；项目未启用 `<IsAotCompatible>`；缺少 AOT CI。
- 因此不能把 "CLI AOT + SQLite 默认" 作为开箱即用的承诺。

**理由**：

- 保留 D-07 核心：库/应用内 SQLite 默认，满足查询、聚合与 Studio。
- CLI 作为 AOT 工具，优先保证开箱可用；JSONL 是零 native 依赖的回退。
- 双默认按宿主区分，不牺牲 AOT 一等约束，也不放弃 SQLite 主存储。

**实现约束**：

1. 库默认 `StorageMode.Sqlite`，提供 `IRunesvanerQueryStore`。
2. CLI 默认 `StorageMode.Jsonl`；`--storage sqlite` 时尝试初始化 SQLite。
3. 任何宿主下 SQLite 初始化失败，自动回退 JSONL，记录 `storage.fallback=jsonl` 与原因，不阻断业务。
4. M1 必须完成 SQLite AOT 实测：native 资产、`SQLitePCLRaw` 初始化、WAL、查询。
5. 若实测发现可行 workaround，M2 可把 CLI 默认切回 SQLite；若不可行，保留 CLI JSONL 默认，并在 M2 提供框架依赖 CLI（SQLite 默认）。

**影响**：

- `05-technical-design.md`：存储默认按宿主区分。
- `07-api-design.md`：库默认 `StorageMode.Sqlite`；CLI 覆盖为 `Jsonl`。
- `08-native-aot-strategy.md`：SQLite AOT 风险与回退、CLI 默认 JSONL。
- `03-prd.md`：FR-040/041 描述按宿主区分。
- `11-roadmap.md`：M1 验证 SQLite AOT；M2 评估框架依赖 CLI。
- `12-testing-and-benchmarks.md`：库 SQLite + CLI JSONL + 回退测试。
- `13-risk-register.md`：R-06 更新。
- `14-hidden-requirements.md`：H-36 更新。

---


### D-08 价格表

**决议**：

- 内置快照，带版本、来源、校验和、获取时间。
- 用户覆盖优先。
- 远端更新默认关闭，显式开启。
- `decimal` 计算；`MidpointRounding.ToEven`；存储 6 位小数，展示 4 位。
- USD 存储；展示可换算，默认离线。

**理由**：可追溯、可回放、不猜测、不静默联网。

**后续**：M2 价格表 schema 与 CI 校验；价格表社区维护流程见 `14-hidden-requirements.md` H-59。

---

### D-09 内容捕获

**决议**：默认 `ContentCapture.None`。

**理由**：隐私底线；延迟/成本诊断不需要内容；`Full` 仅本地显式开启，带警告与保留限制。

**后续**：M1 实现默认值；M2 完成隐私测试与 UI 警告。

---

### D-10 商业化

**决议**：暂不商业化；接受无捆绑赞助；不做托管服务。

**理由**：本地优先是核心定位；个人项目带宽有限；赞助可覆盖基础设施与设计成本。

**后续**：`10-open-source-governance.md` 资助章节；官网不出现付费墙。

---

### D-11 商标与继任

**决议**：项目名/Logo 由发起人个人持有；连续 **36 个月**不活跃，可由维护者 RFC 推举继任并转让商标/域名。

**理由**：保护品牌不被滥用；同时避免 bus factor = 1。

**后续**：`10-open-source-governance.md` 继任条款改为 36 个月；律师审阅转让机制。

---

### D-12 浅色主题

**决议**：同意提供浅色主题；M2 深色优先，1.0 提供浅色。设计要求：暖灰纸感、非纯白、直角、低饱和、无紫绿霓虹、无 AI 光效。

**理由**：用户明确接受，但强调不要"AI 味"；浅色需单独设计，不能简单反转深色。

**后续**：`04-ux-and-visual-design.md` 补充浅色主题约束与验收。

---

### D-13 OTel 策略

**决议**：核心零依赖；`Runesvaner.Exporters.OpenTelemetry` 为可选包；默认不创建额外 Activity。

**理由**：MEAI 已内置 `UseOpenTelemetry`，重复会产生双 Span；AOT 用户不希望被 OTel 体积绑架。

**后续**：M2 实现 OTLP 导出；AOT 矩阵单独验证；文档说明 Span 嵌套关系。

---

### D-14 采样

**决议**：本地 100%；生产文档建议 10%；尾采样 M3 引入，默认关闭。

**理由**：本地诊断需要完整数据；生产需要低开销；尾采样有内存成本。

**后续**：M1 头采样；M3 尾采样 + 加权统计。

---

### D-15 平台

**决议**：目标平台为 **Windows 10/11** 与 **Linux**；**macOS 非目标**，v1 不承诺。

**理由**：目标用户主要是 Windows 10/11 与 Linux 专业开发者；减少 CI 与测试矩阵，符合个人项目带宽。

**影响**：

- CI 矩阵移除 `macos-latest`。
- AOT 矩阵：`win-x64`、`linux-x64`、`linux-arm64`（可选）。
- 文档不再声称 macOS 支持。
- 若未来社区有需求，可作为 v2 社区支持项。

---

### D-16 语言

**决议**：

- UI 默认英文（`en-US`），可切换简体中文（`zh-Hans`）与标准德语（`de-DE`）。
- 文档英文为主；中文与德语可做简版或跳转界面。

**理由**：目标用户包含国际 .NET 开发者；德语为发起人指定的第三语言；文档全量翻译成本高，采用"英文主 + 简版"策略。

**影响**：

- `04-ux-and-visual-design.md` 语言列表更新。
- `17-docs-and-community.md` 多语言策略更新。
- `14-hidden-requirements.md` H-60 更新。
- 文档站需要 i18n 路由与语言切换。

---

### D-17 测试与版本支持

**决议**：接受推荐方案：

- xUnit v3 + 内置断言（减少依赖）。
- UI E2E 使用 Playwright（M3）。
- MEAI 10.x/11.x 契约测试。
- 支持策略：LTS + 最新 STS，EOL 明确。

**后续**：M1 建立测试项目；M3 建立契约测试矩阵。

---

### D-18 AOT 堆分析

**决议**：不作为主线；保留研究分支与 issue，欢迎愿意维护的开发者。

**理由**：托管堆分析需要 runtime 级工程，风险与个人项目不匹配；但社区可能有兴趣。

**后续**：

- 建立 `research/aot-runtime-diagnostics` 分支（不阻塞主线）。
- 开 issue 说明目标、难点、验收标准，标记 `help wanted`。
- 不在 v1/v2 承诺交付。

---

### D-19 Roslyn AOT 分析器

**决议**：v2 投入；可开 issue 给愿意维护的开发者。

**理由**：与发起人 Roslyn 技术偏好匹配；可检测 AOT 不兼容的诊断代码；与主产品互补。

**后续**：

- v2 规划 `Runesvaner.Analyzers`。
- 开 issue 征集维护者与规则清单。
- 不进入 M1–M4 关键路径。

---

### D-20 第三方语义

**决议**：无必须兼容的第三方平台；以 OTel GenAI semconv 为主，其他适配按社区需求。

**理由**：减少兼容矩阵与承诺；OTel 是行业标准，足够覆盖主要后端。

**影响**：

- Langfuse / Phoenix 兼容从 Should 降为 Could。
- OpenInference 等作为 v2 可选适配。
- 文档不声称"保证兼容所有平台"。

---

### D-21 文档站

**决议**：Astro Starlight。

**理由**：Markdown 优先、构建轻、可定制、与 React UI 解耦。

**后续**：M1 建立文档站骨架；M3 完成导航与搜索。

---

### D-22 社区渠道

**决议**：GitHub Issues + Discussions；暂不建 Discord/Slack。

**理由**：渠道越少越好，避免支持碎片化；问答回流到 Discussions 便于搜索。

**后续**：M0 开启 Discussions 分类；M2 建立 Issue 模板与标签。

---

### D-23 文档站托管与域名

**决议**：GitHub Pages；M1 暂不购买域名；默认 URL `https://karlmoriaty.github.io/runesvaner/`。

**后续**：M1 配置 GitHub Actions → Pages；1.0 后再评估自定义域名。详见 `19-open-implementation-decisions.md`。

---

### D-24 Windows ARM64

**决议**：v1 不支持 `win-arm64`；保证 `win-x64` + `linux-x64`，`linux-arm64` 可选。

**后续**：RID/CI 矩阵不添加 `win-arm64`；有明确需求再作为 v2 社区项。

---

### D-25 发布准备时机

**决议**：商标检索暂缓（无律师/预算，后期再提）；NuGet 根包 M0 占位；CLA 首次外部 PR 前审阅。

**后续**：M0 只做 NuGet 占位；正式公开预览或 1.0 前再评估商标；CLA 审阅不阻塞个人开发。

---

## 3. 决议对文档的影响

| 文档 | 修改点 |
| --- | --- |
| `README.md` | 项目名、状态、支持矩阵、存储、平台、路线图 |
| `00-project-charter.md` | 决议状态、v1 范围、AOT 研究分支、终止条件 |
| `01-market-research.md` | 结论改为"已决议"；堆分析改为"保留研究" |
| `02-problem-and-personas.md` | 决议引用 |
| `03-prd.md` | SQLite 按宿主默认、平台、语言、第三方兼容优先级 |
| `04-ux-and-visual-design.md` | 浅色主题约束、三语言 |
| `05-technical-design.md` | 库 SQLite 主存储、CLI JSONL 默认、AOT 交互 |
| `06-observability-model.md` | 存储一致性说明 |
| `07-api-design.md` | 库默认 `StorageMode.Sqlite`；CLI 覆盖 Jsonl |
| `08-native-aot-strategy.md` | SQLite AOT 验证与回退、CLI JSONL 默认、移除 macOS |
| `09-security-privacy.md` | SQLite 文件权限、平台范围 |
| `10-open-source-governance.md` | 36 个月继任、许可证/CLA 确认、语言策略 |
| `11-roadmap.md` | M0 决议完成、双平台、v2 研究 |
| `12-testing-and-benchmarks.md` | SQLite 优先、移除 macOS |
| `13-risk-register.md` | SQLite/AOT 风险、CLA/商标状态 |
| `14-hidden-requirements.md` | H-36 提升、平台/语言更新 |
| `15-decision-log.md` | 本文件 |
| `16-brand-naming.md` | Runesvaner 已选、Aperture 未采用 |
| `17-docs-and-community.md` | 文档站/语言/渠道确认 |
| `18-localization-scope.md` | 英文全量、中文/德语核心简版、术语表 |
| `19-open-implementation-decisions.md` | D-23–D-25 决议记录 |
| `docs/README.md` | 状态与阅读路径 |

---

## 4. 仍需外部完成的事项

这些不是产品决策，但会影响发布：

| 事项 | 负责 | 时机 |
| --- | --- | --- |
| `Runesvaner` 商标检索（USPTO/EUIPO/CNIPA，类别 9/42） | 发起人 | **暂缓**（后期需要时再提） |
| ICLA/CCLA 律师审阅 | 发起人 | 首次外部 PR 前 |
| NuGet 根包占位（`Runesvaner` 0.0.1）；需用户本人注册 NuGet 账号；13 个计划包 ID 已验证 404 | 发起人 | M0 |
| GitHub 仓库 `runesvaner` 已验证 404；待创建（用户已确认仓库名） | 发起人 | 开工时 |
| 域名 WHOIS 确认（候选均无 DNS 解析） | 发起人 | 暂不购买域名（D-23）；1.0 后评估 |
| 价格表来源合法性审阅 | 发起人 + 律师 | M2 前 |
| Windows/Linux AOT 工具链实测 | 工程 | M0/M1 |
| SQLite AOT 实测与 ADR | 工程 | M1 |

---

## 5. 决议后的下一步

在发起人明确说"开工"之前，仍只做文档：

1. 审阅本决议与修改后的文档。
2. 完成第 4 节的外部事项（可与文档审阅并行）。
3. 说"开工"后，建立 `Runesvaner.sln`、项目骨架、治理文件与 CI。

---

## 6. 变更记录

| 日期 | 版本 | 变更 |
| --- | --- | --- |
| 2026-11-25 | 0.1 | 初始 D-01–D-22 待决议 |
| 2026-11-25 | 1.0 | 发起人完成全部决议；项目名改为 Runesvaner；D-07 改为 SQLite 默认；D-11 改为 36 个月；D-15 移除 macOS；D-16 增加德语 |
| 2026-11-26 | 1.1 | 完成 Runesvaner 外部可用性核验；依据 `dotnet/efcore#36068` 将 D-07 细化为库 SQLite / CLI AOT JSONL；新增 `18-localization-scope.md` |
| 2026-11-26 | 1.2 | 确认 GitHub 仓库名为 `runesvaner`；新增 `19-open-implementation-decisions.md`（D-23–D-25） |
| 2026-11-26 | 1.3 | D-23–D-25 全部决议；商标检索暂缓；Git 邮箱确认已绑定 |
