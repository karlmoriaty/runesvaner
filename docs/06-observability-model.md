# Telemetry Data Model and Semantic Conventions

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Engineering, data, interoperability |
| Related documents | `05-technical-design.md`, `07-api-design.md` |
| Standards reference | OpenTelemetry GenAI Semantic Conventions (Development) |

---

## 1. Design Principles

1. **Align with standards**: fields that can be mapped to `gen_ai.*` use the standard name first.
2. **Extension isolation**: Runesvaner-specific fields uniformly use the `runesvaner.*` prefix and do not pollute the standard namespace.
3. **Low-cardinality metrics**: metric labels do not include high-cardinality values such as request_id, user_id, or prompt_version.
4. **High-cardinality traces**: high-dimensional information lives in span attributes / events and is correlated via trace_id.
5. **Do not guess**: missing usage, price, or time is always marked `unknown` or `estimated`.
6. **Schema versioning**: every event carries a `schema` field; breaking changes bump the major version.
7. **Privacy classification**: fields are labeled `public` / `internal` / `sensitive` and can be filtered by level before export.

---

## 2. Entity Model

    RunesvanerSession
      ├── RunesvanerRequest (1..N)
      │     ├── RunesvanerSegment (0..N)
      │     ├── RunesvanerUsage (0..1)
      │     ├── RunesvanerCost (0..1)
      │     ├── RunesvanerStream (0..1)
      │     ├── RunesvanerRetry (0..N)
      │     └── RunesvanerError (0..N)
      └── SelfMonitorSample (0..N)

| Entity | Description |
| --- | --- |
| Session | One process run or one diagnostic session |
| Request | One logical AI operation (chat / embeddings / tool / vector) |
| Segment | A time interval within a request |
| Usage | Token and multimodal usage |
| Cost | Itemized and total cost |
| Stream | Streaming behavior metrics |
| Retry | One attempt |
| Error | Exception or provider error |

---

## 3. Identifiers and Correlation

| Field | Type | Description |
| --- | --- | --- |
| `runesvaner.session_id` | string (ULID) | Session identifier |
| `runesvaner.request_id` | string (ULID) | Request identifier |
| `trace_id` | string (W3C 32 hex) | Aligned with OTel |
| `span_id` | string (W3C 16 hex) | Aligned with OTel |
| `runesvaner.parent_request_id` | string | Agent/tool nesting |
| `runesvaner.attempt` | int | Starts at 1 |
| `runesvaner.schema` | string | For example `runesvaner.event.v1` |

Rationale for ULID: time-sortable, string-safe, and requires no database sequence.

---

## 4. Span Types

| `runesvaner.span.kind` | Corresponding OTel operation | Description |
| --- | --- | --- |
| `request` | — | Runesvaner request root |
| `chat` | `gen_ai.operation.name=chat` | Chat inference |
| `embeddings` | `embeddings` | Embedding |
| `tool` | `execute_tool` | Tool/function call |
| `vector_search` | `retrieval` | Vector search |
| `rerank` | `retrieval` | Reranking |
| `serialize` | — | Serialization |
| `queue` | — | In-app queueing |
| `retry` | — | Retry wait |
| `cache` | — | Cache lookup/write |

> OTel GenAI span naming and operation names are still in Development; M1 freezes the mapping table, see Section 10.

---

## 5. Latency Segmentation Model

### 5.1 Segment Fields

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `kind` | enum | Yes | See Section 5.2 |
| `start_offset_us` | int64 | Yes | Relative to request start |
| `duration_us` | int64 | Yes | Duration |
| `estimated` | bool | Yes | Whether it is an estimate |
| `measurement` | enum | Yes | `stopwatch` / `http_diagnostic` / `derived` / `residual` |
| `lane` | int | No | Parallel grouping, default 0 |
| `status` | enum | No | `ok` / `error` / `cancelled` |
| `attributes` | map | No | Low-cardinality supplementary information |

### 5.2 Segment Kinds

| `kind` | Start | End | Default measurement |
| --- | --- | --- | --- |
| `queue` | Middleware entry | Start of inner call | stopwatch |
| `auth` | Auth start | Auth end | stopwatch |
| `connect` | Connection start | Connection established | http_diagnostic |
| `tls` | TLS start | TLS complete | http_diagnostic |
| `upload` | Request write start | Request write complete | stopwatch |
| `provider_queue` | Upload complete | First byte | derived (estimated) |
| `ttft` | Request start | First non-empty delta | stopwatch |
| `generation` | First delta | Last delta | stopwatch |
| `tool` | Tool start | Tool end | stopwatch |
| `vector_search` | Retrieval start | Retrieval end | stopwatch |
| `serialize` | Serialization start | Serialization end | stopwatch |
| `retry_wait` | Failure | Next attempt | stopwatch |
| `cache_lookup` | Cache query start | Return | stopwatch |
| `unaccounted` | Residual | — | residual |
| `overlap` | Parallel overlap | — | residual |

