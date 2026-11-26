# Hidden Requirements Checklist

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Product, engineering, design, testing |
| Related documents | `03-prd.md`, `05-technical-design.md`, `06-observability-model.md`, `09-security-privacy.md` |

> These requirements usually do not appear in user stories, but when they are missing, the product "looks usable while actually being untrustworthy". Each entry gives the hidden rationale and an acceptance suggestion.

---

## A. Measurement and Statistical Credibility

### H-01 Monotonic clock separated from wall clock

- **Requirement**: all durations use a monotonic clock; displayed times use the wall clock.
- **Hidden reason**: NTP adjustment, daylight saving time, and manual time changes can make wall-clock differences negative or jump.
- **Consequence if missing**: negative durations, P95 jitter, and cross-day statistical errors.
- **Acceptance**: a unit test injects a backwards clock and durations never become negative; JSON records both `monotonic_ns` and UTC time.

### H-02 Clock granularity and platform differences

- **Requirement**: documentation states the timing granularity on different OSes; do not over-promise on microsecond-level differences.
- **Hidden reason**: the default timer granularity on Windows may be 15.6 ms; Linux is usually better.
- **Consequence if missing**: segments of short requests show 0 or jitter, and users believe the data is fabricated.
- **Acceptance**: the benchmark report notes platform and granularity; segments smaller than the granularity are marked `estimated` or merged.

### H-03 Network time cannot be precisely separated from provider processing time

- **Requirement**: `provider_queue` explicitly states that it includes network RTT and is an estimate.
- **Hidden reason**: the client can only observe "upload complete" and "first byte"; it cannot know when the server started processing.
- **Consequence if missing**: users misjudge network latency as a slow model and optimize in the wrong direction.
- **Acceptance**: UI tooltip and documentation; provide `estimated=true`; do not give a "precise provider queue value".

### H-04 TTFT definition ambiguity

- **Requirement**: distinguish TTFB, TTFT, and first chunk at the same time; TTFT counts only the first non-empty delta.
- **Hidden reason**: different providers send role-only chunks, heartbeats, or empty deltas first.
- **Consequence if missing**: TTFT is too small and the user-experience metric is distorted.
- **Acceptance**: streaming tests cover empty chunks; report fields `ttfb_us` and `ttft_us` are separate.

### H-05 Parallel segments and overlap

- **Requirement**: support lanes; display `overlap`; do not simply add parallel time.
- **Hidden reason**: parallel tool calls, concurrent retrieval, and streaming generation alongside tool calls may overlap.
- **Consequence if missing**: the sum of segments far exceeds total duration and users no longer trust the waterfall.
- **Acceptance**: construct parallel requests and validate the `overlap` computation; multi-lane UI display.

### H-06 Residual and unexplained time

- **Requirement**: display `unaccounted` and explain it when it exceeds 5%.
- **Hidden reason**: scheduling, GC, serialization, and clock granularity all leave residual time.
- **Consequence if missing**: segment percentages do not sum to 100% and users think the calculation is wrong.
- **Acceptance**: the residual segment is displayed separately; provide drill-down to "raw events".

### H-07 Sampling bias and weighting

- **Requirement**: record the sampling probability; weight during aggregation; the UI shows the sampling mode.
- **Hidden reason**: head sampling drops records randomly; tail sampling biases toward slow/error requests.
- **Consequence if missing**: cost and latency statistics are systematically low or high.
- **Acceptance**: given 10% sampling and a known total, the weighted result error is ≤ 2%; tail sampling is counted separately.

### H-08 High-cardinality labels

- **Requirement**: metric labels do not contain request_id, user_id, or prompt_version; high cardinality goes to traces.
- **Hidden reason**: metric backends store data by label combination; a cardinality explosion causes cost and query failures.
- **Consequence if missing**: Prometheus memory spikes, higher bills, query timeouts.
- **Acceptance**: metric label whitelist test; code review checklist.

### H-09 Clock drift and cross-service merging

- **Requirement**: use OTel timestamps when merging across services, and note the clock error risk.
- **Hidden reason**: clocks on different machines are not synchronized, and parent/child span times may be inverted.
- **Consequence if missing**: negative durations appear in the distributed waterfall.
- **Acceptance**: merge logic is fault-tolerant; documented; v1 does not perform automatic correction.

---

## B. Cost and Billing

### H-10 Cached tokens are a subset of input

