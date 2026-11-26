# 测试策略与性能基准

> **中文版（Legacy full version）** — 英文主文档见 [`../12-testing-and-benchmarks.md`](../12-testing-and-benchmarks.md)。按 D-16/18，后续将精简为核心简版。


| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 工程、CI、贡献者 |
| 关联文档 | `05-technical-design.md`、`08-native-aot-strategy.md`、`11-roadmap.md` |

---

## 1. 测试目标

1. **正确性**：分段、用量、成本、流式语义与契约一致。
2. **不干扰**：采集失败、存储失败、导出失败都不影响业务。
3. **低开销**：性能门槛可量化、可回归。
4. **AOT 可用**：每次发布前验证。
5. **隐私**：默认不含内容；导出可脱敏。
6. **可维护**：测试快速、稳定、可诊断。

---

## 2. 测试金字塔

| 层级 | 比例 | 内容 | 运行频率 |
| --- | --- | --- | --- |
| 单元测试 | 70% | 纯函数、成本、分段、采样、序列化 | 每次 PR |
| 集成测试 | 20% | MEAI 管线、SQLite、JSONL 回退、CLI、Studio API | 每次 PR |
| 契约测试 | 5% | MEAI 10.x/11.x、OTel 映射 | 每日 + 发布 |
| AOT/Trim | 3% | 发布 + smoke | 每次发布 |
| 基准 | 2% | BenchmarkDotNet | 每次 PR（门槛） |
| 手工/探索 | — | UI、真实 provider | 里程碑 |

---

## 3. 单元测试

### 3.1 覆盖范围

| 模块 | 关键用例 |
| --- | --- |
| 分段 | 起止偏移、并行 lane、残差、estimated |
| 成本 | 缓存、推理、阶梯、批量、未知、精度、舍入 |
| 采样 | 概率边界、加权、规则 |
| 时钟 | 单调、假时钟、溢出 |
| 序列化 | schema、未知字段、缺字段、超大字段 |
| 脱敏 | 哈希、header 黑名单、路径 |
| 保留 | 时间/大小阈值、活跃会话保护 |
| 熔断 | 队列满、IO 失败、恢复 |
| 流式状态机 | 首 token、空 delta、取消、异常、dispose |

### 3.2 断言风格

- 使用 xUnit v3（待 D-17 确认）。
- 断言库待定：优先内置 `Assert` + 自定义扩展，减少依赖；若使用 FluentAssertions，需注意许可证变更。
- 不使用快照库作为唯一断言；关键字段显式断言。
- 浮点/金额：使用 `decimal` 精确比较；时间使用容差。

### 3.3 测试数据

- 价格表：固定 JSON，版本 `test-1`。
- 请求：构造器 + builder，避免巨大匿名对象。
- 流式：自定义 `IAsyncEnumerable`，可控制延迟、异常、取消。
- 隐私：包含假 Key、假 PII 的 fixture，验证不落盘。

---

## 4. 集成测试

### 4.1 MEAI 管线

| 用例 | 验证 |
| --- | --- |
| 非流式 Chat | 返回值、记录字段、无重复 Span |
| 流式 Chat | chunk 顺序、内容、TTFT、取消 |
| Embedding | 批量大小、usage、维度 |
| 中间件顺序 | `UseRunesvaner().UseOpenTelemetry()` 与反序 |
| 异常 | inner 抛异常时记录并原样抛出 |
| Dispose | inner 被释放一次 |
| GetService | 透传正确 |

### 4.2 存储

| 用例 | 验证 |
| --- | --- |
| 库 SQLite 默认写入 | 事务、WAL、查询一致 |
| 库 SQLite 初始化失败 | 自动回退 JSONL，记录原因 |
| CLI 默认 JSONL | 行完整、可读、顺序 |
| JSONL 回退追加 | 行完整、可读、顺序 |
| 崩溃模拟 | SQLite WAL 恢复；JSONL 半行忽略 |
| 并发写 | 单 writer、无交错 |
| 保留清理 | 删除最旧、不删活跃 |
| 磁盘满 | 熔断、业务成功 |
| JSONL 导入 SQLite | 与源数据一致、可重建 |