### 5.3 Formulas

- `ttfb = first_byte_ts - request_start_ts`
- `ttft = first_non_empty_delta_ts - request_start_ts`
- `generation = last_delta_ts - first_non_empty_delta_ts`
- `provider_queue = ttfb - upload_complete_ts` (includes network RTT, estimated)
- `itl_i = delta_ts_i - delta_ts_{i-1}`
- `tps = output_tokens / generation_seconds`
- `overlap = sum(parallel durations) - union(parallel intervals)`
- `unaccounted = total_duration - union(all non-residual intervals)`

### 5.4 Display Rules

- The sum of segments ≠ total elapsed time is normal.
- If `abs(unaccounted) > 5%`, the UI shows and explains the discrepancy.
- `estimated` segments use diagonal hatching + dashed lines; the tooltip explains the measurement method.
- Parallel segments use lanes; do not simply add parallel time together.

---

## 6. Usage Model

| Field | Type | Description |
| --- | --- | --- |
| `gen_ai.usage.input_tokens` | int | Input tokens |
| `gen_ai.usage.output_tokens` | int | Output tokens |
| `runesvaner.usage.cached_input_tokens` | int | Cached input |
| `runesvaner.usage.reasoning_tokens` | int | Reasoning tokens (must be labeled if overlapping with output) |
| `runesvaner.usage.image_tokens` | int | Image tokens |
| `runesvaner.usage.audio_tokens` | int | Audio tokens |
| `runesvaner.usage.source` | enum | `provider` / `estimated` / `mixed` |
| `runesvaner.usage.estimated` | bool | Whether any part is estimated |

Rules:

- Prefer usage returned by the provider.
- When missing, if local token estimation is enabled, mark `estimated=true` and the `estimator` version.
- Do not add `reasoning_tokens` and `output_tokens` together; record them according to provider semantics.
- Cached tokens are a subset of input and do not additionally increase total input.

---

## 7. Cost Model

### 7.1 Fields

| Field | Type | Description |
| --- | --- | --- |
| `runesvaner.cost.currency` | string | Default `USD` |
| `runesvaner.cost.input` | decimal | Non-cached input cost |
| `runesvaner.cost.cached_input` | decimal | Cached input cost |
| `runesvaner.cost.output` | decimal | Output cost |
| `runesvaner.cost.reasoning` | decimal | Reasoning cost |
| `runesvaner.cost.total` | decimal | Total |
| `runesvaner.cost.status` | enum | `known` / `partial` / `unknown` |
| `runesvaner.cost.price_table_version` | string | Price table version |
| `runesvaner.cost.price_entry_id` | string | Matched price entry |
| `runesvaner.cost.discounts` | map | Batch/cache/other discounts |

### 7.2 Calculation

    non_cached_input = max(0, input_tokens - cached_input_tokens)
    input_cost = non_cached_input * input_price / 1_000_000
    cached_cost = cached_input_tokens * cached_price / 1_000_000
    output_cost = output_tokens * output_price / 1_000_000
    reasoning_cost = reasoning_tokens * reasoning_price / 1_000_000
    total = input_cost + cached_cost + output_cost + reasoning_cost

- Tiered pricing: accumulate by interval; do not repeat at interval boundaries.
- Batch discount: applied after itemization.
- Precision: `decimal`, stored with 6 decimal places; displayed with 4.
- Rounding: `MidpointRounding.ToEven` (pending confirmation in D-08).
- Unknown: `total = null`, `status=unknown`; do not display 0.

### 7.3 Status Matrix

| Situation | status | total |
| --- | --- | --- |
| Model known, usage complete | known | numeric |
| Model known, some usage missing | partial | numeric (missing items marked null) |
| Model unknown | unknown | null |
| Price table missing | unknown | null |
| Retry billing unclear | known/partial + `billing.status=unknown` | numeric + warning |

---

## 8. Streaming Model

| Field | Type | Description |
| --- | --- | --- |
| `runesvaner.stream.ttft_us` | int64 | First token |
| `runesvaner.stream.ttfb_us` | int64 | First byte (if available) |
| `runesvaner.stream.chunk_count` | int | Number of chunks |
| `runesvaner.stream.itl_p50_us` | int64 | Inter-token latency P50 |
| `runesvaner.stream.itl_p95_us` | int64 | Inter-token latency P95 |
| `runesvaner.stream.tokens_per_second` | double | Output rate |
| `runesvaner.stream.cancelled` | bool | Whether cancelled |
| `runesvaner.stream.interrupted` | bool | Whether abnormally interrupted |
| `runesvaner.stream.finish_reason` | string | Provider finish reason |

