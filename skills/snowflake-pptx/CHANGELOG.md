# Changelog

All notable changes to this project are documented here.

---

## [1.6.0] — 2026-03-23

Reliability and font sizing fixes from generation session `0d5d1106` review (Semantic Views deck): increased `four_column_numbers` content font sizes to match user expectations, improved line-count estimation with word-wrap awareness, fixed macOS script permissions, and updated QA workflow for large decks. 63 tests passing.

### Fixed — `four_column_numbers` content font size too small (Issue 6, P0)

- **Root cause**: `ApplyFourColumnNumbersContentFontSize` font table was too aggressive — dropped to 13pt at 56 chars and 12pt at 70 chars. User manually corrected slide 8 from 13pt to 16pt in the delivered deck.
- **Fix**: Raised font table by 2-3pt across all tiers: ≤55 chars → 16pt (was 14pt), 56-69 → 15pt (was 13pt), 70-80 → 14pt (was 12pt), >80 → 14pt floor (was 12pt).
- **Verified**: Rebuilt the Semantic Views deck; slide 8 content now renders at 15pt (content is 65 chars, hitting the 56-69 tier).

### Fixed — `EstimateLineCount` naive character-division overestimates (Issue 2, P1)

- **Root cause**: `Validator.EstimateLineCount` used `ceil(paragraph.Length / charsPerLine)` which doesn't account for word-wrap behaviour. A 73-char string was estimated at 5 lines (ceil(73/18)) even though real word-wrap would produce fewer lines for text with normal word lengths.
- **Fix**: Rewritten with word-wrap simulation — splits by spaces, accumulates words per line, wraps when adding a word would exceed `charsPerLine`. Single long words that exceed the column width still occupy one line (matching PowerPoint behaviour).
- **Visibility change**: Method changed from `private` to `internal` for testability.

### Fixed — Shell scripts lack execute permission on macOS (Issue 3, P1)

- **Root cause**: `.sh` scripts committed without `+x` from Windows. Every macOS first-run fails with "Permission denied".
- **Fix**: Added self-healing `chmod +x "${SCRIPT_DIR}"/*.sh` to `build-and-validate.sh` so sibling scripts are automatically made executable on first invocation.

### Fixed — Export script `len(doc)` after `doc.close()` (Issue 5, P2)

- **Root cause**: The macOS LibreOffice+PyMuPDF export script in `qa-workflow.md` called `len(doc)` after `doc.close()`, causing a runtime error.
- **Fix**: Capture `page_count = len(doc)` before `doc.close()`.

### Added — Batched QA guidance for large decks in `qa-workflow.md`

- For decks with 10+ slides, dispatch multiple QA subagents in parallel (6-8 slides each) instead of a single agent that may crash on memory limits. Addresses QA subagent failure observed in session.

### Changed — Documentation updates

- **`gotchas.md`**: Updated gotcha #11 to reflect new font scale (16pt→14pt instead of 14pt→12pt). Added gotcha #15 (macOS first-run script permissions).
- **`snowflake-template.md`**: Updated `four_column_numbers` column content guidelines — target ~40-55 chars for 16pt, font scale now 16/15/14pt.

### Changed — Test updates

- Updated `FourColumnIcons_ColContentExceedsLineLimit_ProducesWarning` test to use realistic word-wrappable text (was `new string('a', 73)` which the new word-wrap estimator treats as a single unwrappable token). 63 tests passing.

---

## [1.5.0] — 2026-03-23

Validator hardening from generation session `05ccd29b` review (AI Analytics Head-to-Head deck): tightened character limits for titles, subtitles, and divider slides based on visual overflow evidence. Added title line-wrap detection, headshot subtitle limit, and fixed a false-positive agenda validation quirk. 63 tests passing.

### Changed — Tightened title character limits in Validator.cs

- **Content slide title**: 60 → 45 chars. Titles over ~40 chars wrap to 2 lines and overlap the subtitle at the content slide title font size.
- **Column slide titles** (`two_column_titled`, `three_column_titled`, `three_column_icons`, `four_column_numbers`, `four_column_icons`): 60 → 50 chars.
- **Divider titles** (`section`, `chapter_particle`): 60 → 40 chars. Divider slides are visual breaks — titles should be ~5 words max.
- **Divider subtitles** (`section`, `chapter_particle`): 100 → 60 chars. Subtitles should be short phrases (~8 words), not full sentences.

### Added — `title_headshot` subtitle limit in Validator.cs

- New entry in `ContentLimits` for `title_headshot` with subtitle max 50 chars (headshot photo reduces available horizontal width). Addresses slide 1 subtitle/headshot overlap reported by user.

### Added — Title line-wrap check in Validator.cs

- New warning when a content slide title would wrap to 2+ lines at ~30 chars/line (title font size). Estimates line count using the existing `EstimateLineCount` method. Catches the exact issue that required slide 14 to be regenerated mid-session.

### Fixed — Agenda `items` false-positive scalar length check