### 4.3 CLI

| 用例 | 验证 |
| --- | --- |
| `report` | 输出字段、退出码 |
| `report --json` | schema 兼容 |
| `top` | 排序、过滤 |
| `export` | 文件格式、脱敏 |
| `doctor` | 检测项、错误信息 |
| 无数据 | 退出码 3、可行动提示 |

### 4.4 Studio API

| 用例 | 验证 |
| --- | --- |
| 无 token | 401 |
| 错误 token | 401 |
| 正确 token | 200 |
| 非 localhost Host | 400 |
| 分页 | 上限、游标 |
| 排序注入 | 白名单拒绝 |
| XSS fixture | 转义 |
| 空数据 | 空状态 |

---

## 5. 契约测试

### 5.1 MEAI 版本矩阵

| 版本线 | 状态 |
| --- | --- |
| 10.x（LTS 线） | 必须 |
| 11.x（最新） | 必须 |
| 未来预览 | 尽力 |

测试内容：

- `IChatClient` 所有方法签名可用。
- `ChatResponseUpdate` 结构。
- `IEmbeddingGenerator` 泛型约束。
- `ChatClientBuilder` 扩展方法。
- `GetService` 行为。

策略：

- 单独 `Runesvaner.ContractTests` 项目。
- 使用多版本包引用（通过条件编译或独立项目）。
- 失败时优先适配，不要求用户升级。

### 5.2 OTel 映射

- 用固定 Runesvaner 事件，导出为 OTLP，校验字段。
- 与 GenAI semconv 版本绑定；升级时审查。
- 不把 Development 字段当稳定契约。

---

## 6. AOT 与 Trim 测试

见 `08-native-aot-strategy.md`。补充：

| 测试 | 内容 |
| --- | --- |
| 构建 | `PublishAot` 0 警告 |
| 运行 | 非流式、流式、错误、取消 |
| 字段 | 会话 schema 完整 |
| 体积 | 记录增量 |
| 启动 | 冷启动中位数 |
| 依赖 | SQLite/OTel 可选矩阵 |

---

## 7. 性能基准

### 7.1 工具

- BenchmarkDotNet（Release、Server GC? 按场景）。
- 固定硬件：CI runner 可能噪声大；门槛用相对基线 + 容差。
- 本地基准用于诊断，CI 用于回归。

### 7.2 场景

| 场景 | 说明 | 指标 |
| --- | --- | --- |
| B01 采样关闭 | 仅 `IsEnabled` 判断 | ns/op、分配 |
| B02 采样开启、无存储 | 记录构造 + 分段 | µs/op、分配 |
| B03 采样开启、库 SQLite | 完整链路（异步落盘） | µs/op、P95 |
| B03b CLI 默认 JSONL | CLI AOT 链路 | µs/op、P95 |
| B04 流式 100 chunk | 每 chunk 记录 | µs/chunk |
| B05 成本计算 | 已知/未知/阶梯 | µs/op |
| B06 10k 请求批量 | 吞吐 | req/s、内存 |
| B07 Studio 查询 | 1 万/10 万请求 | ms/query |

### 7.3 初始门槛

| 指标 | M1 | M2 | 1.0 |
| --- | --- | --- | --- |
| B01 | ≤ 100 ns | ≤ 50 ns | ≤ 50 ns |
| B02 | ≤ 500 µs | ≤ 200 µs | ≤ 100 µs |
| B03 P95 | ≤ 2 ms | ≤ 1 ms | ≤ 0.5 ms |
| B04 | ≤ 2 µs | ≤ 1 µs | ≤ 1 µs |
| CPU 增量 | ≤ 2% | ≤ 1% | ≤ 1% |
| 分配/请求 | ≤ 8 KB | ≤ 4 KB | ≤ 2 KB |

