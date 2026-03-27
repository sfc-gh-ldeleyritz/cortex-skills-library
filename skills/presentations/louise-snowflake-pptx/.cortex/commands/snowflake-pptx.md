---
name: snowflake-pptx
description: "**[USER-INVOCABLE]** Create Snowflake-branded PowerPoint presentations with researched content. Use this skill whenever the user wants to create a Snowflake presentation, deck, or slides: including customer pitches, partner decks, internal enablement, event talks, data cloud presentations, or any Snowflake-branded .pptx. Also triggers for: snowflake pptx, snowflake slides, snowflake pitch deck, customer presentation for Snowflake, Snowflake template deck. Use /snowflake-pptx or $snowflake-pptx to invoke directly."
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - Bash(dotnet *)
  - Bash(powershell *)
  - Bash(mkdir *)
  - Bash(ls *)
  - Bash(git checkout *)
  - WebSearch
  - WebFetch
  - Agent
---

# Snowflake PPTX Generator

Create pixel-perfect Snowflake-branded PowerPoint presentations backed by real research.

This skill uses the Snowflake January 2026 template (22 slide types) to produce professional decks. Every presentation is researched via web search (and Glean MCP when available), and speaker notes include source references.

---

## Workflow

### Phase 1: Gather Requirements

#### Config File

Read `.cortex/skills/pptx/references/config-schema.md` for the full config file schema and requirements. Read the config file as your first action.