- **Root cause**: `ValidateSlideLimits` called `CheckStringLength` on the `items` field's serialized list representation (e.g. "System.Collections.Generic.List..."), triggering a spurious "exceeds 40 char limit" warning even when all individual items were within limits.
- **Fix**: Skip `CheckStringLength` when the field resolves to a list rather than a scalar string. Individual item length checks via `CheckListField` are unaffected.

### Changed — Documentation updates

- **`gotchas.md`**: Added 3 new entries: #12 (title slide subtitle overlaps headshot at >50 chars), #13 (divider subtitles max 60 chars / 8 words), #14 (content slide titles wrap at ~40-45 chars).
- **`snowflake-template.md`**: Updated content limits table to reflect new per-type title and subtitle limits. Added divider design intent note: "Divider slides are visual pauses. Titles max 40 chars. Subtitles max 60 chars — short phrases, not sentences."

### Added — Test coverage (13 new tests, 63 total)

- `Content_TitleExceeds45Chars_ProducesWarning` / `Content_TitleAt45Chars_NoCharLimitIssue` — new 45-char limit boundary tests.
- `ColumnSlide_TitleExceeds50Chars_ProducesWarning` / `ColumnSlide_TitleAt50Chars_NoCharLimitIssue` — new 50-char limit.
- `Section_SubtitleExceeds60Chars_ProducesWarning` / `Section_SubtitleWithin60Chars_NoWarning` — new divider subtitle limit.
- `ChapterParticle_SubtitleExceeds60Chars_ProducesWarning` — divider subtitle limit.
- `Section_TitleExceeds40Chars_ProducesWarning` — divider title limit.
- `Content_TitleWrapsTo2Lines_ProducesLineWrapWarning` / `Content_ShortTitle_NoLineWrapWarning` — title line-wrap detection.
- `TitleHeadshot_SubtitleExceeds50Chars_ProducesWarning` / `TitleHeadshot_SubtitleWithin50Chars_NoWarning` — headshot subtitle limit.
- `Agenda_ItemsListDoesNotTriggerScalarLengthCheck` — agenda quirk fix verification.

---

## [1.4.0] — 2026-03-22

Enhancements from generation session `e51ed136` review (Xbox vs PlayStation deck): slide variety enforcement, inline character budgets, strengthened QA gate, and hook-level warnings for capitalization and layout variety. 50 tests passing.

### Added — Slide variety validation in Validator.cs

- New warning when `content` type exceeds 30% of total slides (only for decks with 6+ slides).
- New warning for each consecutive `content` slide beyond the 2nd in a run, prompting the agent to insert a visual break (column layout, quote, split, etc.).

### Added — Non-blocking warnings in `yaml-slide-type-check.py` hook

- **Capitalization warnings**: Detects `chapter_particle` and `section` slides with non-uppercase titles/subtitles. Emits `additionalContext` (not a block) advising ALL CAPS.
- **Variety warnings**: Detects when `content` type exceeds 30% of slides or when more than 2 consecutive `content` slides appear. Non-blocking so the agent sees the guidance at write time.

### Changed — Strengthened QA phase in `/snowflake-pptx` command

- Phase 3 renamed to "QA & Deliver (MANDATORY -- DO NOT SKIP)" with explicit 7-step checklist: validate-spec, build, validate, export slides, launch QA subagent, fix-and-rerun loop, deliver only after PASS.
- Added "DO NOT deliver a PPTX that has not been through visual QA" as a bolded hard gate.

### Added — Inline character budget table in `/snowflake-pptx` command

- Added a quick-reference table with hard limits for all column, quote, split, and agenda fields directly in the spec generation instructions. Includes the 80% margin guideline and narrow-column line-count heuristic (~23 chars/line x 3 lines = ~69 chars target).

### Added — Title slide default preference

- New rule: use `title` (not `title_wave`) as the default opening slide when no presenter headshot is available. `title_wave` only when explicitly requested.

### Added — Divider slide capitalization rule in command

- New content quality rule: `chapter_particle` and `section` slides MUST use ALL CAPS for both title and subtitle, with good/bad examples.

### Added — Agenda item length examples

- Added max 40 chars guidance with concrete good/bad examples to the Agenda Slide Guidance section.

### Fixed — `validate.sh` missing wslpath conversion

- Added `wslpath -w` conversion for `DOTNET_DLL` when using Windows `dotnet.exe` from WSL, matching the existing behavior in `build.sh` and `validate-spec.sh`.

---

## [1.3.0] — 2026-03-22

Reliability and correctness fixes from generation session `04672f25` review: 9 issues addressed across font sizing, capitalization, agenda parsing, validation, QA workflow, and documentation. All changes include new test coverage (50 tests passing).

### Fixed — `four_column_numbers` degenerate font table (Issue 9, P0)