- **Requirement**: `cached_input_tokens` does not add to the input total; split it out of the input during calculation.
- **Hidden reason**: provider usage semantics differ; some count cache in input, others list it separately.
- **Consequence if missing**: double billing or under-billing.
- **Acceptance**: multi-provider fixtures; validate `non_cached = input - cached`.

### H-11 Overlap semantics of reasoning tokens and output tokens

- **Requirement**: record according to the provider definition; do not add by default; mark whether they are included in output.
- **Hidden reason**: models differ in how reasoning tokens are billed and disclosed.
- **Consequence if missing**: cost overestimated or underestimated.
- **Acceptance**: price entries include a `reasoning_included_in_output` flag; unit tests.

### H-12 Cumulative intervals in tiered pricing

- **Requirement**: accumulate by interval without duplication at boundaries; support both the "everything above the threshold is charged at the new price" and the "progressive by segment" modes.
- **Hidden reason**: provider tier rules are inconsistent.
- **Consequence if missing**: wrong costs for large requests.
- **Acceptance**: boundary value tests (threshold -1, threshold, threshold +1); mode field.

### H-13 Price table version and historical replay

- **Requirement**: each request records the price table version; historical sessions are replayed with the version in effect at the time.
- **Hidden reason**: prices change, and bills need to be explainable.
- **Consequence if missing**: old data is recomputed under new prices and cannot be reconciled.
- **Acceptance**: after switching the price table, old session costs are unchanged; the UI shows the version.

### H-14 Do not guess unknown models

- **Requirement**: `CostStatus.Unknown`, total cost is null, and 0 is not displayed.
- **Hidden reason**: guessing creates a false impression of "cheap".
- **Consequence if missing**: users make decisions based on wrong costs.
- **Acceptance**: unknown model test; the UI shows `—` and the reason.

### H-15 Decimal precision and rounding

- **Requirement**: `decimal` arithmetic; store 6 digits; display 4 digits; unified rounding rule.
- **Hidden reason**: `double` accumulates error; different rounding causes reconciliation differences.
- **Consequence if missing**: amounts differ from the bill by a few cents and users lose trust.
- **Acceptance**: property tests on monetary amounts; rounding rules documented.

### H-16 Multi-currency and exchange rates

- **Requirement**: store USD; display optionally converted values; exchange rate timestamp; no network access by default.
- **Hidden reason**: enterprise bills may be multi-currency; exchange rates change daily.
- **Consequence if missing**: conversion errors and privacy leaks (silent network access).
- **Acceptance**: exchange rate optional and offline by default; show "estimated exchange rate".

### H-17 Batch API discounts

- **Requirement**: support batch discount; make the stacking order with cache discounts explicit.
- **Hidden reason**: the Batch API usually offers a 50% discount, which is easy to miss.
- **Consequence if missing**: cost overestimated by a factor of two.
- **Acceptance**: price entries include `batch_discount`; combination tests.

### H-18 Multimodal token units

- **Requirement**: record images/audio in provider units (images, seconds, tokens); mark unknown when they cannot be unified.
- **Hidden reason**: providers use different billing units.
- **Consequence if missing**: wrong multimodal costs.
- **Acceptance**: fields `image_tokens`, `audio_tokens`, `unit`; unknown when not determinable.

### H-19 Retries and failed billing

- **Requirement**: record attempt; distinguish billed/not_billed/unknown; detect potential double billing.
- **Hidden reason**: 429/5xx may or may not be billed; a timeout may have already produced output.
- **Consequence if missing**: the bill does not match the request count.
- **Acceptance**: retry scenario tests; hints in the UI exception area.

### H-20 Budget alerts do not block requests

- **Requirement**: exceeding the budget only records an event and a hint; it does not throw and does not reject the request.
- **Hidden reason**: a diagnostic tool should not change business behavior.
- **Consequence if missing**: production incident.
- **Acceptance**: over-budget test; business return values unchanged.

---

## C. Streaming and Concurrency

### H-21 Streaming wrapper does not read ahead

- **Requirement**: pass through chunk by chunk; do not buffer the whole stream.
- **Hidden reason**: read-ahead changes latency, memory, and backpressure.
- **Consequence if missing**: worse TTFT and memory spikes.
- **Acceptance**: slow consumer test; memory ceiling test.

### H-22 Cancellation separated from timeout

- **Requirement**: record `cancelled` and `timeout` separately; keep the chunks already received.
- **Hidden reason**: both manifest as `OperationCanceledException`.
- **Consequence if missing**: cannot distinguish user behavior from system failure.
- **Acceptance**: cancellation/timeout tests; independent fields.

