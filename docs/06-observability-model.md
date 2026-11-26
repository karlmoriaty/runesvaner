# 遥测数据模型与语义约定

| 字段 | 值 |
| --- | --- |
| 文档状态 | Draft |
| 版本 | 0.1 |
| 日期 | 2026-11-25 |
| 受众 | 工程、数据、互操作 |
| 关联文档 | `05-technical-design.md`、`07-api-design.md` |
| 标准参考 | OpenTelemetry GenAI Semantic Conventions（Development） |

---

## 1. 设计原则

1. **对齐标准**：能映射到 `gen_ai.*` 的字段优先使用标准名。
2. **扩展隔离**：Runesvaner 特有字段统一使用 `runesvaner.*` 前缀，不污染标准命名空间。
3. **指标低基数**：Metric labels 不包含 request_id、user_id、prompt_version 等高基数值。
4. **Trace 高基数**：高维信息放在 Span attributes / events 中，通过 trace_id 关联。
5. **不猜测**：缺失的用量、价格、时间一律标记 `unknown` 或 `estimated`。
6. **Schema 版本化**：每个事件带 `schema` 字段；破坏性变更升主版本。
7. **隐私分级**：字段标注 `public` / `internal` / `sensitive`，导出前可按级别过滤。

---

## 2. 实体模型

    RunesvanerSession
      ├── RunesvanerRequest (1..N)
      │     ├── RunesvanerSegment (0..N)
      │     ├── RunesvanerUsage (0..1)
      │     ├── RunesvanerCost (0..1)
      │     ├── RunesvanerStream (0..1)
      │     ├── RunesvanerRetry (0..N)
      │     └── RunesvanerError (0..N)
      └── SelfMonitorSample (0..N)

| 实体 | 说明 |
| --- | --- |
| Session | 一次进程运行或一次诊断采集 |
| Request | 一次逻辑 AI 操作（chat / embeddings / tool / vector） |
| Segment | 请求内部的一段时间区间 |
| Usage | Token 与多模态用量 |
| Cost | 分项与总成本 |
| Stream | 流式行为指标 |
| Retry | 一次尝试 |
| Error | 异常或 provider 错误 |

---

## 3. 标识与关联

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `runesvaner.session_id` | string (ULID) | 会话标识 |
| `runesvaner.request_id` | string (ULID) | 请求标识 |
| `trace_id` | string (W3C 32 hex) | 与 OTel 对齐 |
| `span_id` | string (W3C 16 hex) | 与 OTel 对齐 |
| `runesvaner.parent_request_id` | string | Agent/工具嵌套 |
| `runesvaner.attempt` | int | 从 1 开始 |
| `runesvaner.schema` | string | 例如 `runesvaner.event.v1` |

ULID 选择理由：可按时间排序、字符串安全、无需数据库序列。

---

## 4. Span 类型

| `runesvaner.span.kind` | 对应 OTel 操作 | 说明 |
| --- | --- | --- |
| `request` | — | Runesvaner 请求根 |
| `chat` | `gen_ai.operation.name=chat` | Chat 推理 |
| `embeddings` | `embeddings` | Embedding |
| `tool` | `execute_tool` | 工具/函数调用 |
| `vector_search` | `retrieval` | 向量检索 |
| `rerank` | `retrieval` | 重排 |
| `serialize` | — | 序列化 |
| `queue` | — | 应用内排队 |
| `retry` | — | 重试等待 |
| `cache` | — | 缓存查找/写入 |

> OTel GenAI 的 Span 命名与操作名仍在 Development；M1 冻结映射表，见第 10 节。

---

## 5. 延迟分段模型

### 5.1 分段字段

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `kind` | enum | 是 | 见第 5.2 节 |
| `start_offset_us` | int64 | 是 | 相对请求开始 |
| `duration_us` | int64 | 是 | 持续时间 |
| `estimated` | bool | 是 | 是否为估算 |
| `measurement` | enum | 是 | `stopwatch` / `http_diagnostic` / `derived` / `residual` |
| `lane` | int | 否 | 并行分组，默认 0 |
| `status` | enum | 否 | `ok` / `error` / `cancelled` |
| `attributes` | map | 否 | 低基数补充信息 |

### 5.2 分段类型

