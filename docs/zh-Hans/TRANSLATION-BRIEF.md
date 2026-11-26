# Translation Brief — English Source of Truth

This directory contains the Chinese legacy full translation. The English files in `docs/` are the source of truth.

## Rules for English documents

1. Translate faithfully and completely. Do not summarize, merge, or omit sections, tables, formulas, defaults, edge cases, risks, acceptance criteria, decision IDs, URLs, or code identifiers.
2. Preserve Markdown structure: heading levels, tables, lists, block quotes, indentation, and relative links.
3. Keep these unchanged:
   - Product/brand: `Runesvaner`
   - Decision IDs: `D-01` … `D-25`
   - Package/type/CLI names, JSON fields, metric names, code spans
   - File names and relative links
4. Use this terminology:
   - Diagnostic Session = diagnostic session
   - Request = request
   - Segment = segment
   - Latency waterfall = latency waterfall
   - Cost ledger = cost ledger
   - Price table = price table
   - Estimated = estimated
   - Unaccounted = unaccounted
   - Sampling = sampling
   - Trace = trace
   - Token = token
   - Provider = provider
   - Tenant = tenant
   - Feature = feature
5. English technical style: concise, precise, no marketing language, no emojis.
6. Do not edit `docs/zh-Hans/`, `README.md`, code, or build files when translating a source document.
7. Do not run builds or tests for a translation-only task.
8. After writing, report the target file and any Chinese fragments intentionally retained (for example, quoted Chinese product names).

## Status

- English: complete after translation.
- Chinese (`zh-Hans`): legacy full version; will be condensed to core-only per D-16/D-18.
- German (`de-DE`): pending; core-only summary planned.