### H-23 Exception propagation unchanged

- **Requirement**: record and then rethrow unchanged; do not wrap the exception type.
- **Hidden reason**: business code may rely on specific exception types for retries.
- **Consequence if missing**: retry policy fails.
- **Acceptance**: exception type and stack trace tests.

### H-24 Multithreading and AsyncLocal

- **Requirement**: label scope propagates across `await`; parallel tasks do not interfere.
- **Hidden reason**: `AsyncLocal` is copied across parallel branches.
- **Consequence if missing**: misattributed labels and wrong cost attribution.
- **Acceptance**: `Task.WhenAll` test; nested scope test.

### H-25 Concurrent writes and backpressure

- **Requirement**: single writer; drop the oldest when the queue is full; count drops.
- **Hidden reason**: multiple threads writing directly to a file will interleave.
- **Consequence if missing**: corrupted JSONL and blocked requests.
- **Acceptance**: 1000 concurrent request test; queue-full test.

---

## D. Privacy and Compliance

### H-26 No content by default

- **Requirement**: `ContentCapture.None` by default; documentation states this explicitly.
- **Hidden reason**: developers may assume by default that "a diagnostic tool records content".
- **Consequence if missing**: compliance incident.
- **Acceptance**: scan the default configuration's on-disk output; no Prompt/Completion fields.

### H-27 Content capture limit and truncation

- **Requirement**: `Full` mode has a byte limit; truncation is marked; nothing is silently dropped.
- **Hidden reason**: a large prompt may be tens of MB.
- **Consequence if missing**: disk exhaustion and UI freezes.
- **Acceptance**: oversized content test; `truncated=true`.

### H-28 Hash salt and linkability

- **Requirement**: add a process-level salt by default; documentation states "linkable, not anonymous".
- **Hidden reason**: hashes without a salt can be reversed with rainbow tables.
- **Consequence if missing**: false anonymity.
- **Acceptance**: the salt is not exported; hashing tests.

### H-29 Local UI token not leaked

- **Requirement**: the URL token is not written to shell history; support one-time opening.
- **Hidden reason**: a URL printed by the CLI is easily copied into chats or logs.
- **Consequence if missing**: local data accessed by others.
- **Acceptance**: CLI `open` launches the browser directly; documentation discourages copying the URL.

### H-30 DNS rebinding and Host validation

- **Requirement**: validate Host/Origin; disable CORS; CSP.
- **Hidden reason**: a malicious web page can resolve a domain to 127.0.0.1.
- **Consequence if missing**: local data is read.
- **Acceptance**: security tests; fake Host requests return 400.

### H-31 File permissions and symbolic links

- **Requirement**: 0600/0700; reject writes through symlinks; path whitelist.
- **Hidden reason**: multi-user machines and shared directories.
- **Consequence if missing**: data leak or arbitrary file write.
- **Acceptance**: permission tests; symlink tests.

### H-32 Export redaction

- **Requirement**: run the redactor before export; sensitive data is not exported by default.
- **Hidden reason**: users may forget to configure it.
- **Consequence if missing**: content leaks to third parties.
- **Acceptance**: scan export fixtures; sensitive fields are absent.

### H-33 Legitimacy of price table sources

- **Requirement**: record the source URL, retrieval time, and checksum; do not bundle data of unknown origin.
- **Hidden reason**: price data may be subject to terms of use.
- **Consequence if missing**: legal risk.
- **Acceptance**: complete price table metadata; NOTICE statement.

---

## E. AOT and Packaging

### H-34 JSON source generation

- **Requirement**: all serialization uses `JsonSerializerContext`.
- **Hidden reason**: reflection-based serialization fails after trimming.
- **Consequence if missing**: `NotSupportedException` at AOT runtime.
- **Acceptance**: AOT smoke; zero IL2026 warnings.

### H-35 Configuration source-generated binding

- **Requirement**: JSON configuration uses a source-generated binder; code configuration is preferred.
- **Hidden reason**: `IConfiguration.Get<T>()` depends on reflection.
- **Consequence if missing**: configuration is empty or throws under AOT.
- **Acceptance**: AOT configuration test.

### H-36 SQLite native dependency

