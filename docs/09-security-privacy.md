# Security and Privacy Design

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Engineering, Security, Compliance |
| Related documents | `05-technical-design.md`, `06-observability-model.md`, `10-open-source-governance.md` |

---

## 1. Security Goals

1. **Do not leak content**: Do not collect Prompt / Completion by default; do not record secrets.
2. **Do not expand the attack surface**: Local Studio binds to loopback only, requires a token, and uses no external resources.
3. **Do not break the business**: A failure in the collection pipeline does not affect requests.
4. **Auditable**: Users can check where data is, how long it is retained, and where it is exported.
5. **Trustworthy supply chain**: Minimal dependencies, signing, SBOM, vulnerability response.

---

## 2. Assets and Trust Boundaries

### 2.1 Assets

| Asset | Sensitivity | Location |
| --- | --- | --- |
| Prompt / Completion | High | Process memory, optional local files |
| API Key / Token | Very high | Process memory, environment variables |
| Tenant / user identifiers | High | Local files, export |
| Cost and usage | Medium | Local files, export |
| Price table | Low | Local files, in-package |
| Local Studio token | High | Process memory, temporary files |
| Diagnostic session files | Medium-high | User directory |

### 2.2 Trust boundaries

    [Business process]
      ├── Runesvaner collection (same process, trusted)
      ├── Local file system (user permissions, semi-trusted)
      ├── Studio HTTP (loopback, authentication required)
      └── OTLP export (external network, untrusted)

| Boundary | Threat |
| --- | --- |
| In-process | Malicious libraries, reflection abuse, memory dumps |
| File system | Access by other local users, symbolic links, path traversal |
| Studio HTTP | DNS rebinding, CSRF, XSS, unauthorized access |
| OTLP | Man-in-the-middle, wrong endpoint, sensitive field leakage |
| Price table | Malicious JSON, path traversal, remote tampering |
| Supply chain | Dependency poisoning, build machine compromise |

---

## 3. Privacy Design

### 3.1 Content capture levels

| Level | Collected | Default | Purpose |
| --- | --- | --- | --- |
| `None` | Prompt/Completion not collected | Yes | The vast majority of scenarios |
| `MetadataOnly` | Length, hash, token estimate | No | Correlation needed without viewing content |
| `Full` | Original text (with limits) | No | Local debugging, explicitly enabled |

Rules:

- When `Full` is enabled, the CLI and Studio display a prominent warning.
- `Full` is retained for 7 days by default and is not included in regular aggregate exports.
- `Full` does not change the default OTLP export behavior; redaction is mandatory before export.

### 3.2 Hashing and identifiers

- `user` / `tenant` are truncated SHA-256 (16 hex) with a process-level salt by default.
- The salt is stored in a local configuration file with permissions 0600; it is not exported.
- Hashing is not a promise of anonymization; the document clearly states the boundary of "correlatable, irreversible".
- Low-sensitivity dimensions such as `model`, `provider`, and `feature` are not hashed.

### 3.3 Sensitive Headers

Dropped by default:

- `Authorization`
- `Proxy-Authorization`
- `api-key`
- `x-api-key`
- `cookie`
- `set-cookie`
- `x-amz-security-token`
- `x-goog-api-key`

Rules:

- Allowlist takes precedence over denylist: only explicitly allowed headers are collected.
- If users collect custom headers, it must be explicitly configured and warned about in the documentation.

### 3.4 Logs

- Logs do not contain Prompt, Completion, Key, or Cookie.
- Error messages record only hashes or types; sensitive content in the original exception message (which may contain the request body) is not recorded.
- Debug logging is off by default; a warning is displayed when enabled.

---

## 4. Local Studio Security

### 4.1 Network binding

- Defaults to `127.0.0.1` with a random port.
- Default `0.0.0.0` is prohibited; if the user explicitly requests remote access, you must:
  - Display a high-risk warning.
  - Require an access token to be set.
  - Document the responsibilities for reverse proxy and TLS.