- **Root cause**: Content font table was flattened to 14pt for all lengths (a quick hack from a prior session), removing graceful degradation for longer text.
- **Fix**: Restored proper sliding scale: <=55 chars -> 14pt, 56-69 -> 13pt, 70-80 -> 12pt, >80 -> 12pt floor. Validator already allows up to 90 chars at 23 chars/line.
- **Consolidated number sizing**: Replaced inline `ApplyFourColumnNumbersNumberFontSize` (55-line method) with centralised `NumberSizing.ApplyNumberSizing`. Updated `NumberThresholds` to battle-tested values: `(4,36pt) (5,28pt) (6,24pt) (8,18pt) (>8,14pt)`.
- **Removed dead code**: 4 stale `four_column_numbers` entries in `AutofitManager.RunSizeOverrides` that were never called and would conflict if activated.

### Fixed — `chapter_particle` titles not uppercased (Issue 2, P1)

- **Root cause**: `ContentInjector.BuildContentMap` only auto-uppercased titles/subtitles for `section` and `section_dots`, not `chapter_particle`. The template hint says "CH. TITLE, ALL CAPS" but the code didn't enforce it.
- **Fix**: Added `"chapter_particle"` to both the title and subtitle uppercase conditions in `ContentInjector.cs`.
- **Validator warning**: New warning when divider slides (`section`, `section_dots`, `chapter_particle`) have mixed-case titles, advising to write them in ALL CAPS.

### Fixed — Agenda slide silent data loss on two-key dict format (Issue 3, P1)

- **Root cause**: `HandleAgendaItems` required exactly 1 key per dict item. The common format `{title: "...", subitems: [...]}` (2 keys) was silently skipped, leaving the agenda slide with template placeholder text.
- **Fix**: Added fallback parsing for multi-key dicts: if `d.Count != 1`, tries to extract a `title` key. Both single-key (`"Title": {subitems: [...]}`) and explicit-title (`{title: "...", subitems: [...]}`) formats now work.
- **Validator**: New `CheckAgendaItems` method warns on multi-key format (accepted but not preferred) and errors on dicts with no `title` key.

### Added — Mandatory QA phase in SKILL.md (Issue 1, P0)

- Added explicit "Mandatory QA Phase" section with 5-step checklist: export slides, verify images, dispatch QA subagent, fix issues, re-verify.
- Added column content limits quick reference table for all 4 column slide types.
- Marked `build-and-validate.cmd` as the primary recommended command; demoted `build.cmd` to "build only".
- Added platform detection note: always use `.cmd` wrappers on Windows.

### Added — `build-and-qa.cmd` wrapper script (Issue 1)

