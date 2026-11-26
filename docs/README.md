# Runesvaner 设计文档索引

> 当前阶段：**M1 Walking Skeleton**。设计文档与最小可运行骨架并存。
> 仓库：https://github.com/karlmoriaty/runesvaner
> 项目名 `Runesvaner`、Apache-2.0、ICLA+CCLA 已于 2026-11-25 决议；D-01–D-25 全部决议，见 `15-decision-log.md`。

---

## 1. 阅读路径

### 1.1 发起人（约 30 分钟）

1. `00-project-charter.md`：为什么做、做什么、不做什么。
2. `01-market-research.md`：两个切口的证据与推荐。
3. `15-decision-log.md`：**D-01–D-25 决议记录**。
4. `11-roadmap.md`：里程碑与退出标准。

### 1.2 工程（约 2 小时）

1. `05-technical-design.md`：架构、线程、开销。
2. `06-observability-model.md`：数据模型与公式。
3. `07-api-design.md`：公共 API。
4. `08-native-aot-strategy.md`：AOT 约束。
5. `12-testing-and-benchmarks.md`：测试与基准。
6. `14-hidden-requirements.md`：容易漏的细节。

### 1.3 设计（约 1 小时）

1. `04-ux-and-visual-design.md`：设计语言、色彩、布局。
2. `02-problem-and-personas.md`：用户与场景。
3. `16-brand-naming.md`：命名与品牌。
4. `18-localization-scope.md`：英文全量、中文/德语核心简版的范围。
5. `19-open-implementation-decisions.md`：D-23–D-25 实施级决议记录。

### 1.4 贡献者（约 30 分钟）

1. `10-open-source-governance.md`：许可证、CLA、治理。
2. `17-docs-and-community.md`：构建、Issue、PR。
3. `11-roadmap.md`：当前里程碑与可参与任务。

---

## 2. 文档地图

| 文档 | 内容 | 状态 |
| --- | --- | --- |
| `00-project-charter.md` | 愿景、范围、成功指标、终止条件 | Draft |
| `01-market-research.md` | 市场、竞品、两个切口评分、来源 | Draft |
| `02-problem-and-personas.md` | 痛点、画像、JTBD、旅程、异议 | Draft |
| `03-prd.md` | 功能需求、验收标准、NFR | Draft |
| `04-ux-and-visual-design.md` | 设计语言、色彩、布局、组件、Logo | Draft |
| `05-technical-design.md` | 架构、模块、线程、开销、成本引擎 | Draft |
| `06-observability-model.md` | Span、分段、指标、成本、Schema | Draft |
| `07-api-design.md` | 公共 API、DI、选项、扩展点 | Draft / API 未冻结 |
| `08-native-aot-strategy.md` | AOT 约束、矩阵、CI、体积 | Draft |
| `09-security-privacy.md` | 威胁模型、隐私、Studio 安全 | Draft |
| `10-open-source-governance.md` | License、CLA、治理、发布 | Draft / 待法律审阅 |
| `11-roadmap.md` | M0–M4 与 v2 | Draft |
| `12-testing-and-benchmarks.md` | 测试金字塔、基准门槛 | Draft |
| `13-risk-register.md` | 24 项风险与缓解 | Draft |
| `14-hidden-requirements.md` | 60 条隐形需求 | Draft |
| `15-decision-log.md` | **D-01–D-25 决议记录** | **Accepted** |
| `16-brand-naming.md` | Runesvaner 命名、NuGet 初筛、商标待办 | Accepted |
| `17-docs-and-community.md` | 文档站、社区、发布、贡献者体验 | Draft |
| `18-localization-scope.md` | 本地化分级、术语表、翻译维护 | Accepted |
| `19-open-implementation-decisions.md` | D-23–D-25 决议记录 | **Accepted** |

---

## 3. 文档状态图例

| 状态 | 含义 |
| --- | --- |
| Draft | 可讨论，可能变更 |
| Review | 等待发起人/维护者审阅 |
| Accepted | 已决议，作为实现依据 |
| Superseded | 被新文档替代，保留历史 |
| Blocking | 历史状态；当前无 Blocking 项 |

---

## 4. 单一事实来源

| 主题 | 权威文档 |
| --- | --- |
| 产品范围与非目标 | `00-project-charter.md` |
| 功能与验收标准 | `03-prd.md` |
| 架构与模块 | `05-technical-design.md` |
| 数据模型与公式 | `06-observability-model.md` |
| 公共 API | `07-api-design.md` + XML 文档 |
| AOT 约束 | `08-native-aot-strategy.md` |
| 隐私与安全 | `09-security-privacy.md` |
| 治理与 CLA | `10-open-source-governance.md` |
| 路线图 | `11-roadmap.md` |
| 决策 | `15-decision-log.md` |

出现冲突时，以本表为准，并在冲突文档中标注"以 X 为准"。

---

## 5. 维护规则

1. 文档与代码同 PR 更新；纯文档 PR 需 1 名 Reviewer。
2. 每份文档顶部保留元信息（状态、版本、日期、受众、关联文档）。
3. 破坏性决策需在 `15-decision-log.md` 记录。
4. 过时文档标记 `Superseded`，不直接删除。
5. 每季度检查一次链接与过时内容。
6. 设计文档使用中文；代码、Issue、PR 以英文为主。
7. 不在文档中承诺未决议的事项为"已决定"。

---

## 6. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md` 与 `19-open-implementation-decisions.md`。关键结论：

1. D-01 主赛道：AI 负载诊断首发。
2. D-02 项目名：Runesvaner。
3. D-03 许可证：Apache-2.0。
4. D-04 CLA：ICLA + CCLA。
5. D-05 目标框架：`net10.0`。
6. D-09 默认内容捕获：`None`。
7. D-07 存储：库 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
8. D-23：GitHub Pages；M1 不买域名。
9. D-24：win-arm64 非目标；linux-arm64 可选。
10. D-25：商标暂缓；NuGet M0 占位；CLA 首次外部 PR 前审阅。

---

## 7. 下一步

决议已完成。等待发起人明确说"开工"后：

1. 建立 `Runesvaner.sln` 与项目骨架。
2. 添加 `LICENSE`、`CONTRIBUTING.md`、`CODE_OF_CONDUCT.md`、`SECURITY.md`、`CLA.md`。
3. 建立 CI（build + test + markdown lint）。
4. 开始 M1 Walking Skeleton。

在发起人明确说"开工"之前，不写业务代码。