Ask the user these questions using `AskUserQuestion` (skip any already answered by the config
or the user's initial message):

1. **Topic / purpose**: "What is this presentation about?" Offer options relevant to Snowflake:
   - Customer pitch / proof of value
   - Partner overview / co-sell deck
   - Internal enablement / training
   - Event talk (Summit, Build, World Tour)
   - (user can always type something else)

2. **Target audience**: "Who is the audience?"
   - C-suite / executives
   - Technical leadership (CTO, VP Eng)
   - Data engineers / architects
   - Business analysts / data team
   - (user can always type something else)

3. **Source material**: "Do you have any URLs, documents, or key points to include?" Accept URLs, file paths, or freeform text. If the user provides nothing, that's fine: research will fill the gaps.

4. **Presenter**: "Who is presenting?" Accept name and title (e.g. "Jane Doe | Sr. Data Engineer"). If the user declines or says N/A, omit the attribution field from the title slide spec.
   - **Skip entirely** if `presenter_name` + `presenter_title` are already in the config.
   - If `presenter_headshot` is in the config, it is automatically used for the `title_headshot` cover slide and can also be used for `quote_photo` slides.

**Defaults (don't ask, but respect if overridden):**
- **Length**: Medium (10-15 slides), or from `deck_length` in config
- **Safe harbor**: Included after title slide, or from `safe_harbor` in config
- **Output path**: Current directory, auto-named as `snowflake-deck-{topic-slug}.pptx`, or from `output_dir` in config
- **Presenter**: From `.cortex/snowflake-pptx-config.json` if present
- **Company**: From config; used as subtitle prefix on title slide when present
- **Deck length**: From config; overrides medium default
- **Safe harbor**: From config; overrides include-by-default

If the user already provided some of these details in their initial message, don't re-ask: extract the answers from context and confirm.

### Phase 2: Research & Generate

#### Research

Read `.cortex/skills/pptx/references/research-workflow.md` for the full research process (capped agent search, Glean MCP check, source tracking).

For user-provided URLs: use `WebFetch` inline: do not delegate to the agent.

#### Speaker Notes Requirements

Read `.cortex/skills/pptx/references/speaker-notes-requirements.md` for speaker notes format and requirements.

#### Generate the YAML Spec

Build the YAML presentation spec using the slide types from the Snowflake template. Read the pptx skill's [snowflake-template.md](../skills/pptx/snowflake-template.md) for the full slide type reference and field definitions.

**Slide type selection guidance:**

> **Note**: Slide 1 is always the cover (`title_headshot`). Slide 2 is always `safe_harbor`. Slide 3 is always `agenda`. Do not omit these.

| Purpose | Recommended slide types |
|---------|------------------------|
| Legal | `safe_harbor` (always slide 2) |
| Opening | `title_headshot` (default), `title_customer_logo` (co-branded only) |
| Agenda overview | `agenda` |
| Section breaks | `section`, `chapter_particle` |
| Key points / bullets | `content` |
| Stats / KPIs | `four_column_numbers` |
| Feature comparison | `two_column_titled` (use sparingly; prefer 3-column) |
| Solution pillars | `three_column_icons` (preferred), `three_column_titled` |
| Capabilities with icons | `three_column_icons`, `four_column_icons` |
| Architecture / detail | `split` (image + content) |
| Customer quotes | `quote`, `quote_photo`, `quote_simple` |
| Data tables | `table_styled`, `table_striped` (max 5 rows) |
| Closing | `thank_you` |

**Slide type preference (mandatory):**

When choosing multi-column layouts, **always prefer 3- and 4-column types** over 2-column:
- `three_column_icons` > `three_column_titled` > `two_column_titled`
- Use `three_column_icons` whenever icons would enhance the content (SVG icons are injected from the asset catalog, preserving aspect ratio)
- Use `four_column_numbers` for stats/KPIs and `four_column_icons` for categories
- Reserve `two_column_titled` for true binary comparisons (before/after, pros/cons)

**Fixed slide structure (mandatory: applies to every deck regardless of length):**

Every presentation MUST begin and end with these slides in this exact order:

1. **Slide 1: `title_headshot`** — always the cover slide. If `presenter_headshot` is set in config, the headshot photo is injected automatically. If `presenter_headshot` is not set, the slide renders without a photo. Exception: use `title_customer_logo` instead only when a customer or partner logo is explicitly provided by the user.
   - **Required fields**: `title_line1`, `title_line2`, `subtitle`, `date` (standard title fields).
   - **Presenter fields**: `name` (from config `presenter_name`), `speaker_title` (from config `presenter_title`), `company` (from config `company`). These populate the headshot label area. If omitted, the placeholder text is removed automatically.
   - **Photo**: set `photo` to config `presenter_headshot` path (expand `~`). Injected into the circular headshot slot.

2. **Slide 2: `safe_harbor`** — always second, no fields required.

3. **Slide 3: `agenda`**: Agenda / contents overview.

[variable content slides here]

**Last slide: `thank_you`**: Branded closing slide. Always last.

**Title slide default:** When there is no presenter headshot, use `title` (not `title_wave`) as the default opening slide. Only use `title_wave` if the user specifically requests a wave-style title or the deck calls for a more dynamic opening.

**Deck structure guidelines by length:**

- **Short (6-9 slides)**: title_headshot → safe_harbor → agenda → 3-5 content slides → thank_you
- **Medium (10-15 slides)**: title_headshot → safe_harbor → agenda → 2-3 sections with 2-3 content slides each → thank_you
- **Long (20-30 slides)**: title_headshot → safe_harbor → agenda → 4-5 sections with chapter dividers, 3-4 content slides per section → thank_you

**Vary the slide types.** Don't use `content` for every slide. Mix column layouts, stats callouts, quotes, and comparison slides to keep the audience engaged.

**Visual variety rules (mandatory — the validator enforces these):**

The validator groups variable content slides into five categories. Before repeating any slide type, cover as many categories as possible:

| Category | Slide types | Best for |
|----------|-------------|----------|
| **bullet** | `content` | General talking points, narratives |
| **column** | `two_column_titled`, `three_column_titled`, `three_column_icons`, `four_column_numbers`, `four_column_icons` | Comparisons, pillars, stats, features |
| **quote** | `quote`, `quote_photo`, `quote_simple` | Key takeaways, memorable statements |
| **table** | `table_styled`, `table_striped` | Structured data, spec comparisons |
| **visual** | `split` | Product screenshots, image + text |

**Category coverage by deck length:**
- **Short (6-9 slides)**: Use at least 3 categories (bullet + column + quote)
- **Medium (10-15 slides)**: Use at least 4 categories (add table or visual)
- **Long (20-30 slides)**: Use all 5 categories

**Key rules:**
1. **Never repeat a specific type** (e.g., `content` a second time) until you have used at least one slide from the column and quote categories.
2. **Never use 2+ consecutive slides of the same type.** After any slide type, switch to a different one.
3. **Vary within categories.** Don't use `three_column_titled` twice when `three_column_icons` or `four_column_icons` haven't been used yet.
4. **Mix dividers.** When using 3+ section breaks, alternate between `section` and `chapter_particle`.
5. **Stats and KPIs → `four_column_numbers`**: Whenever you have 3-4 quantitative facts (percentages, counts, dollar figures), use a big-numbers layout instead of bullets.
6. **Feature pillars / categories → `three_column_icons` or `three_column_titled`**: When describing 3 parallel concepts (pillars, categories, tiers), use column layouts with icons rather than bullet lists.
7. **Demos / screenshots → `split`**: When describing a visual workflow or showing a product screenshot, use a split slide with the image on one side and text on the other.
8. **Key takeaways / memorable quotes → `quote_simple`**: Extract the single most important sentence from a section and present it as a quote slide between content slides.
9. **Comparisons → `two_column_titled`**: When contrasting two approaches, tools, or options, use a two-column layout.
10. **Aim for at most 30% bullet slides** in any deck. The rest should be visual layouts.

> **IMPORTANT — Write tool usage**: When writing the YAML spec file with the `Write` tool,
> the `content` parameter MUST be a plain YAML string. Do NOT pass a JSON array or content
> block object (e.g. `[{"text": "...", "type": "text"}]`). Pass the raw YAML text directly
> as the string value of `content`.

#### Build the PPTX

After generating the YAML spec, **pause and show it to the user for review**. Proceed to build only after the user approves (or explicitly says to skip review).

**Pre-build check**: Before running the build command, verify the binary exists:
```bash
ls <repo-root>/.cortex/skills/pptx/scripts/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll
```
If missing, restore and rebuild:
```bash
git checkout -- <repo-root>/.cortex/skills/pptx/
dotnet build <repo-root>/.cortex/skills/pptx/scripts/dotnet/SnowflakePptx -c Release
```

**Build using wrapper scripts (recommended):**
```cmd
REM Windows
.cortex\skills\pptx\scripts\build.cmd spec.yaml output.pptx
```
```bash
# macOS/Linux
.cortex/skills/pptx/scripts/build.sh spec.yaml output.pptx
```

> **WARNING:** Do NOT use `set SNOWFLAKE_TEMPLATE_PATH=... && dotnet ...` on Windows cmd.exe — the `&&` chaining corrupts the environment variable. Always use the wrapper script.

**IMPORTANT - Save the spec YAML**: Always save the YAML spec file alongside the output PPTX (e.g., `output-spec.yaml` next to `output.pptx`). This is required for:
- Reproducibility: regenerating the deck with fixes
- Future edits: modifying content without starting over
- Version control: tracking deck changes over time

The postbuild hook will remind you if the spec file is missing.

Validate the output:

```bash
dotnet <repo-root>/.cortex/skills/pptx/scripts/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll \
  validate output.pptx
```

If the .NET project needs to be built first:

```bash
dotnet build <repo-root>/.cortex/skills/pptx/scripts/dotnet/SnowflakePptx -c Release
```

### Phase 3: QA & Deliver (MANDATORY -- DO NOT SKIP)

Read `.cortex/skills/pptx/references/qa-workflow.md` for the full QA process (validation, image export, visual inspection).
Read `.cortex/skills/pptx/references/gotchas.md` for known pitfalls before starting.

You MUST complete ALL of the following before delivering the final PPTX:

1. Run `validate-spec --strict` -- MUST return 0 errors
2. Build the PPTX
3. Run `validate` -- MUST return 0 errors
4. Export slides to images: `powershell -ExecutionPolicy Bypass -File .cortex\skills\pptx\scripts\export-slides.ps1 -PptxPath output.pptx -OutDir temp\qa`
5. Launch a QA subagent to visually inspect ALL exported slide images
6. If QA subagent reports ANY Major or Critical issues: fix the spec, rebuild, re-export, re-run QA
7. Only deliver after QA subagent returns PASS

**DO NOT deliver a PPTX that has not been through visual QA.** Skipping this step is the single most common cause of broken deliverables.

Deliver the final `.pptx` to the user. If fewer than 4 speakers are populated on the `speaker_headshots` slide, include this note immediately after delivering the path:
> "Note: The speaker headshots slide has [N] placeholder stock photo(s) -- open the file in PowerPoint and delete the unused photo slots before presenting."

---

## Co-Branding

If the user mentions a customer, partner, or event, use `title_customer_logo` as the title slide type and set the `customer_logo` field to the provided logo file path.

For event branding (Summit, Build, World Tour), note this in the title slide subtitle or section headers as appropriate.

---

## Available Icons

The Snowflake template includes 150 SVG icons for use in `three_column_icons` and `four_column_icons` slide types.

Icon files are in `.cortex/skills/pptx/scripts/snowflake/assets/svg/`. Reference them by filename in the YAML spec (e.g., `col1_icon: "data-engineering.svg"`).

All icons use kebab-case naming (e.g., `analytics.svg`, `secure-data.svg`, `cloud.svg`). One special file `graphic_snowflake_logo_blue.svg` is the Snowflake wordmark logo.

---

## Content Limits

Refer to [snowflake-template.md](../skills/pptx/snowflake-template.md) for authoritative content limits enforced by the validator. Key limits:

- `title`: 60 chars (content/column types), 30 chars (split)
- `subtitle`: 80 chars
- `bullets`: 6 items max, 120 chars each
- `quote_text`: 200 chars
- `notes`: 4000 chars
- Total slides: 50 max

Keep text concise. Presentations are visual: don't pack paragraphs into slides.

### Character Budgets (Quick Reference -- hard limits)

| Field         | Limit              | Types                                    |
|---------------|--------------------|------------------------------------------|
| col_content   | 200 chars          | two_column_titled                        |
| col_content   | 120 chars          | three_column_titled                      |
| col_content   | 100 chars          | three_column_icons                       |
| col_content   | 90 chars           | four_column_numbers                      |
| col_content   | 80 chars           | four_column_icons                        |
| col_title     | 25 chars           | three_column_titled, three_column_icons  |
| col_title     | 20 chars           | four_column_icons                        |
| col_number    | 7 chars            | four_column_numbers                      |
| quote_text    | 200 chars          | quote, quote_photo, quote_simple         |
| split content | 300 chars          | split                                    |
| split title   | 30 chars           | split (half-width title box)             |
| agenda items  | 40 chars each, max 6 | agenda                                 |

Aim for **80% of limits** to leave margin and avoid validation round-trips. Narrow columns (`four_column_*`) fit ~23 chars/line x 3 lines = **~69 chars max target**.

**Table slides** (`table_styled`, `table_striped`): always provide only the columns you need; unused/empty columns should be omitted; the table will auto-size to fill the content area.

## Content Quality Rules

- **Never use em dashes or en dashes** (`—` U+2014, `–` U+2013). Replace em dashes with a colon (`:`) or semicolon (`;`); replace en dashes with a hyphen (`-`). Example: `"No lock-in: run on any cloud"` not `"No lock-in — run on any cloud"`. This applies to ALL fields: titles, subtitles, bullets, col content, notes, quotes, and attributions.
- **Divider slides (`chapter_particle`, `section`) MUST use ALL CAPS** for both title and subtitle. The builder auto-uppercases these, but write them in ALL CAPS in the spec for clarity and to avoid validator warnings. Example: `title: "GENERATION 6"`, `subtitle: "PS2 VS XBOX (2000-2001)"`. NOT: `title: "Generation 6"`.
- **Always provide a `subtitle`** on `content`, `two_column`, `two_column_titled`, `three_column`, `three_column_titled`, `three_column_icons`, `four_column_numbers`, `four_column_icons`, `split`, `quote`, and `quote_photo` slides. A subtitle fills the subtitle placeholder and prevents a large visual gap between the title and body. Use a short tagline (10-60 chars).
- **Keep `col*_content` under 60 chars** on `four_column_icons` and `three_column_icons` slides (narrow columns); under 120 chars on other column slides.
- **Column field ordering**: column fields (`col1`, `col2`, `col3`) must be provided in left-to-right visual order.
- **Keep `quote` text under 130 chars** on `quote_simple` slides; the large quote-mark graphic leaves limited space.
- **Set `attribution` to the presenter's name and title** (e.g. `"Jane Doe | Sr. Data Engineer"`), not a generic tagline like "Snowflake Data Cloud | 2026". This field appears on the title slide. Ask the user during requirements gathering.

## Agenda Slide Guidance

The `agenda` slide renders all items as **top-level bullets** (no visual hierarchy). Keep items simple and flat:

- **Maximum 6 items** on an agenda slide
- **All items are top-level** - subitems are flattened automatically (no indentation)
- **Max 40 chars per item.** Abbreviate if needed:
  - BAD: "Generation 7: PS3 vs Xbox 360 (2005-2006)" (43 chars)
  - GOOD: "Gen 7: PS3 vs Xbox 360 (2005-06)" (33 chars)
- If a deck has more than 6 sections, use two agenda slides ("Agenda: Part 1" / "Part 2")

Example:
```yaml
- type: agenda
  items:
    - "Introduction & Overview"
    - "Snowflake Data Cloud"
    - "Core Capabilities"
    - "Customer Success Stories"
    - "Next Steps & Q&A"
```

## Speaker Headshots Guidance

The `speaker_headshots` slide has 4 photo slots. If fewer than 4 speakers are specified, the extra slots show placeholder stock photos.