- **Requirement**: SQLite by default in the library/application; JSONL by default in CLI AOT; AOT must validate `e_sqlite3`; fall back on failure.
- **Hidden reason**: `Microsoft.Data.Sqlite` requires `e_sqlite3`; `dotnet/efcore#36068` is still open, and the native library may not be found under AOT.
- **Consequence if missing**: the default storage is unavailable, or the AOT publish lacks the native library.
- **Acceptance**: library SQLite smoke; CLI JSONL smoke; record the SQLite AOT attempt; fallback test; the Health page shows the fallback reason.

### H-37 OTel double Spans

- **Requirement**: do not create additional Activities by default; create them only in the explicit mode.
- **Hidden reason**: MEAI already includes `UseOpenTelemetry`; duplication produces two sets of Spans.
- **Consequence if missing**: backend data doubles and cost increases.
- **Acceptance**: test with both enabled; Span count assertion.

### H-38 Package size

- **Requirement**: size budget for the core library; separate optional packages.
- **Hidden reason**: AOT users are sensitive to size.
- **Consequence if missing**: users remove the dependency.
- **Acceptance**: size baseline; CI records it.

### H-39 Target framework and MEAI version

- **Requirement**: an explicit support matrix; no commitments for frameworks that are already EOL.
- **Hidden reason**: .NET 8 support ended in 2026-11; multi-targeting slows CI.
- **Consequence if missing**: maintenance burden and CI explosion.
- **Acceptance**: support matrix documentation; contract tests.

---

## F. Developer Experience

### H-40 Actionable empty state

- **Requirement**: the empty state gives commands, documentation links, and next steps.
- **Hidden reason**: users are most easily discouraged the first time they open Studio.
- **Consequence if missing**: uninstall.
- **Acceptance**: empty-data E2E; copy review.

### H-41 Actionable error messages

- **Requirement**: errors include the reason, location, and next step.
- **Hidden reason**: when the diagnostic tool itself fails, users have no way forward.
- **Consequence if missing**: more issues and declining trust.
- **Acceptance**: error message inventory; every entry has a next step.

### H-42 Five-minute first experience

- **Requirement**: install → run the sample → see the waterfall in ≤ 5 minutes.
- **Hidden reason**: competitor platforms have high deployment cost; this is the differentiator.
- **Consequence if missing**: low trial conversion.
- **Acceptance**: timed test; ≥ 80% completion.

### H-43 Keyboard and accessibility

- **Requirement**: main paths are keyboard accessible; color is not the sole encoding; contrast.
- **Hidden reason**: engineering users prefer the keyboard; color-blind users need texture.
- **Consequence if missing**: poor usability and poor reputation.
- **Acceptance**: keyboard E2E; axe scan.

### H-44 Documentation in the same PR as code

- **Requirement**: public API/behavior changes must update the documentation.
- **Hidden reason**: stale documentation is a common failure point in open-source projects.
- **Consequence if missing**: users fail to integrate according to old documentation.
- **Acceptance**: PR template checklist item; documentation link test.

### H-45 One-command local build

- **Requirement**: `build.cmd` / `build.sh` or just `dotnet build`; devcontainer.
- **Hidden reason**: contributors are lost when their first build fails.
- **Consequence if missing**: few external contributions.
- **Acceptance**: build on a clean machine; contributor feedback.

### H-46 Test data contains no real user data

- **Requirement**: all fixtures are synthetic or redacted; documentation warns about this.
- **Hidden reason**: an open-source repository publishes its test data.
- **Consequence if missing**: privacy incident.
- **Acceptance**: repository scan; fixture review.

---

## G. Operations and Failure

### H-47 Collector self-monitoring

- **Requirement**: queue, drops, disk write latency, circuit breaking state.
- **Hidden reason**: the diagnostic tool itself can also fail.
- **Consequence if missing**: data is missing and nobody knows.
- **Acceptance**: Health page; metrics tests.

### H-48 Crash safety

- **Requirement**: a half-written JSONL line can be ignored; startup can recover.
- **Hidden reason**: process crashes and container kills.
- **Consequence if missing**: the entire session is unreadable.
- **Acceptance**: crash injection test.

### H-49 Disk full and retention

- **Requirement**: dual thresholds; clean up the oldest; protect active sessions.
- **Hidden reason**: long-running processes fill the disk.
- **Consequence if missing**: the business is affected.
- **Acceptance**: disk-full simulation; cleanup test.

### H-50 Graceful shutdown

- **Requirement**: `FlushAsync`; a wait upper bound at host shutdown.
- **Hidden reason**: containers exit immediately after SIGTERM.
- **Consequence if missing**: the last requests are lost.
- **Acceptance**: shutdown test; flush timeout.

---