| `kind` | 起点 | 终点 | 默认测量 |
| --- | --- | --- | --- |
| `queue` | 中间件进入 | 开始调用 inner | stopwatch |
| `auth` | 认证开始 | 认证结束 | stopwatch |
| `connect` | 连接开始 | 连接建立 | http_diagnostic |
| `tls` | TLS 开始 | TLS 完成 | http_diagnostic |
| `upload` | 请求写入开始 | 请求写入完成 | stopwatch |
| `provider_queue` | 上传完成 | 首字节 | derived（estimated） |
| `ttft` | 请求开始 | 首个非空 delta | stopwatch |
| `generation` | 首个 delta | 最后 delta | stopwatch |
| `tool` | 工具开始 | 工具结束 | stopwatch |
| `vector_search` | 检索开始 | 检索结束 | stopwatch |
| `serialize` | 序列化开始 | 序列化结束 | stopwatch |
| `retry_wait` | 失败 | 下一次尝试 | stopwatch |
| `cache_lookup` | 缓存查询开始 | 返回 | stopwatch |
| `unaccounted` | 残差 | — | residual |
| `overlap` | 并行重叠 | — | residual |

### 5.3 公式

- `ttfb = first_byte_ts - request_start_ts`
- `ttft = first_non_empty_delta_ts - request_start_ts`
- `generation = last_delta_ts - first_non_empty_delta_ts`
- `provider_queue = ttfb - upload_complete_ts`（包含网络 RTT，estimated）
- `itl_i = delta_ts_i - delta_ts_{i-1}`
- `tps = output_tokens / generation_seconds`
- `overlap = sum(parallel durations) - union(parallel intervals)`
- `unaccounted = total_duration - union(all non-residual intervals)`

### 5.4 展示规则

- 分段之和 ≠ 总耗时是正常现象。
- 若 `abs(unaccounted) > 5%`，UI 显示差异并解释。
- `estimated` 段使用斜纹 + 虚线；Tooltip 说明测量方法。
- 并行分段使用 lane；不把并行时间简单相加。

---

## 6. 用量模型

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `gen_ai.usage.input_tokens` | int | 输入 Token |
| `gen_ai.usage.output_tokens` | int | 输出 Token |
| `runesvaner.usage.cached_input_tokens` | int | 缓存命中输入 |
| `runesvaner.usage.reasoning_tokens` | int | 推理 Token（若与输出重叠，需标注） |
| `runesvaner.usage.image_tokens` | int | 图像 Token |
| `runesvaner.usage.audio_tokens` | int | 音频 Token |
| `runesvaner.usage.source` | enum | `provider` / `estimated` / `mixed` |
| `runesvaner.usage.estimated` | bool | 是否有估算成分 |

规则：

- 优先使用 provider 返回的 usage。
- 缺失时，若启用本地 Token 估算，标 `estimated=true` 与 `estimator` 版本。
- 不把 `reasoning_tokens` 与 `output_tokens` 重复相加；按 provider 语义记录。
- 缓存 Token 是输入的子集，不额外增加输入总量。

---

## 7. 成本模型

### 7.1 字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `runesvaner.cost.currency` | string | 默认 `USD` |
| `runesvaner.cost.input` | decimal | 非缓存输入成本 |
| `runesvaner.cost.cached_input` | decimal | 缓存输入成本 |
| `runesvaner.cost.output` | decimal | 输出成本 |
| `runesvaner.cost.reasoning` | decimal | 推理成本 |
| `runesvaner.cost.total` | decimal | 总计 |
| `runesvaner.cost.status` | enum | `known` / `partial` / `unknown` |
| `runesvaner.cost.price_table_version` | string | 价格表版本 |
| `runesvaner.cost.price_entry_id` | string | 命中的价格条目 |
| `runesvaner.cost.discounts` | map | 批量/缓存/其他折扣 |

### 7.2 计算

    non_cached_input = max(0, input_tokens - cached_input_tokens)
    input_cost = non_cached_input * input_price / 1_000_000
    cached_cost = cached_input_tokens * cached_price / 1_000_000
    output_cost = output_tokens * output_price / 1_000_000
    reasoning_cost = reasoning_tokens * reasoning_price / 1_000_000
    total = input_cost + cached_cost + output_cost + reasoning_cost

- 阶梯计价：按区间累加，不在区间边界重复。
- 批量折扣：在分项之后应用。
- 精度：`decimal`，存储 6 位小数；展示 4 位。
- 舍入：`MidpointRounding.ToEven`（待 D-08 确认）。
- 未知：`total = null`，`status=unknown`，不显示 0。

### 7.3 状态矩阵

| 情况 | status | total |
| --- | --- | --- |
| 模型已知、用量完整 | known | 数值 |
| 模型已知、部分用量缺失 | partial | 数值（缺失项标 null） |
| 模型未知 | unknown | null |
| 价格表缺失 | unknown | null |
| 重试计费不明 | known/partial + `billing.status=unknown` | 数值 + 警告 |

---