- Full pipeline: validate-spec -> build -> validate -> export slides to `temp\qa\` -> QA reminder. Chains all steps with error checking at each stage.

### Fixed — `export-slides.ps1` parameter name mismatch (Issue 7)

- Added `[Alias('OutputDir')]` to the `-OutDir` parameter so both `-OutDir` and `-OutputDir` are accepted.

### Fixed — WSL path resolution for shell scripts (Issue 6)

- `validate-spec.sh` and `build.sh` now convert DLL and template paths via `wslpath -w` when using Windows `dotnet.exe` from WSL.

### Changed — Documentation updates (Issues 2, 3, 4, 5, 8, 9)

- **`gotchas.md`**: Upgraded item 3 to "NEVER parallel-edit spec.yaml" with stronger wording. Added items 9 (divider capitalization), 10 (agenda single-key dict format), 11 (four_column_numbers dynamic font sizing).
- **`qa-workflow.md`**: Promoted "3+ errors -> full rewrite" rule to bold critical block at top of Fixing Validation Errors section.
- **`snowflake-template.md`**: Added auto-uppercase note for all divider slide types. Updated `four_column_numbers` limits to 30-90 chars with font scale documentation. Added agenda item format examples showing both single-key and explicit-title formats.

### Added — Test coverage for all enhancements

- 7 new validator tests: divider slide capitalization warnings (3), agenda item format validation (3), line-count test fix for updated CharsPerLine (1).
- New `NumberSizingTests.cs`: 9-case theory for threshold values, 2 symmetric sizing tests, 1 threshold verification test.
- All 50 tests passing (Debug + Release).

---

## [1.2.0] — 2026-03-21

Creative renderer QA fixes: resolved 14 issues from QA session `77eacdcb` across visual rendering, content density, tooling, and documentation. All layout fixes include new validator warnings and test coverage.

### Fixed — Timeline text truncation (P0)

- Increased horizontal margin from 0.8" to 1.2" and reduced column width cap from 2.8" to 2.5" (factor 0.9→0.85).
- Text box X positions clamped to `[0.3, SLIDE_W - 0.3]` to prevent off-slide overflow.
- Body font size increased from 12pt to 13pt for legibility.

### Fixed — Section divider sparse subtitle (P1)

- **Diagonal variant**: Subtitle font size 16→22pt, moved from y:3.0 to y:2.5, height 0.7→1.5", added `valign: 'middle'`.
- **Side-panel variant**: Subtitle font size 16→20pt, height 0.7→1.2", added `valign: 'middle'`.

### Fixed — Icon-grid blank banner when no icon (P1)

- Banner height is now conditional: 0.65" with icon, 0.20" without. Previously all cards rendered the full accent banner even when no icon was present.

### Fixed — Comparison sparse layout (P2)

- Dynamic bullet spacing: 0.75" for ≤3 bullets, 0.58" for 4+.
- Dynamic font size: 16pt for ≤3 bullets, 14pt for 4+.
- Removed `shrinkText: true` which could make text unreadably small.

### Fixed — Agenda sparse layout (P2)

- Dynamic row height: 0.95" for ≤4 items, 0.85" for 5, 0.65" for 6.
- Dynamic font size: 20pt for ≤4 items, 16pt for 5+.
- Dynamic circle size: 0.48" for ≤4 items, 0.38" for 5+.

### Fixed — Columns bordered sparse two-column layout (P2)

- Body font size increased from 14pt to 16pt when bordered variant has ≤2 columns.

### Added — Content-density warnings in validator

- Timeline: warns when step body exceeds 50 characters.
- Icon-grid: warns when an item has no `icon` field.
- Comparison: warns when both panels have ≤2 bullets.
- Columns: warns when all items in a 2-column layout have body text under 80 characters.
- Agenda: warns when there are ≤3 items.

### Added — Wrapper scripts for creative builds (P0)

- `creative-build.cmd` (Windows) and `creative-build.sh` (macOS/Linux) auto-install `node_modules` and forward arguments to the Node.js entry point. Avoids cmd.exe path failures with `&&` chaining.

### Added — `pptx-inspect.js` test helper

- Lightweight PPTX structural inspector that parses ZIP central directory to verify magic bytes, `[Content_Types].xml`, `ppt/presentation.xml`, and slide count — no external dependencies.

### Changed — Documentation updates

- `gotchas.md`: Added 3 new creative gotchas (cmd.exe path failures, glob patterns for exported assets, character limit headroom).
- `qa-workflow.md`: Added creative-specific QA checklist section (7 items covering timeline, section, agenda, icon-grid, comparison, columns, and content density).
- `snowflake-pptx-creative.md`: Updated build/validate commands to reference wrapper scripts; added icon-grid best practice note about always including icons.

### Added — Test coverage for all enhancements

- 5 new validator tests for content-density warnings.
- 5 new renderer smoke tests for layout fixes (timeline long body, section diagonal subtitle, agenda 3 items, icon-grid without icons, comparison 2 bullets).
- 8 PPTX output quality tests using `pptx-inspect.js` for structural verification across fixture files.

---

## [1.1.0] — 2026-03-21

Skills architecture overhaul: fixed critical hook bug blocking `title_headshot` workflows, resolved content limit inconsistencies, deduplicated 124+ lines of shared content into reference files, and slimmed SKILL.md from 313 to ~77 lines.

### Fixed — `title_headshot` and `speaker_headshots` blocked by validation hook (R0, Critical)

- **Root cause**: `yaml-slide-type-check.py` listed `title_headshot` and `speaker_headshots` in `REMOVED_TYPES`, blocking any YAML spec that followed the documented "Slide 1 is always `title_headshot`" workflow. All three documentation files contradicted the hook.
- **Fix**: Moved both types to `VALID_TYPES` (20 → 22 types). Updated hook comments and blocking message to say "22 valid slide types".
- Also fixed `Validator.cs`: added `title_headshot` to the first-slide structural check (was warning on valid `title_headshot` as first slide) and to `SingletonTypes`.

### Fixed — Content limit inconsistencies across documentation (R1)

- `snowflake-pptx.md` `quote_text` limit was 300 chars; `Validator.cs` enforces 200. Fixed to 200.
- `snowflake-pptx.md` `title` limit was 80 chars; `Validator.cs` enforces 60 for most types. Fixed to 60.
- `snowflake-pptx.md` `subtitle` limit was "80-120 chars"; `Validator.cs` enforces 80. Fixed to 80.
- `snowflake-template.md` limits table updated to match `Validator.cs` actual values.
- Replaced the full content limits table in `snowflake-pptx.md` with a reference to `snowflake-template.md` as the single source of truth.

### Changed — Build instructions use wrapper scripts (R2)

- `snowflake-pptx.md` now documents `.cortex\skills\pptx\scripts\build.cmd` / `build.sh` as the primary build method instead of raw `SNOWFLAKE_TEMPLATE_PATH=... dotnet ...` invocations.
- README updated to show wrapper scripts in the build section.

### Added — Shared reference files under `.cortex/skills/pptx/references/` (R3, R4)

Extracted 124+ lines of duplicated content from both command files into 8 reference files:

- `research-workflow.md` — capped agent search + Glean MCP check
- `speaker-notes-requirements.md` — speaker notes format and minimum requirements
- `config-schema.md` — shared config file handling and field reference
- `qa-workflow.md` — QA process, image export (Windows + macOS), verification loop
- `gotchas.md` — operational pitfalls from past sessions (7 pixel-perfect, 5 creative)
- `design-ideas.md` — color palettes, typography, layout guidance (moved from SKILL.md)
- `research-agent-prompt.md` — formalized research subagent prompt template
- `qa-agent-prompt.md` — formalized visual QA subagent prompt template

Both command files now reference these files instead of duplicating content.

### Changed — SKILL.md slimmed to routing + essentials (R5, R6)

- Narrowed trigger description from "Use this skill any time a .pptx file is involved in any way" to Snowflake-specific triggers only.
- Reduced SKILL.md from ~313 lines to ~77 lines: routing (which command to use), build quick reference, slide type list, and reference file pointers.
- Moved "Design Ideas" section (87 lines) to `references/design-ideas.md`.
- Moved "QA (Required)" and "Converting to Images" sections (147 lines) to `references/qa-workflow.md`.

### Added — `allowed-tools` in command frontmatter (R7)

- Both `snowflake-pptx.md` and `snowflake-pptx-creative.md` now declare `allowed-tools` for auto-approval of tools needed during deck generation (Read, Write, Edit, Glob, Grep, Bash, WebSearch, WebFetch, Agent).

### Changed — Creative QA export unified on `export-slides.ps1` (R8)

- Replaced the inline PowerShell COM snippet in `snowflake-pptx-creative.md` with a reference to the shared `export-slides.ps1` script, which handles COM timing issues and process cleanup.

---

## [1.0.0] — 2026-03-20

First stable release. Two rendering engines — C# template cloner (.NET/OpenXML SDK, 20 slide types) and Node.js creative generator (pptxgenjs, 14 layouts with variants) — driven by a single YAML spec workflow. 150 SVG brand icons, 35 JPG photos, 5 accent colors, 3 moods, and full speaker notes with clickable hyperlinks.

### Changed — SVG asset catalog consolidated (240 → 150 files)

- **Trimmed `assets/svg/`** from 240 to 150 SVGs. Removed `graphic_*`, `icon_nav_*`, and `icon_*` prefixed files; replaced with 149 kebab-case icons (e.g., `data-engineering.svg`, `analytics.svg`, `secure-data.svg`) plus the Snowflake wordmark logo (`graphic_snowflake_logo_blue.svg`).
- **Updated all references** across 12 files: 3 template test fixtures, 5 creative test fixtures, 2 command definitions, 1 skill doc, and the asset README.

### Fixed — `title_customer_logo` squashes Snowflake logo

- **Root cause**: `InjectCustomerLogo()` swapped the image bytes but never adjusted the shape dimensions. The template placeholder shape had a different aspect ratio than the logo image (4.5:1 wide wordmark), causing the logo to be squashed vertically to fit the placeholder bounds.
- **Fix**: Added `AdjustShapeToImageAspectRatio()` in `ContentInjector.cs` — called after image swap, it reads the image's pixel dimensions (supports PNG, JPEG, GIF, SVG), computes a fit-to-box scale preserving aspect ratio, updates the xfrm `ext` (cx/cy) and re-centers horizontally via `off` (x).
- **Key insight**: xfrm walk-up must search grandchildren (structure is `p:pic > p:spPr > a:xfrm`), not just direct children of each ancestor.
- Added `GetImagePixelSize()` — zero-dependency image dimension parser handling PNG (header bytes 16–23), JPEG (SOF0/SOF2 markers), GIF (bytes 6–9), and SVG (viewBox regex or width/height attributes).

### Fixed — Creative `columns` cards variant overlaps subtitle

- **Root cause**: `availableTop` was set to `1.1` inches when a subtitle was present, but subtitle occupies y=0.9 + h=0.4 = 1.3 inches. Cards starting at 1.1 overlapped the subtitle by 0.2 inches.
- **Fix**: Changed `availableTop` from `1.1` to `1.35` in `columns.js` when `slide.subtitle` is present.

### Added — Headshot support in creative `title` layout

- Creative title slides now support an optional `photo` field for a circular presenter headshot.
- `getSvgAspect()` moved to `brand.js` and `LOGO_H` computed dynamically from SVG aspect ratio.
- Warning emitted when headshot file is not found (instead of silent failure).

### Fixed — Creative layout variant issues

- **Side-panel subtitle**: moved subtitle up and added accent line for visual anchoring.
- **Bordered columns**: bar start position moved below header area to avoid title overlap.

### Added — Test fixtures for layout and content boundaries

- Added multiple YAML fixtures exercising max-content, long-deck, and edge-case scenarios across both engines.

### Fixed — `title_headshot` presenter name, title, and company not populated

- **Root cause**: `TemplateMappings.cs` was missing text patterns for `name`, `speaker_title`, and `company` in the `title_headshot` mapping. `HandleTitleHeadshotSlide` already handled replacement for these fields, but with no patterns defined it had nothing to match against, leaving "Firstname Lastname", "Title", "Company" as literal placeholder text.
- **Fix**: Added three text patterns to the `title_headshot` SlideMapping: `["name"] = "Firstname Lastname"`, `["speaker_title"] = "Title"`, `["company"] = "Company"`. All three fields remain optional in the YAML spec; when absent, `DeleteUnfilledPlaceholders` removes the shapes.

### Fixed — `four_column_numbers` content descriptions rendering at 9pt

- **Root cause**: `ApplyFourColumnNumbersContentFontSize` had a font table with a 9pt floor for content exceeding 55 chars, making descriptions unreadable when text reached 56+ chars.
- **Fix**: Replaced the font table to allow 2-line wrapping with a 10pt floor: ≤25 chars → 14pt, 26–40 → 13pt, 41–55 → 12pt, 56–70 → 11pt, >70 → 10pt.

### Added — `ApplyContentBulletFontScale` for content slide bullet overflow

- Added `ApplyContentBulletFontScale` method in `ContentInjector.cs` that applies pre-emptive `fontScale` and `lnSpcReduction` to the body shape's `normAutofit` element based on total bullet character count: ≤300 chars → no override, 301–450 → 85%/9999, 451–600 → 75%/14999, >600 → 65%/19999.

### Changed — Skill docs updated for title_headshot fields and content limits

- **`snowflake-pptx.md`**: Added `name`, `speaker_title`, `company`, `photo` field guidance for `title_headshot` slides. Tightened `col*_content` limit to "40–70 for `four_column_numbers`". Added note that 5+ bullets or bullets over 120 chars trigger automatic font scaling.
- **`snowflake-template.md`**: Added `title_headshot` key fields row. Updated `four_column_numbers` `col_content` limit. Added bullet font scaling note.

### Added — Clickable hyperlinks in speaker notes (C# renderer)

- `WriteNotesSlide()` in `ContentInjector.cs` rewritten to emit multi-paragraph, multi-run XML. URLs matching `https?://[^\s<>"]+` get proper `AddHyperlinkRelationship` relationships and `<a:hlinkClick>` runs — clickable in PowerPoint's Notes view.