### 4.2 Authentication

- Generate a 32-byte random token at startup (`RandomNumberGenerator`).
- The URL carries a one-time token; the API uses `Authorization: Bearer`.
- The token is stored only in process memory; it becomes invalid when the process exits.
- Do not write the token to shell history: CLI output uses the `open` command or a local file.

### 4.3 Host validation and DNS rebinding

- Validate that the `Host` header is `127.0.0.1:<port>` or `localhost:<port>`.
- Validate that `Origin` (if present) is same-origin.
- Disable CORS; do not respond to cross-origin preflight requests.
- Set `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`.
- CSP: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'`.
- Do not use external CDNs, fonts, or icons.

### 4.4 Input handling

- Query parameter allowlist and length limits.
- Pagination limits (default 100, maximum 1000).
- Sort field allowlist to prevent SQL injection.
- JSONL parsing uses `Utf8JsonReader` + source generation; unknown fields are ignored.
- Rendering uses React default escaping; `dangerouslySetInnerHTML` is prohibited.
- File name/path parameters do not accept user input; session IDs are validated as ULIDs.

### 4.5 CSRF

- The local token already provides authentication; additionally require:
  - All write operations use `POST` + `Content-Type: application/json`.
  - Validate `Origin` / `Sec-Fetch-Site`.
  - Do not use cookie authentication.

---

## 5. File System Security

| Threat | Mitigation |
| --- | --- |
| Path traversal | All paths consist of a configured root directory + fixed subdirectories; reject `..` |
| Symbolic links | Check that the target is not a symlink before writing; open safely using `FileOptions` |
| Excessive permissions | Unix `0600` files, `0700` directories; Windows ACL for the current user only |
| File replaced | Validate the inode/handle when opening; use exclusive writes for sensitive files |
| Disk exhaustion | Retention policy + maximum size + circuit breaker |
| Malicious JSONL | Limit line size, nesting depth, and field count |
| SQLite WAL/SHM | Same permissions as the main database (0600); cleaned up on shutdown; do not accept user-specified paths |
| Archive bomb | Compressed import is not supported; if supported, limit the decompression ratio |

Default directories:

    ~/.runesvaner/            # User-level
    .runesvaner/              # Project-level (preferred)

v1 platform scope: Windows 10/11 and Linux; macOS is a non-goal (D-15).

---

## 6. Export Security

### 6.1 OTLP

- Off by default.
- The endpoint must be explicitly configured; `http://` is not allowed by default (except localhost).
- Supports `https://` and local collectors.
- Headers are stored in the configuration with permissions 0600; redacted in logs.
- Execute `IContentRedactor` before export.
- Failures do not affect the business.

### 6.2 Exported fields

Exported by default:

- Time, duration, segments, tokens, cost, provider, model, status.
- `feature`, `tenant_hash`, `prompt_version` (low cardinality, hashed).

Not exported by default:

- Prompt / Completion original text.
- `user_id` original text.
- Raw URL (may contain query parameters).
- Raw exception messages.

### 6.3 Third-party compatibility

- Langfuse / Phoenix receive via OTLP; field mappings are listed in the documentation.
- The privacy policies of third-party platforms are not guaranteed; users must evaluate them themselves.
- Provides a "local-only" mode with no export at all.

---

## 7. Supply Chain Security

| Measure | Description |
| --- | --- |
| Dependency minimization | Core has zero third-party dependencies; optional packages are on demand |
| Version pinning | Centrally managed in `Directory.Packages.props` |
| NuGet signing | Release packages are signed; dependency signatures are verified |
| SBOM | SPDX/CycloneDX generated on every release |
| CodeQL | GitHub Actions weekly + on PR |
| Dependabot | Weekly dependency updates; immediate security updates |
| Secret scanning | Enable GitHub secret scanning + push protection |
| Build isolation | GitHub-hosted runners; least-privilege tokens |
| Release authentication | NuGet Trusted Publishing (OIDC), no long-lived API keys |
| Artifact signing | GitHub Artifact Attestations (if available) |
| Code review | At least 1 maintainer approval; 2 people for sensitive changes |