## 8. 流式模型

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `runesvaner.stream.ttft_us` | int64 | 首 Token |
| `runesvaner.stream.ttfb_us` | int64 | 首字节（若可得） |
| `runesvaner.stream.chunk_count` | int | chunk 数 |
| `runesvaner.stream.itl_p50_us` | int64 | Token 间延迟 P50 |
| `runesvaner.stream.itl_p95_us` | int64 | Token 间延迟 P95 |
| `runesvaner.stream.tokens_per_second` | double | 输出速率 |
| `runesvaner.stream.cancelled` | bool | 是否取消 |
| `runesvaner.stream.interrupted` | bool | 是否异常中断 |
| `runesvaner.stream.finish_reason` | string | provider 结束原因 |

规则：

- TTFT 只认首个**非空** delta。
- 空 delta（role-only、心跳）不计入。
- 若 provider 不提供逐 Token，只有 chunk，则 `tps` 按 chunk 估算并标 `estimated`。
- 取消与超时分开：`cancelled=true` + `error.type=timeout` 可同时存在。

---

## 9. 指标

Meter：`Runesvaner.AI`

| 指标 | 类型 | 单位 | 标签 |
| --- | --- | --- | --- |
| `runesvaner.ai.requests` | Counter | `{request}` | operation, provider, model, status |
| `runesvaner.ai.request.duration` | Histogram | `s` | operation, provider, model |
| `runesvaner.ai.ttft` | Histogram | `s` | operation, provider, model |
| `runesvaner.ai.tokens` | Counter | `{token}` | token.type (input/output/cached/reasoning), provider, model |
| `runesvaner.ai.cost` | Counter | `{usd}` | provider, model, cost.status |
| `runesvaner.ai.retries` | Counter | `{retry}` | provider, model, error.type |
| `runesvaner.ai.cache.hits` | Counter | `{hit}` | provider, model |
| `runesvaner.ai.vector.search.duration` | Histogram | `s` | collection, status |
| `runesvaner.ai.tool.duration` | Histogram | `s` | tool.name, status |
| `runesvaner.collector.queue.length` | ObservableGauge | `{item}` | — |
| `runesvaner.collector.dropped` | Counter | `{event}` | reason |
| `runesvaner.collector.flush.duration` | Histogram | `s` | store |

### 9.1 桶

请求耗时：`[0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60]`（与 GenAI 建议一致）。

TTFT：`[0.01, 0.025, 0.05, 0.1, 0.2, 0.4, 0.8, 1.6, 3.2, 6.4, 12.8]`。

### 9.2 高基数规则

禁止作为 Metric 标签：

- `request_id`、`trace_id`、`session_id`
- `user_id`、`tenant_id`（原文）
- `prompt_version`
- `error.message`
- 原始 URL、Prompt 文本

允许：`provider`、`model`、`operation`、`status`、`error.type`、`token.type`、`tool.name`（若基数可控）。

---

## 10. 与 OTel GenAI 的映射

| Runesvaner | OTel GenAI（Development） | 备注 |
| --- | --- | --- |
| `runesvaner.span.kind=chat` | `gen_ai.operation.name=chat` | Span 名称待 semconv 冻结 |
| `provider` | `gen_ai.provider.name` | 使用标准枚举 |
| `request_model` | `gen_ai.request.model` | 精确请求名 |
| `response_model` | `gen_ai.response.model` | 响应名 |
| `input_tokens` | `gen_ai.usage.input_tokens` | |
| `output_tokens` | `gen_ai.usage.output_tokens` | |
| `finish_reason` | `gen_ai.response.finish_reasons` | 数组 |
| `error.type` | `error.type` | 低基数 |
| `server.address` | `server.address` | 若可得 |
| `server.port` | `server.port` | 若可得 |
| `runesvaner.*` | 自定义 attributes | 不映射到标准名 |

风险：

- GenAI semconv 仍是 Development，字段可能改名。
- 映射表在 M1 冻结；每次升级 semconv 需兼容性审查。
- 不把 Runesvaner 扩展字段伪装成标准字段。

---

## 11. 事件格式

### 11.1 Envelope

    {
      "schema": "runesvaner.event.v1",
      "session_id": "01JQ8Z3K7M9X2V4N6P8R0T2W4Y",
      "event_id": "01JQ8Z3K7M9X2V4N6P8R0T2W4Z",
      "type": "request.end",
      "timestamp": "2026-11-25T14:30:12.3456789Z",
      "monotonic_ns": 1234567890123,
      "payload": { }
    }

### 11.2 事件类型

| `type` | 触发 | 关键 payload |
| --- | --- | --- |
| `session.start` | 会话开始 | app, environment, version, os |
| `request.start` | 请求开始 | operation, provider, model, tags |
| `segment` | 分段完成 | kind, offset, duration, estimated |
| `stream.first_byte` | 首字节 | offset |
| `stream.first_token` | 首 Token | offset, chunk_index |
| `stream.chunk` | 每 chunk | offset, length, index |
| `usage` | 用量确定 | input, output, cached, reasoning |
| `cost` | 成本计算 | breakdown, status, price_version |
| `retry` | 重试 | attempt, error, backoff |
| `error` | 错误 | type, message_hash, stack_hash |
| `request.end` | 请求结束 | status, total_duration, links |
| `self.monitor` | 周期采样 | queue, dropped, flush |
| `session.end` | 会话结束 | request_count, size |

