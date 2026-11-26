# 本地化范围与翻译策略：Runesvaner

> **中文版（Legacy full version）** — 英文主文档见 [`../18-localization-scope.md`](../18-localization-scope.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Accepted（D-16 实施文档） |
| 版本 | 1.0 |
| 日期 | 2026-11-26 |
| 受众 | 文档、本地化、工程、社区 |
| 关联文档 | `04-ux-and-visual-design.md`、`14-hidden-requirements.md`、`15-decision-log.md`、`17-docs-and-community.md` |

---

## 1. 原则

1. **英文是唯一 source of truth**：英文文档必须完整，覆盖每一处细节、边界与示例。
2. **中文/德语覆盖核心**：只翻译"重要核心"，可删减非必要细节；不承诺逐字对应。
3. **UI 三语言完整**：界面字符串量小，必须完整翻译，不能出现半中半英或半德半英。
4. **关键内容不可删减**：安全、隐私、许可证、破坏性变更、AOT 限制、成本免责声明必须在三种语言中完整同步。
5. **翻译不改变语义**：命令、默认值、数值、字段名、错误码必须与英文一致。
6. **人工审阅**：机器翻译只能作为初稿；德语必须由母语者审阅，中文需技术审阅。

---

## 2. 语言代码

| 语言 | 代码 | 默认 | 说明 |
| --- | --- | --- | --- |
| 英语 | `en-US` | 是 | 完整文档与 UI |
| 简体中文 | `zh-Hans` | 否 | 核心文档简版 + 完整 UI |
| 标准德语 | `de-DE` | 否 | 核心文档简版 + 完整 UI |

不提供其他语言；社区如需，可通过 RFC 增加，但必须承诺维护。

---

## 3. 文档分级

### 3.1 L1：全文翻译（核心必读）

这些页面必须提供中文/德语完整版本（允许语言风格调整，不允许删步骤）：

| 分类 | 页面 |
| --- | --- |
| 入口 | `README.md` |
| Getting Started | Install、5-minute quickstart、First diagnosis |
| Concepts | Diagnostic session、Request & segments、Cost ledger、Sampling、Privacy model |
| Guides | Microsoft.Extensions.AI 接入、Cost configuration、Native AOT、Troubleshooting |
| Operations | Storage & retention、Security essentials |
| Contributing | Build & test、CLA |
| Reference | Configuration（顶层选项）、CLI（常用命令） |

L1 的标准：

- 所有命令可复制执行。
- 所有默认值、字段名、错误码与英文一致。
- 有"你应该看到什么"的验证步骤。
- 有英文原文链接，便于对照。

### 3.2 L2：核心简版（保留结论与操作，删减解释）

这些页面提供中文/德语的**简版**，保留：

- 结论与推荐。
- 操作步骤与命令。
- 关键默认值与限制。
- 常见错误与下一步。
- 英文全文链接。

允许删减：

- 原理推导、设计权衡。
- 边界条件的长篇解释。
- 历史背景与竞品对比。
- 多个示例变体（保留一个最小示例）。
- 完整的属性/指标表（保留最常用项）。

适用页面：

| 分类 | 页面 |
| --- | --- |
| Reference | Public API 摘要、Event schema 摘要、Metrics 摘要 |
| Project | Roadmap 摘要、Governance 摘要 |
| Guides | 高级采样/预算配置、VectorData/SK 集成细节 |
| Operations | 故障排查的长尾案例 |

### 3.3 L3：仅英文（提供跳转）

这些页面不翻译，中文/德语页面显示：

- 一句说明："此页仅提供英文完整版，因为包含大量 API/架构细节。"
- 英文原文链接。
- 如涉及安全/破坏性变更，必须在 L1/L2 中有摘要与警告。

适用页面：

| 分类 | 页面 |
| --- | --- |
| Reference | 完整 API 参考（DocFX 生成）、完整指标/属性表、完整事件 schema |
| Architecture | 内部架构、线程模型、开销预算、AOT 矩阵细节 |
| Process | 全部 ADR/RFC、完整变更日志、发布检查清单 |
| Performance | 完整基准方法、数据集、原始结果 |
| Examples | 所有示例变体、边缘场景 |

---

## 4. 不可删减内容

以下内容即使属于 L2/L3，也必须在中文/德语中有完整摘要或全文：

| 内容 | 要求 |
| --- | --- |
| 安全警告 | 全文翻译；不得省略条件与后果 |
| 隐私默认值 | 明确 `None`、哈希、导出脱敏 |
| 密钥/Header 处理 | 明确永不采集的字段 |
| 许可证与 CLA | 要点翻译；正式文本以英文/法律文本为准 |
| 破坏性变更 | 迁移步骤、影响范围、回滚方式 |
| AOT/Trim 限制 | 明确 SQLite/OTel 的 AOT 状态与回退 |
| 成本免责声明 | 明确估算、未知模型、价格表版本 |
| 数据保留/删除 | 明确默认保留期与删除方式 |

---

## 5. 页面级范围（初始）

| 页面 | 英文 | 中文 | 德语 | 级别 |
| --- | --- | --- | --- | --- |
| README | 完整 | 全文 | 全文 | L1 |
| Install | 完整 | 全文 | 全文 | L1 |
| 5-minute quickstart | 完整 | 全文 | 全文 | L1 |
| First diagnosis | 完整 | 全文 | 全文 | L1 |
| Diagnostic session | 完整 | 全文 | 全文 | L1 |
| Request & segments | 完整 | 全文 | 全文 | L1 |
| Cost ledger | 完整 | 全文 | 全文 | L1 |
| Sampling | 完整 | 全文 | 全文 | L1 |
| Privacy model | 完整 | 全文 | 全文 | L1 |
| MEAI integration | 完整 | 全文 | 全文 | L1 |
| Cost configuration | 完整 | 全文 | 全文 | L1 |
| Native AOT | 完整 | 全文 | 全文 | L1 |
| Troubleshooting | 完整 | 全文 | 全文 | L1 |
| Storage & retention | 完整 | 全文 | 全文 | L1 |
| Security essentials | 完整 | 全文 | 全文 | L1 |
| Build & test | 完整 | 全文 | 全文 | L1 |
| CLA | 完整 | 全文 | 全文 | L1 |
| Configuration（顶层） | 完整 | 全文 | 全文 | L1 |
| CLI（常用命令） | 完整 | 全文 | 全文 | L1 |
| Public API 摘要 | 完整 | 简版 | 简版 | L2 |
| Event schema 摘要 | 完整 | 简版 | 简版 | L2 |
| Metrics 摘要 | 完整 | 简版 | 简版 | L2 |
| Roadmap 摘要 | 完整 | 简版 | 简版 | L2 |
| Governance 摘要 | 完整 | 简版 | 简版 | L2 |
| 高级采样/预算 | 完整 | 简版 | 简版 | L2 |
| SK / VectorData 细节 | 完整 | 简版 | 简版 | L2 |
| 完整 API 参考 | 完整 | 跳转 | 跳转 | L3 |
| 内部架构/线程模型 | 完整 | 跳转 | 跳转 | L3 |
| ADR / RFC | 完整 | 跳转 | 跳转 | L3 |
| 变更日志 | 完整 | 跳转 | 跳转 | L3 |
| 完整基准 | 完整 | 跳转 | 跳转 | L3 |
| 示例变体 | 完整 | 跳转 | 跳转 | L3 |

> 页面名称在 M1 文档站建立后可能微调；级别不变。

---

## 6. 翻译维护

| 规则 | 说明 |
| --- | --- |
| 英文先行 | 英文 PR 合并后才翻译；不翻译未合并内容 |
| L1 同步 | 英文变更后 7 天内更新中文/德语 |
| L2 同步 | 随下一个次版本更新 |
| L3 | 不翻译；保留跳转与安全摘要 |
| 版本标注 | 每页顶部标注 `Based on English vX.Y` |
| 过期提示 | 英文版本更新而译文未更新时，显示"译文可能过期" |
| 术语一致 | 使用第 7 节术语表；新术语需先补充术语表 |
| 机器翻译 | 仅作初稿；必须人工审阅 |
| 法律文本 | 许可证/CLA 以英文法律文本为准；译文仅供参考 |

---

## 7. 术语表

| 英文 | 简体中文 | 标准德语 | 备注 |
| --- | --- | --- | --- |
| Diagnostic Session | 诊断会话 | Diagnosesitzung | 不译作"会话文件" |
| Request | 请求 | Anfrage | 一次逻辑 AI 操作 |
| Segment | 分段 | Segment | 不译作"阶段" |
| Latency waterfall | 延迟瀑布 | Latenz-Wasserfall | UI 术语 |
| Cost ledger | 成本账本 | Kostenbuch | 不译作"账单" |
| Price table | 价格表 | Preistabelle | 带版本 |
| Estimated | 估算 | geschätzt | 与 unknown 区分 |
| Unaccounted | 未解释 | nicht zugeordnet | 残差时间 |
| Sampling | 采样 | Stichproben | 头/尾采样 |
| Trace | 追踪 | Trace | 保留英文，避免歧义 |
| Token | Token | Token | 不翻译 |
| Provider | 提供商 | Anbieter | 上下文明确时保留 Provider |
| Tenant | 租户 | Mandant | 哈希后存储 |
| Feature | 功能 | Funktion | 归因维度 |

> 德语术语表需母语者审阅；中文术语需技术审阅。审阅前不得宣称"最终译法"。

---

## 8. 验收标准

- **AC-L1**：L1 页面三语言均可导航，命令可复制执行，默认值与英文一致。
- **AC-L2**：L2 页面顶部有"英文全文"链接，且包含关键默认值/限制/常见错误。
- **AC-L3**：L3 页面有语言说明与英文跳转；涉及安全/破坏性变更时，L1/L2 有摘要。
- **AC-L4**：安全、隐私、许可证、AOT 限制、成本免责声明在三种语言中 100% 同步。
- **AC-L5**：术语表覆盖所有 UI 与文档高频术语；新增术语先入表再翻译。
- **AC-L6**：过期译文显示提示；不出现"看起来是中文但命令/默认值是旧版"的情况。

---

## 9. 不做什么

- 不承诺逐字翻译或逐页对应。
- 不为 L3 提供机器翻译全文。
- 不在英文未定稿时翻译。
- 不让译文成为法律/安全依据（以英文原文与律师审阅文本为准）。
- 不因为翻译而延迟英文发布。

---

## 10. 待确认

| 事项 | 负责 | 时机 |
| --- | --- | --- |
| 德语母语审阅人 | 发起人/社区 | M1 前 |
| 中文技术审阅人 | 发起人/社区 | M1 前 |
| L2 页面最终清单 | 文档维护者 | M2 前 |
| 文档站 i18n 路由方案 | 工程 | M1 |

---

## 11. 关联决议

- D-16：UI 默认英文，可切换简体中文与标准德语；文档英文为主，中文/德语简版。
- D-21：Astro Starlight；i18n 路由在 M1 落地。
- D-10：暂不商业化；翻译由社区与发起人共同维护。
