# 安全与隐私设计

> **中文版（Legacy full version）** — 英文主文档见 [`../09-security-privacy.md`](../09-security-privacy.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 工程、安全、合规 |
| 关联文档 | `05-technical-design.md`、`06-observability-model.md`、`10-open-source-governance.md` |

---

## 1. 安全目标

1. **不泄露内容**：默认不采集 Prompt / Completion；不记录密钥。
2. **不扩大攻击面**：本地 Studio 仅回环、带令牌、无外部资源。
3. **不破坏业务**：采集链路故障不影响请求。
4. **可审计**：数据在哪、保留多久、导出到哪里，用户可查。
5. **供应链可信**：依赖最小化、签名、SBOM、漏洞响应。

---

## 2. 资产与信任边界

### 2.1 资产

| 资产 | 敏感度 | 位置 |
| --- | --- | --- |
| Prompt / Completion | 高 | 进程内存、可选本地文件 |
| API Key / Token | 极高 | 进程内存、环境变量 |
| 租户 / 用户标识 | 高 | 本地文件、导出 |
| 成本与用量 | 中 | 本地文件、导出 |
| 价格表 | 低 | 本地文件、包内 |
| 本地 Studio 令牌 | 高 | 进程内存、临时文件 |
| 诊断会话文件 | 中高 | 用户目录 |

### 2.2 信任边界

    [业务进程]
      ├── Runesvaner 采集（同进程，可信）
      ├── 本地文件系统（用户权限，半可信）
      ├── Studio HTTP（回环，需认证）
      └── OTLP 导出（外部网络，不可信）

| 边界 | 威胁 |
| --- | --- |
| 进程内 | 恶意库、反射滥用、内存转储 |
| 文件系统 | 其他本地用户读取、符号链接、路径穿越 |
| Studio HTTP | DNS rebinding、CSRF、XSS、未授权访问 |
| OTLP | 中间人、错误端点、敏感字段泄露 |
| 价格表 | 恶意 JSON、路径穿越、远端篡改 |
| 供应链 | 依赖投毒、构建机入侵 |

---

## 3. 隐私设计

### 3.1 内容捕获级别

| 级别 | 采集 | 默认 | 用途 |
| --- | --- | --- | --- |
| `None` | 不采集 Prompt/Completion | 是 | 绝大多数场景 |
| `MetadataOnly` | 长度、哈希、Token 估算 | 否 | 需要关联但不看内容 |
| `Full` | 原文（有上限） | 否 | 本地调试，显式开启 |

规则：

- `Full` 开启时，CLI 与 Studio 显示醒目警告。
- `Full` 默认保留 7 天，且不计入常规聚合导出。
- `Full` 不改变 OTLP 默认导出行为；导出前必须脱敏。

### 3.2 哈希与标识

- `user` / `tenant` 默认 SHA-256 截断（16 hex），加进程级 salt。
- salt 存储于本地配置文件，权限 0600；不导出。
- 哈希不是匿名化承诺；文档明确"可关联、不可逆"的边界。
- 不哈希 `model`、`provider`、`feature` 等低敏维度。

### 3.3 敏感 Header

默认丢弃：

- `Authorization`
- `Proxy-Authorization`
- `api-key`
- `x-api-key`
- `cookie`
- `set-cookie`
- `x-amz-security-token`
- `x-goog-api-key`

规则：

- 白名单优先于黑名单：只采集明确允许的头。
- 若用户自定义 header 采集，必须显式配置，并在文档中警告。

### 3.4 日志

- 日志不包含 Prompt、Completion、Key、Cookie。
- 错误消息只记录哈希或类型；不记录原始异常消息中的敏感内容（可能包含请求体）。
- 调试日志默认关闭；开启时显示警告。

---

## 4. 本地 Studio 安全

### 4.1 网络绑定

- 默认 `127.0.0.1`，随机端口。
- 禁止默认 `0.0.0.0`；如用户显式要求远程访问，必须：
  - 显示高风险警告。
  - 要求设置访问令牌。
  - 文档说明反向代理与 TLS 责任。

### 4.2 认证

- 启动时生成 32 字节随机令牌（`RandomNumberGenerator`）。
- URL 携带一次性令牌；API 使用 `Authorization: Bearer`。
- 令牌仅存于进程内存；进程退出失效。
- 不把令牌写入 shell history：CLI 输出使用 `open` 命令或本地文件。

### 4.3 Host 校验与 DNS Rebinding

- 校验 `Host` 头为 `127.0.0.1:<port>` 或 `localhost:<port>`。
- 校验 `Origin`（若存在）为同源。
- 关闭 CORS；不响应跨域预检。
- 设置 `X-Content-Type-Options: nosniff`、`X-Frame-Options: DENY`、`Referrer-Policy: no-referrer`。
- CSP：`default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'`。
- 不使用外部 CDN、字体、图标。

### 4.4 输入处理

- 查询参数白名单与长度限制。
- 分页上限（默认 100，最大 1000）。
- 排序字段白名单，防 SQL 注入。
- JSONL 解析使用 `Utf8JsonReader` + 源生成；未知字段忽略。
- 渲染使用 React 默认转义；禁止 `dangerouslySetInnerHTML`。
- 文件名/路径参数不接受用户输入；会话 ID 使用 ULID 校验。

### 4.5 CSRF

- 本地 token 已提供认证；额外要求：
  - 所有写操作使用 `POST` + `Content-Type: application/json`。
  - 校验 `Origin` / `Sec-Fetch-Site`。
  - 不使用 cookie 认证。

---

## 5. 文件系统安全

| 威胁 | 缓解 |
| --- | --- |
| 路径穿越 | 所有路径由配置根目录 + 固定子目录组成；拒绝 `..` |
| 符号链接 | 写入前检查目标不是 symlink；使用 `FileOptions` 安全打开 |
| 权限过大 | Unix `0600` 文件、`0700` 目录；Windows ACL 仅当前用户 |
| 文件被替换 | 打开时校验 inode/句柄；敏感文件使用独占写 |
| 磁盘耗尽 | 保留策略 + 最大大小 + 熔断 |
| 恶意 JSONL | 限制单行大小、嵌套深度、字段数量 |
| SQLite WAL/SHM | 与主库同权限（0600）；关闭时清理；不接受用户指定路径 |
| 归档炸弹 | 不支持压缩导入；如支持，限制解压比 |

默认目录：

    ~/.runesvaner/            # 用户级
    .runesvaner/              # 项目级（优先）

v1 平台范围：Windows 10/11 与 Linux；macOS 非目标（D-15）。

---

## 6. 导出安全

### 6.1 OTLP

- 默认关闭。
- 端点必须显式配置；默认不允许 `http://`（除非 localhost）。
- 支持 `https://` 与本地 collector。
- Headers 存储在配置中，权限 0600；日志中脱敏。
- 导出前执行 `IContentRedactor`。
- 失败不影响业务。

### 6.2 导出字段

默认导出：

- 时间、耗时、分段、Token、成本、provider、model、status。
- `feature`、`tenant_hash`、`prompt_version`（低基数、哈希）。

默认不导出：

- Prompt / Completion 原文。
- `user_id` 原文。
- 原始 URL（可能含查询参数）。
- 异常消息原文。

### 6.3 第三方兼容

- Langfuse / Phoenix 通过 OTLP 接收；字段映射在文档中列出。
- 不保证第三方平台的隐私策略；用户需自行评估。
- 提供"仅本地"模式，完全不导出。

---

## 7. 供应链安全

| 措施 | 说明 |
| --- | --- |
| 依赖最小化 | 核心零第三方依赖；可选包按需 |
| 锁定版本 | `Directory.Packages.props` 集中管理 |
| NuGet 签名 | 发布包签名；验证依赖签名 |
| SBOM | 每次发布生成 SPDX/CycloneDX |
| CodeQL | GitHub Actions 每周 + PR |
| Dependabot | 每周依赖更新；安全更新即时 |
| Secret scanning | 启用 GitHub secret scanning + push protection |
| 构建隔离 | GitHub-hosted runner；最小权限 token |
| 发布认证 | NuGet Trusted Publishing（OIDC），不使用长期 API Key |
| 制品签名 | GitHub Artifact Attestations（如可用） |
| 代码审查 | 至少 1 名维护者批准；敏感变更 2 人 |

### 7.1 依赖审查清单

新增依赖必须回答：

1. 许可证是否兼容 Apache-2.0？
2. 是否支持 AOT/Trim？
3. 维护状态与安全记录？
4. 体积增量？
5. 是否可被替代？
6. 是否引入原生依赖？

---

## 8. 威胁模型（STRIDE 简化）

| 威胁 | 场景 | 缓解 |
| --- | --- | --- |
| Spoofing | 伪造 Studio 请求 | 本地 token + Host/Origin 校验 |
| Tampering | 篡改 JSONL / 价格表 | 校验和；只读打开；版本字段 |
| Repudiation | 无法证明数据来源 | session_id、时间戳、schema 版本 |
| Information Disclosure | Prompt 泄露 | 默认不采集；导出脱敏；文件权限 |
| Denial of Service | 大量请求导致队列膨胀 | 背压、丢弃最旧、熔断 |
| Elevation of Privilege | 通过路径穿越写文件 | 路径白名单、symlink 检查 |
| XSS | 恶意 Prompt 在 Studio 渲染 | React 转义；CSP；禁止 HTML 注入 |
| CSRF | 恶意网页调用本地 API | token + Origin 校验 |
| DNS Rebinding | 恶意域名解析到 127.0.0.1 | Host 校验 + token |
| 供应链 | 恶意 NuGet 包 | 签名、锁定、审查、SBOM |
| 提示注入 | 恶意内容影响诊断结论 | 诊断结论只基于数值；不执行内容 |

> 提示注入不是 Runesvaner 的执行风险（不调用 LLM 处理内容），但若未来做"AI 辅助根因分析"，必须重新评估。

---

## 9. 合规提示

以下为工程建议，非法律意见：

- **GDPR**：若采集用户标识或内容，需明确合法性基础、保留期限、数据主体权利。
- **CCPA**：提供删除与导出机制。
- **数据驻留**：本地优先天然满足；OTLP 导出需用户确认区域。
- **行业合规**：医疗、金融场景默认 `ContentCapture.None`。
- **DPA**：项目本身不提供 SaaS，无 DPA；企业自建需自行评估。

---

## 10. 安全测试

| 测试 | 频率 |
| --- | --- |
| 依赖漏洞扫描 | 每次 PR + 每日 |
| CodeQL | 每次 PR + 每周 |
| Secret 扫描 | 每次 push |
| 路径穿越/符号链接测试 | 每次存储变更 |
| Studio 认证绕过测试 | 每次 API 变更 |
| XSS/CSRF 测试 | 每次 UI 变更 |
| 恶意 JSONL 模糊测试 | M2 起 |
| AOT 安全 smoke | 每次发布 |
| 第三方渗透测试 | 1.0 前（预算允许） |

---

## 11. 漏洞响应

1. 接收：`SECURITY.md` 提供私下渠道。
2. 确认：48 小时内响应。
3. 修复：P0 一个发布周期；P1 两个周期。
4. 披露：修复后 90 天或与报告者约定。
5. 致谢：经同意后列出。
6. CVE：必要时申请。

---

## 12. 事件响应预案

| 事件 | 动作 |
| --- | --- |
| 包被投毒 | 撤包、发布安全公告、轮换密钥 |
| Studio 认证绕过 | 立即发布补丁、建议用户关闭 Studio |
| 内容泄露 | 停止导出、清理文件、通知用户 |
| 供应链攻击 | 冻结发布、审计构建、重建制品 |
| 价格表错误 | 发布修正版本、标注历史数据 |

---

## 13. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。与本文件直接相关：

- D-07：库 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
- D-09：默认内容捕获 `None`。
- D-10：暂不商业化；接受无捆绑赞助；不做托管服务。
- D-11：商标个人持有；36 个月不活跃可继任转让。
- D-13：OTLP 默认关闭。
- D-20：不强制兼容第三方语义。