Rules:

- TTFT only counts the first **non-empty** delta.
- Empty deltas (role-only, heartbeat) do not count.
- If the provider does not supply per-token output and only chunks, `tps` is estimated by chunk and marked `estimated`.
- Cancellation and timeout are separate: `cancelled=true` + `error.type=timeout` can coexist.

---

## 9. Metrics

Meter: `Runesvaner.AI`

| Metric | Type | Unit | Labels |
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

### 9.1 Buckets

Request duration: `[0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60]` (consistent with GenAI recommendations).

TTFT: `[0.01, 0.025, 0.05, 0.1, 0.2, 0.4, 0.8, 1.6, 3.2, 6.4, 12.8]`.

### 9.2 High-Cardinality Rules

Prohibited as metric labels:

- `request_id`, `trace_id`, `session_id`
- `user_id`, `tenant_id` (raw values)
- `prompt_version`
- `error.message`
- Raw URLs, prompt text

Allowed: `provider`, `model`, `operation`, `status`, `error.type`, `token.type`, `tool.name` (if cardinality is controllable).

---

## 10. Mapping to OTel GenAI

| Runesvaner | OTel GenAI (Development) | Notes |
| --- | --- | --- |
| `runesvaner.span.kind=chat` | `gen_ai.operation.name=chat` | Span name pending semconv freeze |
| `provider` | `gen_ai.provider.name` | Use standard enums |
| `request_model` | `gen_ai.request.model` | Exact request name |
| `response_model` | `gen_ai.response.model` | Response name |
| `input_tokens` | `gen_ai.usage.input_tokens` | |
| `output_tokens` | `gen_ai.usage.output_tokens` | |
| `finish_reason` | `gen_ai.response.finish_reasons` | Array |
| `error.type` | `error.type` | Low cardinality |
| `server.address` | `server.address` | If available |
| `server.port` | `server.port` | If available |
| `runesvaner.*` | Custom attributes | Not mapped to standard names |

Risks:

- GenAI semconv is still Development; fields may be renamed.
- The mapping table is frozen at M1; every semconv upgrade requires a compatibility review.
- Do not disguise Runesvaner extension fields as standard fields.

---

## 11. Event Format

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

### 11.2 Event Types

| `type` | Trigger | Key payload |
| --- | --- | --- |
| `session.start` | Session start | app, environment, version, os |
| `request.start` | Request start | operation, provider, model, tags |
| `segment` | Segment complete | kind, offset, duration, estimated |
| `stream.first_byte` | First byte | offset |
| `stream.first_token` | First token | offset, chunk_index |
| `stream.chunk` | Each chunk | offset, length, index |
| `usage` | Usage determined | input, output, cached, reasoning |
| `cost` | Cost calculated | breakdown, status, price_version |
| `retry` | Retry | attempt, error, backoff |
| `error` | Error | type, message_hash, stack_hash |
| `request.end` | Request end | status, total_duration, links |
| `self.monitor` | Periodic sampling | queue, dropped, flush |
| `session.end` | Session end | request_count, size |

### 11.3 Full Example (request.end)

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

> Amounts in the example are illustrative and do not represent real prices.

---

## 12. Privacy Classification

| Level | Example fields | Exported by default |
| --- | --- | --- |
| `public` | model, provider, duration, token count | Yes |
| `internal` | feature, tenant_hash, prompt_version | Yes (hashed) |
| `sensitive` | prompt, completion, user_id, raw URL | No |
| `secret` | Authorization, api-key, cookie | Never collected |

Rules:

- `sensitive` fields are recorded only when `ContentCapture.Full`.
- Run `IContentRedactor` before export.
- Logs, exception messages, and HTTP headers must never contain `secret`.

---

## 13. Schema Evolution

| Change | Version policy |
| --- | --- |
| Adding an optional field | Minor version (1.1) |
| Adding an enum value | Minor version; consumers must tolerate it |
| Renaming/deleting a field | Major version (2.0); provide a migrator |
| Semantic change | Major version + documentation |
| Renaming a metric | Major version; dual-write for one cycle |

Consumer rule: ignore unknown fields; degrade unknown enum values to `unknown`.

---

## 14. Decision Status

D-01–D-25 are all resolved; see `15-decision-log.md`. Directly related to this model:

- D-07: SQLite default for libraries/in-app; JSONL default for CLI AOT.
- D-08: built-in price table + user overrides; decimal + ToEven; USD.
- D-13: OTel is for mapping and optional export; no additional Activities are created by default.
- D-14: 100% locally, 10% recommended in production; tail sampling in M3.
- D-20: no commitment to third-party semantic compatibility; OTel GenAI is primary.