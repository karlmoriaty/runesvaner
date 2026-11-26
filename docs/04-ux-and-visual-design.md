# UI/UX and Visual Design Specification: Runesvaner Studio

| Field | Value |
| --- | --- |
| Document status | Draft |
| Version | 0.1 |
| Date | 2026-11-25 |
| Audience | Design, frontend, engineering |
| Related documents | `03-prd.md`, `07-api-design.md`, `15-decision-log.md` |

---

## 1. Design Positioning

Runesvaner Studio is an **engineering instrument**, not a marketing page and not a consumer application.

Keywords:

- **Precise**: like an oscilloscope, a spectrometer, or a debugger, not an "AI assistant".
- **Restrained**: monochrome base, a single accent color, no decorative gradients.
- **High density**: more useful information per screen, without feeling crowded.
- **Explainable**: every number can be drilled down to its measurement method.
- **Trustworthy**: estimated values must be explicitly marked; uncertainty is never hidden behind visual tricks.

### 1.1 Reference Products and What to Borrow

| Product | Borrow | Do not borrow |
| --- | --- | --- |
| Linear | Information density, keyboard-first, restrained motion | Rounded corners and soft shadows |
| Datadog | Chart density, filters, timeline | Complex navigation and ad-like upgrade prompts |
| Sentry | Trace waterfall, error context | Cartoon illustrations |
| Grafana | Dark data visualization, panel layout | Saturation of the default theme |
| Vercel | Typography, whitespace, monochrome system | Large rounded cards |
| JetBrains debugger | Monospaced numbers, tree structures, property panel | Dense toolbars |

### 1.2 Explicit Anti-Patterns

Prohibited:

- Large corner radii (> 2px), pill buttons, circular avatar cards.
- Purple / green neon gradients, glowing borders, AI light effects.
- Glassmorphism, frosted glass, heavy shadows, flashy highlights.
- Cartoon illustrations, 3D icons, anthropomorphic assistants.
- Pure white backgrounds + large gradient areas.
- Meaningless skeleton-screen animations and bounce.
- Using red/green as the only means of distinguishing states.

---

## 2. Design Principles

1. **Conclusion first**: the first screen gives "total duration + largest segment + total cost" before details.
2. **Monochrome base + one accent color**: amber copper is the only brand accent; semantic colors appear only when necessary.
3. **Right angles first**: 0px corner radius by default; inputs and buttons at most 2px.
4. **Monospaced numbers**: all durations, tokens, and amounts use a monospace font and are right-aligned.
5. **Estimates are visible**: `estimated` is marked with hatching or a dashed line and explained in a tooltip.
6. **Color is not the only encoding**: segments use color, texture, and label together.
7. **Keyboard first**: primary paths do not depend on the mouse.
8. **Motion serves state change**: 120–180ms, ease-out, no bounce.
9. **Empty states are actionable**: give the next command instead of "no data".
10. **Local feel**: show the data directory, session count, and storage usage so users know where the data lives.

---

## 3. Color System

### 3.1 Dark Theme (default)

| Token | Value | Usage |
| --- | --- | --- |
| `bg.canvas` | `#0A0C0F` | Application background |
| `bg.surface` | `#101419` | Panels |
| `bg.raised` | `#161B22` | Overlays, selected rows |
| `bg.inset` | `#0D1116` | Inputs, code blocks |
| `border.subtle` | `#222A33` | Dividers |
| `border.strong` | `#33404D` | Focus, selection |
| `text.primary` | `#E8ECF1` | Primary text |
| `text.secondary` | `#9AA6B2` | Secondary text |
| `text.muted` | `#6B7785` | Muted text, units |
| `accent` | `#C8963E` | Brand, primary button, selection |
| `accent.hover` | `#D9A94F` | Hover |
| `accent.muted` | `#3A2E1C` | Accent background |
| `success` | `#4E9B72` | Success, cache hit |
| `warning` | `#C9A227` | Warning, estimate |
| `danger` | `#C05B55` | Error, over budget |
| `info` | `#5B84A8` | Information, links |

### 3.2 Light Theme (optional)

| Token | Value | Usage |
| --- | --- | --- |
| `bg.canvas` | `#F4F2ED` | Warm gray paper-like background |
| `bg.surface` | `#FBFAF7` | Panels |
| `bg.raised` | `#FFFFFF` | Overlays |
| `border.subtle` | `#DDD8CE` | Dividers |
| `text.primary` | `#1B1F24` | Primary text |
| `text.secondary` | `#56606B` | Secondary text |
| `accent` | `#8A5A1F` | Deep amber |
| `danger` | `#A9443F` | Error |

The light theme does not use large pure white areas, to avoid a "cheap SaaS" feel.