### Added — Thin-notes validator warning

- Content slides with notes shorter than 200 chars now emit a warning. `NotesMaxLength` raised from 2000 → 4000 to accommodate verbose notes.

### Changed — Tightened speaker notes prompt in command files

- Every content slide must have ≥ 500 chars of notes (non-negotiable); target 1500–2500 chars.
- A `References:` block is required on every content slide with at least one entry.
- Pre-YAML checklist added: verify every content slide has notes + a `References:` section before writing the spec.

### Fixed — `title_headshot` text rendering with wrong colors

- **Root cause**: `ReplaceShapeTextByPattern` was writing injected text into `tElems[0]` (the first `<a:t>` run), discarding the template's `<a:rPr>` run properties (which carry the white color) that live on the last run.
- **Fix**: Changed the replacement target to `tElems[^1]` (last run) so the template's white `<a:rPr>` is preserved.

### Fixed — `four_column_numbers` big number values wrapping to two lines

- **Root cause**: No dynamic font sizing existed for the number shapes; long values like "60–70%" at 36pt exceeded the column width.
- **Fix**: Added `ApplyFourColumnNumbersNumberFontSize` helper with a char-count lookup table: ≤4 chars → 36pt, 5 → 28pt, 6 → 24pt, 7–8 → 18pt, >8 → 14pt.