### 11.3 完整示例（request.end）

    {
      "schema": "runesvaner.event.v1",
      "session_id": "01JQ8Z3K7M9X2V4N6P8R0T2W4Y",
      "event_id": "01JQ8Z3K7M9X2V4N6P8R0T2W50",
      "type": "request.end",
      "timestamp": "2026-11-25T14:30:20.8456789Z",
      "monotonic_ns": 8434567890123,
      "payload": {
        "request_id": "01JQ8Z3K7M9X2V4N6P8R0T2W4Z",
        "trace_id": "4bf92f3577b34da6a3ce929d0e0e4736",
        "span_id": "00f067aa0ba902b7",
        "operation": "chat",
        "provider": "openai",
        "request_model": "gpt-5",
        "response_model": "gpt-5-2026-08",
        "status": "ok",
        "duration_us": 8430000,
        "ttft_us": 2400000,
        "segments": [
          {"kind":"connect","start_offset_us":120000,"duration_us":80000,"estimated":false,"measurement":"http_diagnostic","lane":0},
          {"kind":"tls","start_offset_us":200000,"duration_us":150000,"estimated":false,"measurement":"http_diagnostic","lane":0},
          {"kind":"upload","start_offset_us":350000,"duration_us":220000,"estimated":false,"measurement":"stopwatch","lane":0},
          {"kind":"provider_queue","start_offset_us":570000,"duration_us":1900000,"estimated":true,"measurement":"derived","lane":0},
          {"kind":"ttft","start_offset_us":0,"duration_us":2400000,"estimated":false,"measurement":"stopwatch","lane":0},
          {"kind":"generation","start_offset_us":2400000,"duration_us":3100000,"estimated":false,"measurement":"stopwatch","lane":0},
          {"kind":"tool","start_offset_us":5500000,"duration_us":400000,"estimated":false,"measurement":"stopwatch","lane":1},
          {"kind":"serialize","start_offset_us":8100000,"duration_us":200000,"estimated":false,"measurement":"stopwatch","lane":0},
          {"kind":"unaccounted","start_offset_us":8300000,"duration_us":130000,"estimated":false,"measurement":"residual","lane":0}
        ],
        "usage": {
          "input_tokens": 12400,
          "output_tokens": 900,
          "cached_input_tokens": 8000,
          "reasoning_tokens": 600,
          "source": "provider"
        },
        "cost": {
          "currency": "USD",
          "input": 0.0055,
          "cached_input": 0.0010,
          "output": 0.0090,
          "reasoning": 0.0060,
          "total": 0.0215,
          "status": "known",
          "price_table_version": "2026-11-01"
        },
        "tags": {
          "runesvaner.feature": "support.answer",
          "runesvaner.tenant_hash": "7f3a9c",
          "runesvaner.prompt_version": "support-v12",
          "runesvaner.attempt": 2
        }
      }
    }

> 示例中的金额为示意，不代表真实价格。

---

## 12. 隐私分级

| 级别 | 字段示例 | 默认导出 |
| --- | --- | --- |
| `public` | model、provider、duration、token 数 | 是 |
| `internal` | feature、tenant_hash、prompt_version | 是（哈希） |
| `sensitive` | prompt、completion、user_id、原始 URL | 否 |
| `secret` | Authorization、api-key、cookie | 永不采集 |

规则：

- `sensitive` 字段仅 `ContentCapture.Full` 时记录。
- 导出前执行 `IContentRedactor`。
- 日志、异常消息、HTTP header 均不得包含 `secret`。

---

## 13. Schema 演进

| 变更 | 版本策略 |
| --- | --- |
| 新增可选字段 | 次版本（1.1） |
| 新增枚举值 | 次版本；消费者需容错 |
| 字段改名/删除 | 主版本（2.0）；提供迁移器 |
| 语义变更 | 主版本 + 文档 |
| 指标改名 | 主版本；双写一个周期 |

消费者规则：忽略未知字段；未知枚举值降级为 `unknown`。

---

## 14. 决议状态

D-01–D-25 已全部决议，见 `15-decision-log.md`。与本模型直接相关：

- D-07：库/应用内 SQLite 默认；CLI AOT 默认 JSONL。
- D-08：内置价格表 + 用户覆盖；decimal + ToEven；USD。
- D-13：OTel 为映射与可选导出，默认不创建额外 Activity。
- D-14：本地 100%，生产建议 10%；尾采样 M3。
- D-20：不承诺第三方语义兼容；OTel GenAI 为主。