Light theme design constraints (D-12):

- Background is warm gray paper-like (`#F4F2ED`), panels are `#FBFAF7`, large pure white areas are prohibited.
- The accent color is deep amber (`#8A5A1F`), not purple/green neon.
- Maintain right angles and 1px borders; no glassmorphism, gradients, or glow.
- Light is not a simple inversion of dark: text contrast, chart grids, and segment colors must be calibrated separately.
- Switch via `prefers-color-scheme` and manually; dark is the default.

### 3.3 Segment Color Scale

Used for waterfalls and stacked bars. Hues are concentrated in amber, blue-gray, cyan-gray, and brown-gray, avoiding purple/green neon.

| Segment | Dark | Light | Texture |
| --- | --- | --- | --- |
| Queue | `#6B7785` | `#7A8490` | Dots |
| Auth | `#8C6A3A` | `#8A6A3F` | None |
| Connect | `#5B84A8` | `#4C7394` | Diagonal lines |
| TLS | `#6F8FA8` | `#5E7E96` | Cross-hatch |
| Upload | `#4E9B72` | `#3F805D` | None |
| Provider Queue | `#C9A227` | `#A8871F` | Diagonal hatch |
| TTFT | `#C8963E` | `#8A5A1F` | None |
| Generation | `#D9A94F` | `#A97028` | None |
| Tool | `#7C8A99` | `#66727F` | Grid |
| Vector | `#5F7F8C` | `#4E6C78` | Dash-dot |
| Serialization | `#8A8F98` | `#737880` | None |
| Retry | `#C05B55` | `#A9443F` | Diagonal hatch |
| Unaccounted | `#3A414A` | `#C9C3B8` | Diagonal hatch |

---

## 4. Type and Typography

### 4.1 Typefaces

| Usage | Typeface | Fallback |
| --- | --- | --- |
| UI text | Inter | `-apple-system, "Segoe UI", "Noto Sans SC", sans-serif` |
| Numbers / code | JetBrains Mono | `"SFMono-Regular", Consolas, monospace` |
| Headings | Inter (600) | Same as above |

No decorative typefaces. Chinese text uses system fonts to avoid font payload.

### 4.2 Font Size and Line Height

| Token | Size | Line height | Usage |
| --- | ---: | ---: | --- |
| `text.xs` | 11px | 16px | Labels, units |
| `text.sm` | 12px | 18px | Tables, secondary information |
| `text.base` | 13px | 20px | Body text |
| `text.md` | 14px | 22px | Primary readings |
| `text.lg` | 18px | 26px | Section headings |
| `text.xl` | 24px | 32px | Page headings |
| `text.display` | 40px | 48px | Total duration / total cost |

Base font size is 13px, matching high-density engineering tools. Body text does not use 16px, to avoid a "consumer website" feel.

---

## 5. Spacing, Grid, and Shape

### 5.1 Spacing

4px base: 4 / 8 / 12 / 16 / 24 / 32 / 48 / 64.

### 5.2 Layout Grid

| Region | Size |
| --- | --- |
| Top bar | 48px |
| Left navigation | 232px (collapsible to 56px) |
| Right details | 380px (draggable 320–520px) |
| Minimum content width | 720px |
| Minimum page width | 1280px |

### 5.3 Shape

| Element | Corner radius |
| --- | --- |
| Buttons | 2px |
| Inputs | 2px |
| Panels / cards | 0px |
| Table rows | 0px |
| Tags | 2px |
| Segment bars | 0px |
| Icon buttons | 2px |

Pill shapes (9999px) and circular buttons are prohibited, unless it is a pure icon action and the size is ≤ 24px.

### 5.4 Shadows

Used only for overlays and dropdown menus:

- `shadow.overlay`: `0 8px 24px rgba(0,0,0,0.35), 0 0 0 1px #222A33`
- Panels do not use shadows; they are separated by 1px borders and background elevation.

---

## 6. Icons and Logo

### 6.1 Icons

- Line icons, 24px grid, 1.5px stroke.
- Lucide is recommended as the base set; key data icons are drawn in-house to avoid stylistic inconsistency.
- Icons do not carry meaning alone; they must be paired with text or an `aria-label`.

### 6.2 Logo Concept

**Concept: aperture / prism decomposition.**

- Outer contour: an approximately circular shape made of five aperture blades, with negative space at the center.
- Lower right: three thin lines radiating from the center, representing "latency, tokens, cost".
- Monochrome graphite; only a single small amber dot at the center serves as the accent.
- Two versions: 32×32 and 16×16; the 16×16 version drops the thin lines and keeps only the aperture contour.
- Prohibited: glow, gradients, 3D, anthropomorphism.