### Fixed — `speaker_headshots` unfilled slots and circle-4 rendering

- **Root cause**: Unfilled circle shapes were never removed (only `txBody` shapes processed). `EvenSpaceHeadshotCircles` used wrong slide width (16:9 instead of Snowflake template).
- **Fix**: Rewrote `ReplaceSpeakerHeadshotsPatterns` with 4-phase lifecycle: collect → inject → remove unfilled → reposition. Works correctly for 1–4 speakers.

### Fixed — `speaker_headshots`, `two_column`, and `title_headshot` slides missing from template

- The January 2026 template was reduced from 24 to 20 slides, removing these types. Inserted three slides from the prior template. `SlideCloner.cs` `TemplateSlideCount` updated to 23. `TemplateMappings.cs` updated with new indices.

### Fixed — `quote_photo` quote text anchored to top instead of centre

- Removed the `HandleQuotePhotoBodyAnchor()` call that was overriding the template's existing `anchor="ctr"`.

### Fixed — `two_column` right column content overwriting left column

- Both `body_left` and `body_right` had identical lorem ipsum. `BuildContentMap` now skips these entries for `two_column`; new `ReplaceTwoColumnBodyPatterns()` assigns content by x-sorted position.

### Fixed — `four_column_numbers` label shapes not found by Y threshold

- Lowered `BodyYThreshold` from 1,371,600 to 1,200,000 EMU.

### Fixed — `three_column` content not injected

- Added missing `case "three_column":` in `HandlePositionalReplacements()` switch.

### Fixed — Pixel-perfect visual QA (bulk fix)

