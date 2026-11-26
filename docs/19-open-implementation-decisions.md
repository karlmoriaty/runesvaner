# 实施级决议记录：D-23–D-25

| 字段 | 值 |
| --- | --- |
| 文档状态 | **Resolved（已决议）** |
| 版本 | 1.0 |
| 决议日期 | 2026-11-26 |
| 决议人 | 项目发起人 |
| 关联文档 | `11-roadmap.md`、`15-decision-log.md`、`16-brand-naming.md`、`17-docs-and-community.md` |

> D-01–D-25 已全部决议；本文档记录 3 个实施级事项 D-23–D-25 的最终结论。
> 这些结论不改变产品定位、架构或治理，只影响 M0/M1 的第一批基础设施与发布准备。

---

## 1. 决议摘要

| ID | 事项 | 最终决议 | 状态 |
| --- | --- | --- | --- |
| D-23 | 文档站托管与域名 | GitHub Pages；M1 暂不购买域名 | 已决议 |
| D-24 | Windows ARM64 | v1 不支持 `win-arm64`；`win-x64` + `linux-x64`，`linux-arm64` 可选 | 已决议 |
| D-25 | 发布准备时机 | 商标检索暂缓；NuGet 根包 M0 占位；CLA 首次外部 PR 前审阅 | 已决议 |

---

## 2. D-23 文档站托管与域名

**决议**：

- 文档站使用 **GitHub Pages** 托管。
- M1 暂不购买自定义域名。
- 默认 URL：`https://karlmoriaty.github.io/runesvaner/`。
- 1.0 后再评估 `runesvaner.dev` 等域名。

**理由**：

1. 项目已在 GitHub，Pages 与仓库、Actions 集成最直接。
2. 个人项目早期不需要域名预算与续费负担。
3. 候选域名当前均无 DNS 解析，后续仍可评估注册。

**影响**：

- M1 文档站 CI 使用 GitHub Actions → Pages。
- README、NuGet 包项目链接使用 Pages URL。
- 若未来切换域名，需要设置重定向与更新 CNAME。

---

## 3. D-24 Windows ARM64

**决议**：

- v1 **不支持** `win-arm64`。
- 保证 `win-x64` 与 `linux-x64`。
- `linux-arm64` 作为可选验证目标，不阻塞发布。
- 若未来有明确需求，可作为 v2 社区支持项。

**理由**：

1. 目标用户以 Windows x64 与 Linux x64 为主。
2. SQLite 在 AOT 下的 native 资产已是风险点；增加 `win-arm64` 会放大 CI 与验证成本。
3. 个人项目优先保证两个主平台的质量。

**影响**：

- `08-native-aot-strategy.md` 的 RID 矩阵：不添加 `win-arm64`。
- `12-testing-and-benchmarks.md` 的 CI 矩阵：不添加 `win-arm64`。
- NuGet 包不发布 `win-arm64` 原生资产。

---

## 4. D-25 发布准备时机

**决议**：

| 子项 | 最终决议 | 说明 |
| --- | --- | --- |
| 商标检索（类别 9/42） | **暂缓** | 发起人目前没有商标律师/预算；等后期需要时再提。不作为 M0 门槛；正式公开预览或 1.0 前再评估 |
| NuGet 根包占位（0.0.1） | **M0** | 13 个计划包 ID 当前 404；占位成本低，避免被抢注 |
| CLA 律师审阅 | **首次接受外部 PR 前** | 不阻塞个人开发；接受外部贡献前必须有可执行文本 |

**理由**：

1. 商标检索暂缓是发起人的明确决定；名称已通过 NuGet/GitHub/域名可用性初筛，工作名阶段可先用。
2. NuGet 根包占位是低成本、可逆的品牌保护动作。
3. CLA 在首次外部 PR 前完成即可，不需要在 M0 阻塞代码开发。

**影响**：

- `11-roadmap.md`：M0 退出标准不再要求"商标检索完成"，改为"商标检索暂缓，风险已记录"；NuGet 占位仍保留。
- `16-brand-naming.md`：商标状态改为"暂缓，正式预览前再评估"。
- `13-risk-register.md`：R-11 商标风险保留，但标注"暂缓"。
- `10-open-source-governance.md`：CLA 审阅时机为首次外部 PR 前。

---

## 5. 执行项

| 事项 | 状态 | 负责 | 时机 |
| --- | --- | --- | --- |
| Git 提交邮箱 | **已完成**：用户确认 `3070687583@qq.com` 已绑定 GitHub 账号 `karlmoriaty` | 用户 | 已完成 |
| NuGet 账号注册/验证 | 需用户本人操作 | 用户 | M0 前 |
| NuGet 根包占位（0.0.1） | 待执行（需 NuGet API key） | 用户 | M0 |
| 商标检索 | **暂缓** | 用户 | 后期需要时 |
| CLA 律师审阅 | 待执行 | 用户 | 首次外部 PR 前 |
| GitHub 仓库 `runesvaner` | 已创建（公开） | 已完成 | 2026-11-26 |
| 文档站 GitHub Pages | 待配置 | 开工后 | M1 |
| 德语母语审阅人 | 待定 | 发起人/社区 | M1 前 |
| 中文技术审阅人 | 待定 | 发起人/社区 | M1 前 |

---

## 6. 不需要决议的固定项

| 事项 | 值 |
| --- | --- |
| 仓库名 | `runesvaner` |
| 产品名 / 包名 / 文档站名 | `Runesvaner` |
| 仓库可见性 | 公开 |
| 默认分支 | `main` |
| 许可证 | Apache-2.0 |
| 目标框架 | `net10.0` |
| 平台 | Windows 10/11 + Linux；macOS 非目标 |
| 存储默认 | 库/应用内 SQLite；CLI AOT 默认 JSONL |

---

## 7. 后续

D-23–D-25 已记录。项目已于 **2026-11-26 开工**，第一批动作已完成：

1. 已创建公开仓库 `karlmoriaty/runesvaner`。
2. 已执行 `git init -b main`，配置 `origin`，并推送首次提交。
3. 已添加 `LICENSE`、`NOTICE`、`.gitignore`、`Directory.Build.props`、`global.json`。
4. 已建立 `Runesvaner.sln` 与 M1 项目骨架（Abstractions、Core、MEAI、JSONL、SQLite、CLI、tests、samples）。
5. CI 已通过：Windows/Linux 构建测试 + Windows Native AOT smoke。
6. 仍待完成：GitHub Pages、NuGet 根包占位（需 NuGet 账号/API key）、RAG sample、基准基线、成本引擎、Studio、OTLP。

---

## 8. 变更记录

| 日期 | 版本 | 变更 |
| --- | --- | --- |
| 2026-11-26 | 0.1 | 初始 D-23–D-25 待决议 |
| 2026-11-26 | 1.0 | 发起人全部按推荐决议；商标检索改为暂缓；Git 邮箱确认已绑定 |