Suggested SVG structure (design sketch, not final asset):

- `viewBox="0 0 32 32"`
- 5 arcs form the blades, `stroke="currentColor"`, `stroke-width="1.5"`, `fill="none"`
- 3 lines radiate from `(16,16)` to the lower right, `stroke-width="1"`
- Center circle `r="1.5"`, `fill="#C8963E"`

---

## 7. Page Structure

### 7.1 Global Layout

    +--------------------------------------------------------------+
    | Top Bar: Product | Session | Search | Time range | Status | Settings |
    +----------+--------------------------------------+------------+
    | Nav      | Main Content                         | Inspector  |
    | Sessions |                                      | Properties |
    | Requests |                                      | / Cost     |
    | Costs    |                                      |            |
    | Compare  |                                      |            |
    | Health   |                                      |            |
    | Settings |                                      |            |
    +----------+--------------------------------------+------------+

### 7.2 Navigation

| Item | Content |
| --- | --- |
| Sessions | Diagnostic session list (one process run) |
| Requests | Request list and filters |
| Costs | Cost ledger and attribution |
| Compare | Comparison of two requests / two versions |
| Health | Collector self-monitoring: queue, drops, persistence latency |
| Settings | Storage, privacy, price table, sampling |

### 7.3 Sessions List

Columns:

- Start time
- Application / environment
- Request count
- P50 / P95 latency
- Total cost
- Error count
- Data size

Empty state: show the `runesvaner run -- <your app>` command, copyable in one click.

### 7.4 Requests List

Columns:

- Time
- Operation (chat / embeddings / tool / vector)
- Provider / Model
- Total duration
- TTFT
- Input / Output tokens
- Cost
- Status
- Anomaly markers (retry, cache, unknown price)

Interaction:

- `J` / `K` move up and down, `Enter` opens details.
- `/` focuses search, `f` opens filters.
- Columns are sortable; filters can be saved as views.

### 7.5 Request Detail: Waterfall

Structure:

1. **Summary bar**: total duration, TTFT, Tokens/s, total cost, status.
2. **Waterfall**: horizontal timeline, segmented lanes.
3. **Segment table**: name, duration, share, measurement method, estimated.
4. **Property panel**: provider, model, tenant hash, prompt version, attempt.
5. **Cost card**: line items, price table version, cache hit rate.
6. **Error / retry card**: error type, backoff, whether double billing is possible.

Waterfall rules:

- The timeline uses adaptive ticks (1ms / 10ms / 100ms / 1s).
- Parallel segments use multiple lanes; within a lane, order by start time.
- `estimated` segments use hatch fill + dashed border.
- `unaccounted` is fixed at the bottom, hatched gray.
- Hover shows: start offset, duration, share, measurement method.
- Clicking a segment: the Inspector shows raw attributes and events.

### 7.6 Costs Ledger

Top:

- Total cost (large monospaced number)
- Currency (USD by default)
- Price table version and effective date
- Time range
- Data completeness (sampling-weighted, count of unknown prices)

Charts:

- Stacked cost by day (Input / Output / Cached / Reasoning)
- Top N table by Feature / Tenant / Model
- Cost vs latency scatter plot (anomalies highlighted)
- Cache hit rate trend

Anomaly area:

- Potential double billing caused by retries
- Models with unknown prices
- Sudden increase in unit cost
- Budget overruns

### 7.7 Compare

- Select two requests or two time windows.
- Side-by-side waterfalls, aligned by the same segments.
- Difference table: absolute difference, percentage, direction.
- Conclusion area: automatically generates explainable sentences such as "the generation phase was 1.2s slower, mainly due to a 38% increase in output tokens".
- Automatic conclusions must expand to evidence; black boxes are not allowed.

### 7.8 Health

Observability of the collector itself:

- Queue length
- Drop event count
- Persistence latency P50/P95
- Estimated collection overhead
- Circuit breaker state
- Version and configuration snapshot

### 7.9 Settings

- Storage path, retention policy
- Content capture level (None / Metadata / Full)
- Sampling rules
- Price table source and overrides
- Export configuration (OTLP endpoint, headers, redaction)
- Theme (dark / light / follow system)

---

## 8. Component Specifications

### 8.1 Buttons

| Type | Background | Border | Text |
| --- | --- | --- | --- |
| Primary | `accent` | None | `#0A0C0F` |
| Secondary | `bg.raised` | `border.subtle` | `text.primary` |
| Ghost | Transparent | None | `text.secondary` |
| Danger | `danger` | None | `#0A0C0F` |

Height: 28px (compact) / 32px (default). Padding: 12px. Corner radius: 2px.