- **C1**: Agenda items 4–5 rendered as sub-bullets. Strip sub-bullet paragraphs before replacing.
- **C2**: `three_column` body shapes not found. Lowered `BodyYThreshold` to 900,000.
- **C3**: `four_column_numbers` col1 number label lower than cols 2–4. Normalise heights to `minH` and strip extra leading empty paragraphs.
- **C4**: `speaker_headshots` name/title shapes not found by positional lookup. Updated position thresholds.
- **M1**: Customer logo not vertically centred. Rewrote to walk all descendants checking for `r:embed`.
- **M5**: `three_column_icons` col content overwritten across all three columns. Removed `break`; target pattern-bearing `<a:t>` run directly.
- **Table width overflow**: Reads actual slide width at runtime from `PresentationPart.Presentation.SlideSize.Cx`.
- **`quote_simple` attribution overlapping footer**: Strip `<p:ph>` element before setting Y position.
- **`quote_photo` quote text anchored to bottom**: Set `bodyPr.Anchor = Top`.
- **Content/column slides using `normAutofit` with few bullets**: Use `<a:noAutofit/>` for ≤ 3 paragraphs.
- **`three_column_icons` character limit**: `col_content` `MaxLength` reduced from 80 to 60.

### Changed — Template reduced from 24 to 20 slides

- Removed `two_column`, `speaker_headshots`, `title_headshot`, and backing slides. Reindexed all 20 remaining types in `TemplateMappings.cs`.

### Changed — Validator cleanup and new rules

- Removed dead types from `ContentLimits`, `ContentSlideTypes`, `SubtitleRequiredTypes`.
- Added table row density warning (>5 data rows).
- Added code-block detection (triple-backtick fences).

### Added — YAML spec slide type validation hook

- `yaml-slide-type-check.py` PostToolUse hook blocks YAML specs containing invalid or removed slide types. Gives targeted replacement advice.

### Added — Cross-file consistency test

- 15 pytest tests verifying that `TemplateMappings.cs`, `Validator.cs`, `yaml-slide-type-check.py`, `SKILL.md`, `snowflake-template.md`, `snowflake-pptx.md`, and `SlideCloner.cs` all agree on the same 20 slide types.

### Fixed — Config file never read during PPTX generation

- All 14 path references pointed to `~/.cortex/snowflake-pptx.yaml` instead of the canonical `~/.snowflake/cortex/snowflake-pptx.yaml`.

### Fixed — Glean MCP search tool never invoked during research

- Two-layer enforcement: preflight hook injects `GLEAN MCP REMINDER` into every PPTX prompt; command file has a **MANDATORY CHECK** block.

### Fixed — Postbuild hook not registered in settings.json

- Wired into `.cortex/settings.json`. Fixed f-string bug where `{stem}` was a literal string.

### Changed — Asset directory cleanup

- Removed `gif/` (unused), `styles/` (unused), 13 unreferenced SVGs.
- `AssetCatalog.cs` `AssetSubdirs` reduced from 4 to 3 (`svg`, `png`, `jpg`).

### Fixed — Title slide title text positioned too high

- PlaceHolder 1 had `anchor="b"` (bottom). Set to `anchor="ctr"`.

### Fixed — Number label vertical misalignment on `four_column_numbers`

- Added `TopAlignNumberLabels()` — sets `anchor="t"` on all four label shapes.

### Fixed — Agenda slide bullet hierarchy causing misalignment

- Removed indentation logic — all agenda items now render as flat top-level bullets.

### Fixed — Empty table columns not removed

- Added `RemoveEmptyTableColumns()` that detects all-empty columns, removes elements, and redistributes widths.

### Changed — Title character limit reduced from 80 to 60

- Long titles on content slides wrapped and overlapped subtitles.

### Added — Subtitle required validation

- 12 slide types now emit a warning when subtitle is missing.

### Added — Post-build hook for spec YAML enforcement

- Checks if a spec YAML file exists alongside the generated PPTX.

### Fixed — Split slide `content` text missing

- Template has `content_title` and `content` in the same shape as different paragraphs. Added `HandleSplitContent()` that replaces at the paragraph level.

### Added — Cortex Code hooks registration

- `UserPromptSubmit` → `snowflake-pptx-preflight.py`; `PostToolUse` → `yaml-emdash-check.py`.

### Changed — Consolidated `.cortex/` directory structure

- Eliminated duplicate source in `commands/pptx/`. All content now lives under `skills/pptx/`. Resolved template drift between the two copies.

### Added — `title_headshot` slide type (slide 24)

- Blue title cover with circular presenter headshot, name/title/company text. Full `HandleTitleHeadshotSlide()` and `ReplaceFirstSquareBlip()` implementation.

### Fixed — `two_column` body text collapsing to ~6pt

- Added `two_column` body shapes to `SpAutofitPreserve`.

### Fixed — `four_column_icons` broken by updated template layout

- Removed `HeaderYThreshold` guard, switched title pattern matching to `StartsWith`, removed 16pt font cap, preserved `spAutoFit` on 8 shapes.