## H. Ecosystem and Standards

### H-51 Evolution of GenAI semantic conventions

- **Requirement**: version the mapping table; review on upgrade; do not treat Development fields as stable.
- **Hidden reason**: semconv is still changing rapidly.
- **Consequence if missing**: exported fields break.
- **Acceptance**: contract tests; changelog.

### H-52 Third-party platform compatibility

- **Requirement**: Langfuse/Phoenix validated via OTLP; do not promise all platforms.
- **Hidden reason**: platform field mapping differs.
- **Consequence if missing**: users export data and then cannot see it.
- **Acceptance**: local collector + platform compatibility test.

### H-53 Schema evolution

- **Requirement**: add optional fields; major version breaks require a migrator; ignore unknown fields.
- **Hidden reason**: consumers lag behind.
- **Consequence if missing**: old CLIs cannot read new data.
- **Acceptance**: cross-version read test.

### H-54 Importing old data

- **Requirement**: JSONL/OTLP import; version detection; error line reporting.
- **Hidden reason**: users may collect first and analyze later.
- **Consequence if missing**: data silos.
- **Acceptance**: import tests; error statistics.

---

## I. Community and Governance

### H-55 CLA enforceable and not intimidating

- **Requirement**: automated comment, clear explanation, contributor copyright retained.
- **Hidden reason**: a CLA is often perceived as "taking copyright".
- **Consequence if missing**: contributors leave.
- **Acceptance**: CLA text review; first-time contributor feedback.

### H-56 Succession plan

- **Requirement**: at least 2 releasable maintainers; rights transfer clause.
- **Hidden reason**: a personal project has a bus factor of 1.
- **Consequence if missing**: the project stalls.
- **Acceptance**: governance documentation; permission check.

### H-57 Trademark and fork policy

- **Requirement**: allow factual use; prohibit confusion; forks must rename.
- **Hidden reason**: impersonation and commercial forks appear as the project grows.
- **Consequence if missing**: brand disputes.
- **Acceptance**: TRADEMARK.md.

### H-58 Version support policy

- **Requirement**: support only LTS + latest STS; explicit EOL.
- **Hidden reason**: unlimited support would crush a personal project.
- **Consequence if missing**: CI and issues explode.
- **Acceptance**: support matrix; EOL announcement.

### H-59 Community maintenance of the price table

- **Requirement**: issue template, validation script, source requirements, review process.
- **Hidden reason**: prices change frequently and one person cannot keep up.
- **Consequence if missing**: stale prices.
- **Acceptance**: price PR process; validation CI.

### H-60 Multilingual documentation

- **Requirement**: English is complete; Chinese/German follow the L1 full translation, L2 condensed version, and L3 redirect defined in `18-localization-scope.md`; the UI is complete in all three languages.
- **Hidden reason**: the target users include Chinese and German developers; full translation is costly, but security/privacy/license content cannot be cut.
- **Consequence if missing**: non-English users misread the product, or critical security information is missing.
- **Acceptance**: L1 is navigable in all three languages; L2 links to the full English text; L3 has a redirect; security/privacy/license content is 100% synchronized.

---

## Priority Summary

| Priority | Items |
| --- | --- |
| P0 (required before M1) | H-01, H-04, H-07, H-10, H-14, H-21, H-23, H-26, H-34, H-35, H-36, H-37, H-40, H-47, H-48 |
| P1 (required before M2) | H-03, H-05, H-06, H-11, H-12, H-13, H-15, H-19, H-22, H-25, H-27, H-28, H-30, H-31, H-32, H-41, H-42, H-49, H-50 |
| P2 (before 1.0) | H-02, H-08, H-09, H-16, H-17, H-18, H-20, H-24, H-29, H-33, H-38, H-39, H-43, H-44, H-45, H-46, H-51, H-52, H-53, H-54, H-55, H-56, H-57, H-58, H-59, H-60 |

---

## Decision Status

D-01–D-25 are all decided; see `15-decision-log.md`. Directly relevant to this checklist:

- D-07: SQLite by default in the library; JSONL by default in CLI AOT; fallback on failure (H-36 promoted to P0).
- D-08: built-in price table + user overrides; decimal + ToEven; USD.
- D-09: `None` by default.
- D-13: do not create additional Activities by default.
- D-14: 100% locally, 10% recommended in production.
- D-15: Windows/Linux.
- D-16: English/Chinese/German.
- D-17: `net10.0`; MEAI 10.x/11.x.
- D-18: AOT-specific scope.