### 8.2 Tables

- Row height 32px; header 28px.
- Headers use `text.xs` uppercase with 0.04em letter spacing.
- Numbers are right-aligned and monospaced.
- Hover background `bg.raised`; selected background `accent.muted` + 2px `accent` edge on the left.
- No zebra striping.

### 8.3 Tags

- Height 18px, font size 11px, corner radius 2px.
- Semantic tags: `estimated` (yellow), `retry` (red), `cached` (green), `unknown` (gray).
- Tags do not use saturated fills; use a 12% opacity background + text in the same hue.

### 8.4 Filters

- A single row of chips at the top; supports AND combinations.
- Numeric ranges use dual inputs, not sliders (poor precision).
- Time ranges use presets + custom.

### 8.5 Charts

- Grid lines `border.subtle`, horizontal lines only.
- Axis labels `text.xs`, `text.muted`.
- No 3D, no shadows, no gradient fills.
- Hover crosshair + tooltip; tooltips use monospaced numbers.
- Legends are clickable for filtering; color + texture dual encoding.

### 8.6 Empty States

Must contain:

1. One sentence of explanation.
2. One copyable command.
3. One documentation link.

### 8.7 Loading and Errors

- Loading: static skeleton + 200ms delay before showing, to avoid flicker.
- Errors: error code, cause, next step, copy diagnostic information.
- Do not use humorous copy or emoji.

---

## 9. Motion

| Scenario | Duration | Curve |
| --- | --- | --- |
| Panel expand | 160ms | ease-out |
| Row selection | 120ms | linear |
| Inspector switch | 140ms | ease-out |
| Toast | 180ms | ease-out |
| Chart update | 200ms | ease-out |

Prohibited: bounce, rotation, parallax, glow pulse. Respect `prefers-reduced-motion`.

---

## 10. Accessibility

- Contrast ≥ 4.5:1 (body text), ≥ 3:1 (large text and graphics).
- Keyboard reachable: Tab order, visible focus (2px `accent` outline).
- The waterfall provides a "table view" alternative.
- Charts provide text summaries.
- State is not conveyed by color alone; use icon + text + texture.
- Support 200% zoom without breaking layout.
- Language: the UI defaults to English (`en-US`), switchable to Simplified Chinese (`zh-Hans`) and Standard German (`de-DE`); dates and numbers are formatted per locale.

---

## 11. Responsive

| Width | Behavior |
| --- | --- |
| ≥ 1440px | Full three columns |
| 1280–1439px | Inspector narrows to 320px |
| 1024–1279px | Inspector becomes a drawer |
| < 1024px | List and details only; the full diagnostic experience is not guaranteed |

Desktop first; no full mobile adaptation.

---

## 12. Copy Tone

- Concise, specific, actionable.
- Use engineering terminology; no anthropomorphism.
- Error copy structure: `what happened` + `why` + `next step`.
- Examples:
  - Good: "TTFT cannot be measured precisely: the provider did not return time to first byte. Marked as estimated."
  - Bad: "Oops, something seems to have gone wrong~"

---

## 13. Design Deliverables

| Deliverable | Form | Status |
| --- | --- | --- |
| Design tokens | JSON + CSS variables | Pending M1 |
| Logo | SVG (32/16) | Pending design |
| Icon set | SVG sprite | Pending M1 |
| Page wireframes | This document | Draft |
| High-fidelity mockups | Figma / Penpot | Decided D-06; mockups pending M1 |
| Component library | React + CSS Modules | Pending M2 |

### 13.1 CSS Token Example

    :root {
      --ap-bg-canvas: #0A0C0F;
      --ap-bg-surface: #101419;
      --ap-bg-raised: #161B22;
      --ap-border-subtle: #222A33;
      --ap-text-primary: #E8ECF1;
      --ap-text-secondary: #9AA6B2;
      --ap-accent: #C8963E;
      --ap-danger: #C05B55;
      --ap-radius: 2px;
      --ap-font-ui: Inter, system-ui, sans-serif;
      --ap-font-mono: "JetBrains Mono", Consolas, monospace;
    }

---

## 14. Decision Status

D-06, D-12, D-16 resolved (2026-11-25):

- D-06: React 19 + TypeScript + Vite + Redux Toolkit + RTK Query; CSS Modules + design tokens; Radix Primitives; uPlot + custom-drawn waterfall.
- D-12: Provide a light theme; dark first in M2, light completed in 1.0; must avoid "AI flavor".
- D-16: The UI defaults to English, switchable to Simplified Chinese and Standard German (`de-DE`).
- D-07: Studio starts embedded with the CLI (local host) and is not deployed separately.
- D-16: Whether UI copy defaults to English.