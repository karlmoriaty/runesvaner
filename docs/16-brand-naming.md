# 命名决议与品牌方向：Runesvaner

| 字段 | 值 |
| --- | --- |
| 文档状态 | **已决议（Resolved）** |
| 版本 | 1.0 |
| 决议日期 | 2026-11-25 |
| 决议人 | 项目发起人 |
| 关联文档 | `04-ux-and-visual-design.md`、`15-decision-log.md` |

---

## 1. 最终名称

**Runesvaner**

| 项 | 值 |
| --- | --- |
| 产品名 | Runesvaner |
| 中文名 | 待发起人确认（暂不在文档中使用中文品牌名） |
| CLI | `runesvaner` |
| 包前缀 | `Runesvaner.*` |
| 命名空间 | `Runesvaner` |
| 配置节 | `Runesvaner` |
| 环境变量 | `RUNESVANER_*` |
| 数据目录 | `.runesvaner/` |
| Meter / ActivitySource | `Runesvaner.AI` |
| 类型前缀 | `Runesvaner*` |
| Studio | Runesvaner Studio |

> 名称由发起人选定（D-02）。名称含义/词源未在文档中臆测；如发起人希望补充品牌故事，可在本节追加。

---

## 2. 可用性初筛

检查日期：2026-11-25。

| 检查项 | 方法 | 结果 |
| --- | --- | --- |
| NuGet `runesvaner` | flatcontainer API | 404（当前无包） |
| NuGet `runesvaner.core` | flatcontainer API | 404 |
| NuGet `runesvaner.abstractions` | flatcontainer API | 404 |
| NuGet `runesvaner.ai` | flatcontainer API | 404 |
| NuGet `runesvaner.cli` | flatcontainer API | 404 |
| NuGet `runesvaner.studio` | flatcontainer API | 404 |
| NuGet `runesvaner.storage.sqlite` | flatcontainer API | 404 |
| NuGet `runesvaner.storage.jsonl` | flatcontainer API | 404 |
| NuGet `runesvaner.exporters.opentelemetry` | flatcontainer API | 404 |
| NuGet `runesvaner.microsoft.extensions.ai` | flatcontainer API | 404 |
| NuGet `runesvaner.semantic.kernel` | flatcontainer API | 404 |
| NuGet `runesvaner.vectordata` | flatcontainer API | 404 |
| NuGet `runesvaner.analyzers` | flatcontainer API | 404 |
| GitHub 用户 `runesvaner` | users API | 404（未发现占用） |
| GitHub 组织 `runesvaner` | orgs API | 404（未发现占用） |
| GitHub 仓库 `runesvaner/runesvaner` | repos API | 404（未发现占用） |
| GitHub 同名仓库搜索 | search API | 0 个 |
| 域名 `runesvaner.dev` / `.com` / `.io` / `.tools` / `getrunesvaner.dev` | DNS | 均无解析（不代表可注册） |
| 商标 | USPTO/EUIPO/CNIPA | **暂缓**（2026-11-26 用户决定；正式预览前再评估） |

结论：

- 当前公开包、GitHub 用户/组织/仓库、候选域名均未发现占用。
- 域名无 DNS 解析只说明当前未使用，不等于可注册；需 WHOIS 确认。
- 商标检索暂缓（无律师/预算）；正式公开预览或 1.0 前再评估。
- 工作名阶段可用；域名确认与商标检索在正式预览前再评估。

---

## 3. 名称评估

| 维度 | 评估 |
| --- | --- |
| 相关性 | 中性偏正面；"Rune" 暗示符号/解读，"svaner" 无直接含义，整体独特 |
| 可读性 | 10 个字符，3 个音节左右；对英语/德语用户可读，对中文用户需适应 |
| 独特性 | 高；GitHub 用户/组织/仓库与 13 个 NuGet ID 均未发现占用 |
| 商标风险 | 暂缓检索；生造词通常风险较低，正式预览前再评估 |
| CLI 友好 | `runesvaner` 全小写、无连字符、长度可接受 |
| 国际化 | 不含负面含义；需确认发音一致性 |
| 搜索友好 | 独特，易搜；但拼写需一次记对 |

### 3.1 风险

1. **拼写成本**：10 个字符，用户可能拼错（rune + svaner）。CLI 与文档需统一，必要时提供 `rsv` 短别名（待定）。
2. **发音不确定**：需发起人确认官方发音，文档不强行标注音标。
3. **商标暂缓**：正式公开预览或 1.0 前再评估类别 9/42；当前不阻塞工作名阶段。
4. **域名未确认**：当前无 DNS 解析，但需 WHOIS 确认可注册性；不影响 M0，影响官网。

---

## 4. 候选历史（未采用）

