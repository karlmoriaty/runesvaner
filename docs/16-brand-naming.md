# Naming Resolution and Brand Direction: Runesvaner

| Field | Value |
| --- | --- |
| Document status | **Resolved** |
| Version | 1.0 |
| Resolution date | 2026-11-25 |
| Resolved by | Project initiator |
| Related documents | `04-ux-and-visual-design.md`, `15-decision-log.md` |

---

## 1. Final name

**Runesvaner**

| Item | Value |
| --- | --- |
| Product name | Runesvaner |
| Chinese name | To be confirmed by the initiator (no Chinese brand name is used in documents for now) |
| CLI | `runesvaner` |
| Package prefix | `Runesvaner.*` |
| Namespace | `Runesvaner` |
| Configuration section | `Runesvaner` |
| Environment variables | `RUNESVANER_*` |
| Data directory | `.runesvaner/` |
| Meter / ActivitySource | `Runesvaner.AI` |
| Type prefix | `Runesvaner*` |
| Studio | Runesvaner Studio |

> The name was chosen by the initiator (D-02). The name's meaning/etymology is not speculated in the document; if the initiator wishes to add a brand story, it may be appended to this section.

---

## 2. Initial availability screening

Check date: 2026-11-25.

| Check item | Method | Result |
| --- | --- | --- |
| NuGet `runesvaner` | flatcontainer API | 404 (no package currently) |
| NuGet `runesvaner.core` | flatcontainer API | 404 |
| NuGet `runesvaner.abstractions` | flatcontainer API | 404 |
| NuGet `runesvaner.ai` | flatcontainer API | 404 |
| NuGet `runesvaner.cli` | flatcontainer API | 404 |
| NuGet `runesvaner.studio` | flatcontainer API | 404 |
| NuGet `runesvaner.storage.sqlite` | flatcontainer API | 404 |
| NuGet `runesvaner.storage.jsonl` | flatcontainer API | 404 |
| NuGet `runesvaner.exporters.opentelemetry` | flatcontainer API | 404 |
| NuGet `runesvaner.microsoft.extensions.ai` | flatcontainer API | 404 |
| NuGet `runesvaner.semantic.kernel` | flatcontainer API | 404 |
| NuGet `runesvaner.vectordata` | flatcontainer API | 404 |
| NuGet `runesvaner.analyzers` | flatcontainer API | 404 |
| GitHub user `runesvaner` | users API | 404 (no occupation found) |
| GitHub organization `runesvaner` | orgs API | 404 (no occupation found) |
| GitHub repository `runesvaner/runesvaner` | repos API | 404 (no occupation found) |
| GitHub search for repositories with the same name | search API | 0 |
| Domains `runesvaner.dev` / `.com` / `.io` / `.tools` / `getrunesvaner.dev` | DNS | No resolution (does not imply registrability) |
| Trademark | USPTO/EUIPO/CNIPA | **Deferred** (user decision 2026-11-26; reevaluate before formal preview) |

Conclusion:

- No occupation was found for public packages, GitHub user/organization/repository, or candidate domains.
- A lack of DNS resolution for a domain only means it is currently unused, not that it is registrable; WHOIS confirmation is required.
- Trademark search is deferred (no lawyer/budget); reevaluate before formal public preview or 1.0.
- Usable during the working-name phase; domain confirmation and trademark search are reevaluated before formal preview.

---

## 3. Name evaluation

| Dimension | Evaluation |
| --- | --- |
| Relevance | Neutral to positive; "Rune" suggests symbols/interpretation, "svaner" has no direct meaning, and the whole is distinctive |
| Readability | 10 characters, about 3 syllables; readable for English/German users, requires adaptation for Chinese users |
| Distinctiveness | High; no occupation found for GitHub user/organization/repository or the 13 NuGet IDs |
| Trademark risk | Search deferred; coined words usually carry lower risk, reevaluate before formal preview |
| CLI friendliness | `runesvaner` is all lowercase, has no hyphens, and has acceptable length |
| Internationalization | Contains no negative meanings; pronunciation consistency needs confirmation |
| Search friendliness | Distinctive and easy to search; but the spelling must be remembered correctly at once |

### 3.1 Risks

1. **Spelling cost**: 10 characters; users may misspell (rune + svaner). The CLI and documentation must be consistent; provide a short alias `rsv` if necessary (pending).
2. **Uncertain pronunciation**: The initiator needs to confirm the official pronunciation; documentation does not force phonetic notation.
3. **Trademark deferred**: Reevaluate classes 9/42 before formal public preview or 1.0; currently does not block the working-name phase.
4. **Domain unconfirmed**: There is currently no DNS resolution, but WHOIS registrability confirmation is required; does not affect M0, affects the official website.