### Fixed — Stretched icons on column icon slides

- `InjectColumnIcons()` now preserves aspect ratio with `GetImageDimensions()` helper and centers within bounding box.

### Fixed — "99.9%" number spilling to next line

- Reduced big-number font from 44pt to 40pt in template slide 13.

### Fixed — Quote text overlapping quotation marks

- Reduced quote font from 42pt to 28pt in template slide 17.

### Added — Icon injection for column icon slides

- `InjectColumnIcons()` replaces template `custGeom` placeholders with actual SVG/image files. Works for both `three_column_icons` and `four_column_icons`.

### Added — Positional handlers for content, agenda, and table slides

- `HandleContentBullets()`, `HandleAgendaItems()` (with nested subitems), `HandleTableData()` + `SetTableRowCells()`.

### Fixed — Unfilled placeholder deletion

- Checks concatenated shape text against patterns (not individual `<a:t>` runs).

### Added — C# .NET implementation

- **`PresentationBuilder`** — full C# port of Python `PPTXBuilder`. Clones slides from template, injects content, auto-appends `thank_you`, sets document properties.
- **`SlideCloner`** — deep-copies slide parts with rId remapping for all `r:embed`, `r:link`, and `r:id` attributes.
- **`ContentInjector`** — two-pass text replacement (run-level → shape-level), positional matching for multi-column slides, `normAutofit` + `fontScale` overrides, unfilled-placeholder cleanup.
- **`AutofitManager`** — per-shape font-scale and line-spacing-reduction overrides.
- **`AssetCatalog`** — resolves image references: absolute path → CWD-relative → asset-directory search → keyword stem search.
- **`TemplateMappings`** — covers all verified slide types with exact text patterns, shape names, and preservation rules.
- **`SlideSpec` / `SlideSpecDeserializer`** — YAML deserialization via `YamlDotNet`.
- **`Validator`** — wraps `OpenXmlValidator` for post-build OOXML validation.
- **`Program.cs`** CLI: `build`, `validate`, and `render` subcommands.

### Added — Node.js creative engine

- **14 layouts**: `title`, `section`, `agenda`, `columns`, `stat-grid`, `split`, `full-bleed`, `quote`, `table`, `thank-you`, `timeline`, `icon-grid`, `comparison`, `closing`.
- **Variants**: `cards`, `numbered`, `bordered` (columns); `diagonal`, `side-panel` (section); `accent-bg`, `inline` (stat-grid).
- **3 moods**: `bold`, `clean`, `dark`.
- **5 accent colors**: `snowflakeBlue`, `starBlue`, `valenciaOrange`, `firstLight`, `purpleMoon`.
- SVG icon support with aspect-ratio preservation.
- Full YAML validation with per-layout required field checks.

### Fixed — Slide dimensions

- Removed `SetSlideSize()` that unconditionally overwrote template dimensions. Template's native `sldSz` (10" × 5.625") is preserved.

### Fixed — Image injection (customer logo / speaker headshots)

- `a:blip` elements are `OpenXmlUnknownElement` in OpenXML SDK 3.x. Both `InjectSpeakerPhotos` and `InjectCustomerLogo` now iterate untyped descendants and detect blips by `r:embed` attribute.

---

## [0.1.0] — 2026-02-28

### Added — Python implementation (initial)

- **`PPTXBuilder`** — build orchestrator. Accepts a YAML/dict spec, clones slides from the Snowflake January 2026 template (23 slide types verified), injects content, and returns PPTX bytes.
- **`XMLSlideCloner`** — Python slide cloner using `python-pptx` + `lxml`. Handles image/media relationship copying, bullet arrow formatting, and icon aspect-ratio preservation.
- **`template_mappings.py`** — 23 verified slide-type definitions with exact text patterns, shape names, and preservation rules.
- **`text_fitting.py`** — pre-emptive font-size calculation before text insertion.
- **`asset_catalog.py`** — asset resolution and SVG-to-PNG rasterization (Pillow fallback when `cairosvg` is absent).
- **`yaml_generator.py`** — `generate_yaml_spec()` helper; builds a well-formed spec dict with safe-harbor and thank-you slides.
- **`schema.py`** — content-limit validation and auto-truncation.
- **`validator.py`** — post-build PPTX validation (placeholder leakage, broken images).
- **`branding.py`** — co-branding configuration (customer logo, region, event presets).
- Template: **Snowflake January 2026** (23 slides, 10" × 5.625").
- Assets: 51 SVG icons, 34 JPG photos.
- Tests: `test_font_sizes.py`, `test_slide_types.py`.

### Fixed — Bullet formatting

- Extra paragraphs cloned from the last template paragraph inherited `buChar` instead of `buBlip` (arrow bullet). Fix captures `buSzPct` + `buBlip` from the first level-0 paragraph and re-applies them to all overflow paragraphs.

### Fixed — Icon aspect ratio

- Icons were stretched to fill the template bounding box. Fix preserves aspect ratio and centers the icon within the original bounds.
