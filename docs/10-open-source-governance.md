# 开源治理：许可证、CLA、社区与发布

| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft / 待法律审阅 |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 项目发起人、维护者、贡献者 |
| 关联文档 | `09-security-privacy.md`、`11-roadmap.md`、`15-decision-log.md` |

> 本文档不是法律意见。CLA、许可证、商标条款在正式启用前应由合格律师审阅。

---

## 1. 项目定位

- **个人开源项目**：非隶属任何公司或组织，版权归发起人及贡献者。
- **目标**：允许更多开发者参与维护，同时保持项目方向与质量。
- **开放**：代码、设计、路线图、Issue、RFC 公开。
- **可持续**：通过 CLA、维护者梯队、自动化降低 bus factor。

---

## 2. 许可证选择

### 2.1 候选对比

| 许可证 | 专利授权 | 商业友好 | 与生态兼容 | 复杂度 | 推荐 |
| --- | --- | --- | --- | --- | --- |
| Apache-2.0 | 明确 | 高 | 与 OTel、CNCF 一致 | 中 | **推荐** |
| MIT | 无明确条款 | 最高 | 与 .NET 生态一致 | 低 | 可选 |
| BSD-3-Clause | 无明确条款 | 高 | 兼容 | 低 | 不推荐 |
| MPL-2.0 | 有 | 中 | 文件级 copyleft | 中 | 不推荐 |

**已决议（D-03）：Apache-2.0。**

### 2.2 推荐理由

1. **专利授权**：AI 诊断涉及数据处理与可视化，专利风险不可忽视；Apache-2.0 提供明确保护。
2. **商业友好**：企业可自由使用、修改、再分发，有利于采用。
3. **生态一致**：OpenTelemetry 使用 Apache-2.0；与 OTLP 互操作更自然。
4. **贡献者友好**：第 5 节明确贡献许可，与 CLA 配合清晰。

### 2.3 竞品许可证观察

| 项目 | 许可证 | 启示 |
| --- | --- | --- |
| Langfuse | MIT Expat + EE 目录 | 核心开源 + 企业版 |
| Arize Phoenix | Elastic License 2.0 | 限制托管服务，非 OSI |
| OpenLLMetry | Apache-2.0 | 纯开源 |
| Semantic Kernel | MIT | 微软生态惯例 |

Runesvaner 定位为**纯开源、无 EE 目录、无托管限制**，以 Apache-2.0 形成差异化。

---

## 3. CLA 设计

### 3.1 为什么需要 CLA

- 允许项目在必要时**再许可**（relicense）或调整治理。
- 明确贡献者授予版权与专利许可，降低法律风险。
- 统一贡献条款，避免每个 PR 单独谈判。

### 3.2 CLA 类型

| 类型 | 说明 | 适用 |
| --- | --- | --- |
| Individual CLA（ICLA） | 个人签署 | 个人贡献者 |
| Corporate CLA（CCLA） | 公司签署，覆盖员工 | 企业贡献者 |
| DCO | 仅声明有权提交，不授予再许可 | 轻量项目 |

已决议（D-04）：**ICLA + CCLA**；CLA Assistant（或等价 Action）作为实施建议。不使用 DCO 作为主要机制，避免版权归属不清。

### 3.3 CLA 核心条款（要点）

1. **版权许可**：贡献者授予项目所有者永久、全球、非独占、不可撤销、免版税的版权许可，允许复制、修改、分发、再许可。
2. **专利许可**：授予制造、使用、销售、进口等专利许可；若贡献者发起专利诉讼，许可终止。
3. **声明**：贡献者声明有权提交，且贡献为原创或已获得授权。
4. **无担保**：贡献按"原样"提供。
5. **保留权利**：贡献者保留版权；项目不主张贡献者的著作权。
6. **标识**：贡献者同意项目在 NOTICE / 贡献者列表中致谢（可选）。
7. **适用法律与管辖**：需与律师确认。
8. **可转让**：项目所有者可将权利转让给继任实体（用于治理延续）。

### 3.4 CLA 自动化