---

## 4. Candidate history (not adopted)

The following candidates were evaluated before the D-02 resolution and are retained as historical records; **they do not represent the current name**.

| Name | NuGet | Meaning | Reason not adopted |
| --- | --- | --- | --- |
| Aperture | Available (404) | Aperture; focus/decompose | Not adopted; association with an Apple historical product, generic word |
| Umbra | Available (404) | Umbra | Not adopted; semantics lean toward "occlusion" |
| Quasar | Available (404) | Quasar | Not adopted; search pollution risk |
| Diffract | Taken | Diffraction | Not adopted; package ID conflict |
| Fathom | Taken | Sounding | Not adopted; trademark conflict |
| Parallax | Taken | Parallax | Not adopted; trademark conflict |
| Refract | Taken | Refraction | Not adopted; package ID conflict |
| Prism | Taken | Prism | Not adopted; generic word, many conflicts |
| Candela | Taken | Unit of luminous intensity | Not adopted; package ID conflict |
| Vernier | Taken | Vernier caliper | Not adopted; trademark of an education company |
| Tomograph | Available (not checked) | Tomography | Not adopted; too long |

> `Aperture` was once used as the working name in older documents; since 2026-11-25 it has been fully replaced with `Runesvaner`.

---

## 5. Naming conventions

GitHub repository name: `runesvaner`; the product name, package prefix, and documentation site remain `Runesvaner`.

### 5.1 Package naming

    Runesvaner
    Runesvaner.Abstractions
    Runesvaner.MicrosoftExtensionsAI
    Runesvaner.SemanticKernel
    Runesvaner.VectorData
    Runesvaner.Storage.Sqlite
    Runesvaner.Storage.Jsonl
    Runesvaner.Exporters.OpenTelemetry
    Runesvaner.Cli
    Runesvaner.Studio
    Runesvaner.Analyzers        # v2

### 5.2 Code naming

| Element | Rule | Example |
| --- | --- | --- |
| Options | `RunesvanerOptions` | `RunesvanerOptions.Storage` |
| Interface | `IRunesvaner*` | `IRunesvanerRecorder` |
| Extension methods | `AddRunesvaner` / `UseRunesvaner` | `services.AddRunesvaner()` |
| Tags | `RunesvanerTags` | `RunesvanerTags.Feature` |
| Events | `RunesvanerEvent` | `RunesvanerEvent.RequestEnd` |
| Meter | `Runesvaner.AI` | `runesvaner.ai.ttft` |
| ActivitySource | `Runesvaner.AI` | — |

### 5.3 Files and directories

    .runesvaner/
      sessions/
      runesvaner.db
      pricing/
      config.json
      studio.token

### 5.4 Environment variables

    RUNESVANER_STORAGE_PATH
    RUNESVANER_STORAGE_MODE
    RUNESVANER_CONTENT_CAPTURE
    RUNESVANER_SAMPLING_HEAD_PROBABILITY
    RUNESVANER_OTLP_ENDPOINT

---

## 6. Brand direction

| Dimension | Direction |
| --- | --- |
| Character | Precise, restrained, engineering instrumentation; not a consumer AI product |
| Color | Graphite black + amber copper; light is warm gray paper feel; purple-green neon prohibited |
| Shape | Square corners preferred, maximum 2px corner radius; pills/large corner radii prohibited |
| Icons | Linear, 1.5px stroke; avoid AI glow |
| Logo concept | An abstract combination of "rune/scale + observation"; no literal camera aperture; specific drafts pending design |
| Copy | Primarily English; condensed Chinese/German versions; no exaggerated adjectives |

> Logo, font, and icon assets are produced during the M1 design phase; this document does not generate images.

---

## 7. Renaming cost and freeze

- Since 2026-11-25, documents, package names, namespaces, and the CLI have been unified as `Runesvaner`.
- No further renaming after public preview (M2); if the trademark search fails later, a replacement name must be decided before formal preview.
- Replacement name priority: `Umbra`, `Quasar` (available on NuGet); the final decision is made by the initiator.

---

## 8. Next steps

1. The initiator confirms the name's meaning/pronunciation (optional).
2. Trademark search deferred; reevaluate before formal preview.
3. WHOIS confirm the domain (all current candidates have no DNS resolution).
4. Completed: GitHub repository `karlmoriaty/runesvaner` (2026-11-26).
5. M0 registers the NuGet root package reservation (the 13 planned IDs are currently 404).
6. M1 produces three Logo draft versions.

---

## 9. Related resolutions

- D-02: Name Runesvaner.
- D-11: Trademark held personally; transferable if inactive for 36 months.
- D-12: Light theme design constraints.
- D-16: Multilingual strategy.