以下候选在 D-02 决议前评估，现保留作为历史记录，**不代表当前名称**。

| 名称 | NuGet | 含义 | 未采用原因 |
| --- | --- | --- | --- |
| Aperture | 可用（404） | 光圈；聚焦/分解 | 未采用；Apple 历史产品联想、通用词 |
| Umbra | 可用（404） | 本影 | 未采用；语义偏"遮蔽" |
| Quasar | 可用（404） | 类星体 | 未采用；搜索污染风险 |
| Diffract | 已占用 | 衍射 | 未采用；包 ID 冲突 |
| Fathom | 已占用 | 测深 | 未采用；商标冲突 |
| Parallax | 已占用 | 视差 | 未采用；商标冲突 |
| Refract | 已占用 | 折射 | 未采用；包 ID 冲突 |
| Prism | 已占用 | 棱镜 | 未采用；通用词、冲突多 |
| Candela | 已占用 | 光强单位 | 未采用；包 ID 冲突 |
| Vernier | 已占用 | 游标卡尺 | 未采用；教育公司商标 |
| Tomograph | 可用（未查） | 断层扫描 | 未采用；过长 |

> `Aperture` 在旧版文档中曾作为工作名；自 2026-11-25 起全部替换为 `Runesvaner`。

---

## 5. 命名规范

GitHub 仓库名：`runesvaner`；产品名、包前缀与文档站仍为 `Runesvaner`。

### 5.1 包命名

    Runesvaner
    Runesvaner.Abstractions
    Runesvaner.MicrosoftExtensionsAI
    Runesvaner.SemanticKernel
    Runesvaner.VectorData
    Runesvaner.Storage.Sqlite
    Runesvaner.Storage.Jsonl
    Runesvaner.Exporters.OpenTelemetry
    Runesvaner.Cli
    Runesvaner.Studio
    Runesvaner.Analyzers        # v2

### 5.2 代码命名

| 元素 | 规则 | 示例 |
| --- | --- | --- |
| 选项 | `RunesvanerOptions` | `RunesvanerOptions.Storage` |
| 接口 | `IRunesvaner*` | `IRunesvanerRecorder` |
| 扩展方法 | `AddRunesvaner` / `UseRunesvaner` | `services.AddRunesvaner()` |
| 标签 | `RunesvanerTags` | `RunesvanerTags.Feature` |
| 事件 | `RunesvanerEvent` | `RunesvanerEvent.RequestEnd` |
| Meter | `Runesvaner.AI` | `runesvaner.ai.ttft` |
| ActivitySource | `Runesvaner.AI` | — |

### 5.3 文件与目录

    .runesvaner/
      sessions/
      runesvaner.db
      pricing/
      config.json
      studio.token

### 5.4 环境变量

    RUNESVANER_STORAGE_PATH
    RUNESVANER_STORAGE_MODE
    RUNESVANER_CONTENT_CAPTURE
    RUNESVANER_SAMPLING_HEAD_PROBABILITY
    RUNESVANER_OTLP_ENDPOINT

---

## 6. 品牌方向

| 维度 | 方向 |
| --- | --- |
| 气质 | 精密、克制、工程仪表；不是消费级 AI 产品 |
| 色彩 | 石墨黑 + 琥珀铜；浅色为暖灰纸感；禁用紫绿霓虹 |
| 形状 | 直角优先，最大 2px 圆角；禁用胶囊/大圆角 |
| 图标 | 线性、1.5px 描边；避免 AI 光效 |
| Logo 概念 | "符文/刻度 + 观测"的抽象组合；不使用相机光圈具象；具体草案待设计 |
| 文案 | 英文为主；中文/德语简版；不使用夸张形容词 |

> Logo、字体、图标资产在 M1 设计阶段产出；本文档不生成图片。

---

## 7. 改名成本与冻结

- 自 2026-11-25 起，文档、包名、命名空间、CLI 已统一为 `Runesvaner`。
- 公开预览（M2）后不再改名；如后期商标检索失败，需在正式预览前决定替代名。
- 替代名优先级：`Umbra`、`Quasar`（NuGet 可用）；最终由发起人决定。

---

## 8. 下一步

1. 发起人确认名称含义/发音（可选）。
2. 商标检索暂缓；正式预览前再评估。
3. WHOIS 确认域名（当前候选均无 DNS 解析）。
4. 已完成：GitHub 仓库 `karlmoriaty/runesvaner`（2026-11-26）。
5. M0 注册 NuGet 根包占位（13 个计划 ID 当前 404）。
6. M1 产出 Logo 三版草案。

---

## 9. 关联决议

- D-02：名称 Runesvaner。
- D-11：商标由个人持有；36 个月不活跃可转让。
- D-12：浅色主题设计约束。
- D-16：多语言策略。