### 7.1 Dependency review checklist

New dependencies must answer:

1. Is the license compatible with Apache-2.0?
2. Does it support AOT/Trim?
3. Maintenance status and security record?
4. Size increase?
5. Can it be replaced?
6. Does it introduce native dependencies?

---

## 8. Threat Model (simplified STRIDE)

| Threat | Scenario | Mitigation |
| --- | --- | --- |
| Spoofing | Forged Studio requests | Local token + Host/Origin validation |
| Tampering | Tampered JSONL / price table | Checksums; open read-only; version field |
| Repudiation | Cannot prove data provenance | session_id, timestamp, schema version |
| Information Disclosure | Prompt leakage | Not collected by default; export redaction; file permissions |
| Denial of Service | Many requests cause queue growth | Backpressure, drop oldest, circuit breaker |
| Elevation of Privilege | Write files via path traversal | Path allowlist, symlink check |
| XSS | Malicious Prompt rendered in Studio | React escaping; CSP; no HTML injection |
| CSRF | Malicious web page calls local API | token + Origin validation |
| DNS Rebinding | Malicious domain resolves to 127.0.0.1 | Host validation + token |
| Supply chain | Malicious NuGet package | Signing, pinning, review, SBOM |
| Prompt injection | Malicious content affects diagnostic conclusions | Diagnostic conclusions are based only on numeric values; content is not executed |

> Prompt injection is not an execution risk for Runesvaner (it does not call an LLM to process content), but if "AI-assisted root cause analysis" is added in the future, it must be re-evaluated.

---

## 9. Compliance Notes

The following are engineering recommendations, not legal advice:

- **GDPR**: If user identifiers or content are collected, the legal basis, retention period, and data subject rights must be defined.
- **CCPA**: Provide deletion and export mechanisms.
- **Data residency**: Local-first naturally satisfies this; OTLP export requires the user to confirm the region.
- **Industry compliance**: In healthcare and financial scenarios, `ContentCapture.None` is the default.
- **DPA**: The project itself does not provide SaaS and has no DPA; enterprises self-hosting must evaluate it themselves.

---

## 10. Security Testing

| Test | Frequency |
| --- | --- |
| Dependency vulnerability scanning | Every PR + daily |
| CodeQL | Every PR + weekly |
| Secret scanning | Every push |
| Path traversal/symlink tests | Every storage change |
| Studio authentication bypass tests | Every API change |
| XSS/CSRF tests | Every UI change |
| Malicious JSONL fuzzing | From M2 |
| AOT security smoke | Every release |
| Third-party penetration testing | Before 1.0 (budget permitting) |

---

## 11. Vulnerability Response

1. Receive: `SECURITY.md` provides a private channel.
2. Acknowledge: respond within 48 hours.
3. Fix: P0 within one release cycle; P1 within two cycles.
4. Disclose: 90 days after the fix or as agreed with the reporter.
5. Credit: listed after consent.
6. CVE: request when necessary.

---

## 12. Incident Response Plan

| Incident | Action |
| --- | --- |
| Package poisoned | Delist the package, publish a security advisory, rotate keys |
| Studio authentication bypass | Release a patch immediately, advise users to shut down Studio |
| Content leakage | Stop export, clean up files, notify users |
| Supply chain attack | Freeze releases, audit the build, rebuild artifacts |
| Price table error | Release a corrected version, annotate historical data |

---

## 13. Decision Status

D-01–D-25 have all been decided; see `15-decision-log.md`. Directly related to this document:

- D-07: Library SQLite by default; CLI AOT JSONL by default; fallback on failure.
- D-09: Default content capture is `None`.
- D-10: No commercialization for now; unbundled sponsorship is accepted; no hosted service.
- D-11: Trademark held personally; successor transfer possible after 36 months of inactivity.
- D-13: OTLP off by default.
- D-20: No forced compatibility with third-party semantics.