- 使用 [CLA Assistant](https://github.com/cla-assistant/cla-assistant) 或 GitHub Action。
- PR 首次提交时自动评论，要求签署。
- 签署记录存储于仓库 `cla-signatures` 分支或外部服务。
- 机器人检查未签署 PR，阻止合并。
- 企业贡献者通过 CCLA 覆盖多个员工。

### 3.5 CLA 的反模式

- 条款过度宽泛，导致贡献者不愿签署。
- 不说明再许可目的。
- 手动签署、无记录。
- 修改 CLA 不通知既有贡献者。
- 用 CLA 剥夺贡献者的专利防御权。

---

## 4. 治理模型

### 4.1 角色

| 角色 | 权限 | 产生方式 |
| --- | --- | --- |
| Owner（BDFL） | 最终决策、发布、商标、CLA | 发起人 |
| Maintainer | 合并、发布、路线图 | Owner 邀请 |
| Reviewer | 代码审查、标签 | Maintainer 邀请 |
| Contributor | 提交 PR/Issue | 任何人 |
| Security Responder | 漏洞响应 | Owner 指定 |

### 4.2 决策流程

| 类型 | 流程 |
| --- | --- |
| 日常修复 | 1 名 Reviewer 批准 + CI 通过 |
| 新功能 | Issue 讨论 + 1 名 Maintainer 批准 |
| 公共 API 变更 | RFC + API Review + 2 名 Maintainer |
| 数据模型变更 | RFC + 兼容性评估 |
| 许可证/治理变更 | RFC + Owner 批准 + 贡献者通知 |
| 安全问题 | 私下处理，修复后披露 |

### 4.3 Lazy Consensus

- 提案在 Issue/Discussion 公示 7 天。
- 无实质反对即通过。
- 有反对则进入 RFC 讨论；Owner 最终裁决。
- 阻塞性反对必须给出理由与替代方案。

### 4.4 RFC 流程

1. 复制 `docs/rfcs/0000-template.md`。
2. 提交 PR，状态 `Draft`。
3. 讨论期 ≥ 7 天。
4. 状态改为 `Accepted` / `Rejected` / `Deferred`。
5. 实施后在文档中链接。

RFC 适用范围：

- 公共 API 破坏性变更。
- 数据 schema 主版本。
- 新包/新依赖。
- 治理与许可证。
- 性能门槛调整。

---

## 5. 商标与品牌

- 项目名称与 Logo 由 Owner 持有（待 D-02/D-11）。
- 允许：在事实性描述中提及项目名、在 fork 中说明"基于 Runesvaner"。
- 不允许：使用项目名/Logo 暗示官方认可、作为商业产品名、注册相似商标。
- Fork 必须使用不同名称与 Logo，避免混淆。
- 商标政策独立于代码许可证。

---

## 6. 行为准则

采用 Contributor Covenant 2.1（或更新版本）：

- 禁止骚扰、歧视、人身攻击、泄露隐私。
- 维护者有权删除不当内容、临时或永久封禁。
- 报告渠道：项目邮箱（待定）+ 私下处理。
- 冲突处理：回避、调解、必要时由 Owner 决定。

---

## 7. 安全策略

- `SECURITY.md` 说明支持的版本、报告渠道、响应时限。
- 不支持私下修复后长期不披露。
- 安全公告通过 GitHub Security Advisories 发布。
- 关键漏洞修复后发布补丁与致谢。

---

## 8. 发布流程

### 8.1 版本策略

- SemVer 2.0：`MAJOR.MINOR.PATCH`。
- 预览：`1.0.0-preview.1`。
- 实验性包：`0.x`，不承诺兼容。
- 公共 API 破坏性变更 → 主版本。
- 数据 schema 主版本独立于包版本，但需在发行说明中标注。

### 8.2 分支模型

| 分支 | 用途 |
| --- | --- |
| `main` | 开发主线，始终可构建 |
| `release/1.x` | 维护旧版本 |
| `feature/*` | 功能分支，PR 后删除 |
| `hotfix/*` | 紧急修复 |

- Trunk-based，短生命周期分支。
- 合并要求：CI 全绿、1–2 名 Reviewer、无未解决讨论。
- 不使用 merge commit（squash 或 rebase，待定）。

### 8.3 发布检查清单

1. 版本号与变更日志。
2. 所有测试通过（单元、集成、AOT、基准）。
3. 公共 API 兼容性检查。
4. 依赖漏洞扫描。
5. SBOM 生成。
6. NuGet 包签名。
7. GitHub Release + 发行说明。
8. 文档站更新。
9. 社交渠道公告。

### 8.4 NuGet 发布

- 使用 Trusted Publishing（OIDC），避免长期 API Key。
- 包元数据：作者、仓库 URL、许可证、图标、README、SourceLink。
- 符号包（`.snupkg`）。
- 包验证：`dotnet package validate`。
- 预览包标记 `preview`，不与稳定包混用。

---

## 9. Issue 与 PR 规范

### 9.1 Issue 模板

- Bug：版本、OS、TFM、AOT、复现步骤、期望/实际、日志（脱敏）。
- Feature：问题、场景、建议方案、替代方案。
- Performance：基准、数据集、环境。
- Security：不公开，走 SECURITY.md。

### 9.2 PR 模板

- What does the pull request do
- Current behavior
- Expected behavior
- How implemented
- Checklist（测试、文档、AOT、性能）
- Breaking changes
- Fixed issues

### 9.3 标签

`bug`、`enhancement`、`performance`、`aot`、`privacy`、`good first issue`、`help wanted`、`needs design`、`blocked`、`breaking change`。

### 9.4 Good First Issue

- 范围明确、文件定位清晰、有验收标准。
- 不涉及公共 API、AOT、隐私。
- 提供 mentor（维护者）。

---

## 10. 贡献者成长

| 阶段 | 行为 | 认可 |
| --- | --- | --- |
| 首次贡献 | 修复文档/测试 | 致谢 |
| 常规贡献 | 多个 PR | 邀请为 Reviewer |
| Reviewer | 审查、标签 | 列入维护者名单 |
| Maintainer | 合并、发布、路线图 | 仓库权限 |

原则：贡献者按行为获得信任，不按公司/资历。

---

## 11. 资金与可持续性

| 方式 | 是否采用 | 说明 |
| --- | --- | --- |
| GitHub Sponsors | 接受（无捆绑） | 不影响功能 |
| Open Collective | 可选 | 透明账目 |
| 企业赞助 | 接受（无捆绑） | 不附带路线图控制权 |
| 商业支持 | 暂不 | 个人项目带宽有限 |
| 托管服务 | 明确不做 | 与本地优先定位冲突（D-10） |

原则：

- 不接受附带路线图控制权的赞助。
- 不接受与隐私承诺冲突的收入。
- 资金用途公开（基础设施、设计、安全审计）。

---

## 12. 继任与 Bus Factor

- 至少 2 名 Maintainer 拥有发布权限。
- CLA 允许权利转让给继任实体。
- 关键决策文档化，避免口口相传。
- 若 Owner 连续 36 个月不活跃（D-11）：
  - 维护者可通过 RFC 推举新 Owner。
  - 商标与域名需提前约定转让机制。

---

## 13. 法律审阅清单

在正式开源前，律师应审阅：

1. 许可证文本与 NOTICE。
2. ICLA / CCLA 条款。
3. 商标注册与使用政策。
4. 隐私政策（若未来有网站/云服务）。
5. 价格表数据来源与使用许可。
6. 第三方依赖许可证兼容性。
7. 出口管制与制裁条款（如适用）。
8. 贡献者专利条款。

---

## 14. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。与本文件直接相关：

- D-02：项目名 Runesvaner；商标待检索。
- D-03：Apache-2.0。
- D-04：ICLA + CCLA + 自动化。
- D-10：暂不商业化；接受无捆绑赞助；不做托管。
- D-11：个人持有；36 个月继任条款。
- D-17：LTS + 最新 STS。