> 门槛是工程目标，不是营销承诺；M1 用真实测量校准。若无法达标，优先减少分配与字符串，而不是删除功能。

### 7.4 回归策略

- 每个 PR 运行 B01–B05。
- 与 `main` 基线比较；超过阈值 → 失败或要求解释。
- 噪声处理：至少 3 次运行，取中位数；CI runner 差异用相对值。
- 基准结果存档，生成趋势图（M3）。

### 7.5 基准伦理

- 不隐藏开销：同时报告 P50/P95/P99、分配、GC。
- 不只用"无操作"场景。
- 不把 provider 网络时间算作 Runesvaner 开销。
- 公开基准代码与数据。

---

## 8. 流式专项测试

| 用例 | 期望 |
| --- | --- |
| 正常 100 chunk | 顺序、内容、TTFT、TPS |
| 首 chunk 为空 | TTFT 不算；等首个非空 |
| 中途异常 | 保留已接收、原样抛 |
| 用户取消 | `cancelled=true`、不抛 |
| 超时 | `timeout`、保留部分 |
| 慢消费者 | 不预读、不阻塞 |
| Dispose 提前 | flush、记录 |
| 多次枚举 | 与 inner 行为一致（通常只允许一次） |

---

## 9. 隐私与安全测试

| 用例 | 期望 |
| --- | --- |
| 默认配置 | 无 Prompt/Completion |
| Full 配置 | 有内容、有警告、保留 7 天 |
| Authorization header | 不落盘 |
| 路径穿越 | 拒绝 |
| 符号链接 | 拒绝写入 |
| 恶意 JSONL | 安全解析、限制深度 |
| Studio 无 token | 401 |
| Studio 外域 Host | 400 |
| XSS Prompt | 转义 |
| 导出脱敏 | 敏感字段移除 |

---

## 10. 测试环境

| 环境 | 用途 |
| --- | --- |
| 本地 | 快速单元/集成 |
| GitHub Actions | PR 全量 + Windows/Linux |

RID 范围：`win-x64`、`linux-x64`；`linux-arm64` 可选；`win-arm64` 非目标（D-24）。
| 夜间 | 契约、长基准、模糊测试 |
| 发布前 | AOT、安全、手工 UI |

### 10.1 依赖模拟

- 不调用真实 LLM provider。
- 使用本地 `HttpMessageHandler` mock 或 ASP.NET Core TestServer。
- 流式 mock 支持延迟、分块、异常、取消。
- 价格表使用固定 fixture。

### 10.2 数据集

- 合成数据：10 / 1 万 / 10 万请求。
- 包含：正常、慢、错误、重试、缓存、未知价格、超预算。
- 不含真实用户数据；如需真实数据，必须脱敏并获授权。

---

## 11. 覆盖率

| 范围 | 目标 |
| --- | --- |
| 核心（分段、成本、采样） | ≥ 90% 行 + 分支 |
| 集成层 | ≥ 80% |
| CLI | ≥ 70% |
| Studio API | ≥ 80% |
| UI | 关键路径 E2E（M3） |

覆盖率是护栏，不是目标；不为了数字写无意义测试。

---

## 12. 发布前检查

1. 单元 + 集成 + 契约全绿。
2. AOT Windows/Linux smoke。
3. 基准无回归。
4. 隐私测试通过。
5. Studio 安全测试通过。
6. 依赖漏洞扫描无高危。
7. 文档与变更日志更新。
8. 手工验证"30 秒体验"。

---

## 13. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md` 与 `19-open-implementation-decisions.md`。与本文件直接相关：

- D-07：库 SQLite 默认；CLI AOT 默认 JSONL；失败回退。
- D-17：xUnit v3；MEAI 10.x/11.x；LTS + 最新 STS。
- D-15：Windows/Linux runner；macOS 非目标。
- D-24：`win-arm64` 非目标；`linux-arm64` 可选。
- D-14：本地 100%，生产建议 10%；尾采样 M3。
- D-06：UI E2E 使用 Playwright（M3）。
