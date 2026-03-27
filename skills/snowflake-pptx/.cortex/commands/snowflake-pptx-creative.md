---
name: snowflake-pptx-creative
description: "[USER-INVOCABLE] Create Snowflake-branded PowerPoint presentations with creative, varied layouts. Use when the user wants creative or visually distinctive Snowflake slides (as opposed to pixel-perfect template). Also triggers for: snowflake creative pptx, snowflake creative deck, creative snowflake presentation. Use /snowflake-pptx-creative or $snowflake-pptx-creative to invoke directly."
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - Bash(node *)
  - Bash(npm *)
  - Bash(powershell *)
  - Bash(mkdir *)
  - Bash(ls *)
  - Bash(cd *)
  - WebSearch
  - WebFetch
  - Agent
---

# Snowflake Creative PPTX Generator

Create visually distinctive, Snowflake-branded PowerPoint presentations with creative, varied layouts — backed by real research.

This skill uses a Node.js renderer with 11 flexible layout types to produce high-impact decks. Every presentation is researched via web search (and Glean MCP when available), and speaker notes include source references. The creative skill is fully independent from the pixel-perfect template skill.

---

## Renderer Setup

The Node.js renderer must be ready before building. The CLI must always be invoked from the **repo root**.

**Pre-build/validate check**: Before running any build or validate command, verify that `node_modules/` exists:

```bash
ls .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/node_modules/
```

If the directory does not exist (or the `ls` fails), install dependencies first:

```bash
cd .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx && npm install
```

**Alternatively**, use the wrapper scripts which auto-install dependencies:

- **Windows**: `.cortex\skills\pptx\scripts\creative-build.cmd build --spec spec.yaml --out output.pptx`
- **macOS/Linux**: `.cortex/skills/pptx/scripts/creative-build.sh build --spec spec.yaml --out output.pptx`

**Build command** (run from repo root):

```bash
node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  build --spec spec.yaml --out output.pptx
```

**Validate command** (run from repo root):

```bash
node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  validate spec.yaml
```

The `validate` command validates the YAML spec (not the PPTX output). It produces a human-readable summary to stdout and exits with code 0 on success, non-zero on failure.

---

## Workflow

### Phase 1: Gather Requirements

#### Config File

Read `.cortex/skills/pptx/references/config-schema.md` for the full config file schema and requirements. Read the config file as your first action.

Note: The `safe_harbor` field is ignored by this skill (creative decks don't include a safe harbor slide).

Ask the user these questions using `AskUserQuestion` (skip any already answered by the config or the user's initial message):

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

4. **Style direction** *(optional)*: "Any style direction? For example: 'dark and premium', 'clean and minimal', 'bold and energetic' — or leave blank and I'll choose based on your topic and audience."

**Defaults (don't ask, but respect if overridden):**
- **Length**: Medium (10-15 slides), or from `deck_length` in config
- **Output path**: Current directory, auto-named as `snowflake-creative-{topic-slug}.pptx`, or from `output_dir` in config
- **Presenter**: From `.cortex/snowflake-pptx-config.json` if present
- **Company**: From config; used as subtitle on title slide when present
- **Deck length**: From config; overrides medium default
- **Mood**: From user's style direction answer, or Claude picks based on topic/audience

If the user already provided some of these details in their initial message, don't re-ask: extract the answers from context and confirm.

---

### Phase 2: Research

#### Research

Read `.cortex/skills/pptx/references/research-workflow.md` for the full research process (capped agent search, Glean MCP check, source tracking).

For user-provided URLs: use `WebFetch` inline: do not delegate to the agent.

#### Speaker Notes Requirements

Read `.cortex/skills/pptx/references/speaker-notes-requirements.md` for speaker notes format and requirements.

---

### Phase 3: Generate YAML Spec

#### Mood Selection

If the user provided a style direction in Phase 1, use it as the `mood` field. Only these three named values are valid:
- `"dark and premium"` — dark navy background, white text, Snowflake blue accents
- `"clean and minimal"` — white background, navy text, Snowflake blue accents
- `"bold and energetic"` — Snowflake blue background, white text, navy accents

If the user left style direction blank, choose the mood based on topic and audience:
- Executive / C-suite pitch → `"dark and premium"`
- Technical deep-dive → `"clean and minimal"`
- Event talk / energetic keynote → `"bold and energetic"`
- Default when uncertain → `"clean and minimal"`

## Composition Principles

- **Vary layout types** — Don't use the same layout type on back-to-back slides. Alternate between data-heavy layouts (`stat-grid`, `table`) and visual/narrative layouts (`section`, `full-bleed`, `timeline`).
- **Rotate accent colors** — Apply Rule 6 to use at least 3 different accent colors in any deck longer than 5 slides.
- **Use variants deliberately** — `section diagonal` works well as a mid-deck divider. `columns cards` works well for feature comparisons with 3+ options. `stat-grid accent-bg` is high-impact for a key metrics slide.
- **One "wow" slide per 5 slides** — Ensure at least one visually striking slide (e.g., `full-bleed`, `comparison` with bold accent, `timeline`) every 5 slides.

## Anti-Patterns (Do Not Do These)

- ❌ Same `accent_color` on every slide
- ❌ All `section` slides using `default` variant
- ❌ Using `stat-grid` for every slide with numbers
- ❌ More than 3 bullet points in a `comparison` panel
- ❌ `timeline` with only 2 steps (use `section` + `columns` instead)
- ❌ `icon-grid` with 5 items (not supported — use 4 or 6)

#### Generation Rules (Mandatory)

These five rules apply to every generated spec without exception:

1. **No two consecutive slides with the same layout.** If the next slide would repeat the previous layout, pick a different layout that fits the content.

2. **Max 20% `columns:1`** (plain single-column content) slides in any deck. In a 10-slide deck, at most 2 slides may use `columns: 1`. Prefer multi-column, `stat-grid`, `split`, `full-bleed`, or `quote` layouts.

3. **Every deck must include at least one** of: `stat-grid`, `full-bleed`, or `quote`. These visually distinctive layouts are what make the creative skill worthwhile.

4. **`free` layout is a last resort** — only use it when no other layout type fits the content. Always attempt `split`, `columns`, or `full-bleed` first.

5. **Slide structure**: `title` → `agenda` → content slides → `thank-you`. Every deck begins with a title slide and agenda, and ends with thank-you. Do not omit these structural slides.

**Rule 6 — Rotate accent colors:** Most slides accept an optional `accent_color` field. Rotate through the palette across the deck — do not use `snowflakeBlue` on every slide. Suggested pattern for a 10-slide deck: slides 1–3 use `snowflakeBlue`, slides 4–5 use `starBlue`, slide 6 uses `valenciaOrange`, slide 7 uses `firstLight`, slides 8–9 use `purpleMoon`, slide 10 uses `snowflakeBlue`. `title` and `thank-you` layouts always use `snowflakeBlue` regardless of this field.

Valid values: `snowflakeBlue`, `starBlue`, `valenciaOrange`, `firstLight`, `purpleMoon`

**Rule 7 — Headshot injection:** When `presenter_headshot` is set in `.cortex/snowflake-pptx-config.json`, include `headshot: <expanded path>` on the title slide spec. Expand `~` to the absolute home directory path. Omit the field entirely when `presenter_headshot` is absent or empty.

#### Deck Length

| Setting | Slide count |
|---------|-------------|
| `short` | 6-9 slides |
| `medium` | 10-15 slides |
| `long` | 20-30 slides |

Use `deck_length` from config, or default to medium. The user can override during requirements gathering.

**Deck structure by length:**
- **Short (6-9)**: title → agenda → 3-5 content slides → thank-you
- **Medium (10-15)**: title → agenda → 2-3 sections with 2-3 content slides each → thank-you
- **Long (20-30)**: title → agenda → 4-5 sections with section dividers, 3-4 content slides per section → thank-you

#### Available Layout Types

| Layout | Description | Variants |
|--------|-------------|----------|
| `title` | Full-slide title card with background and subtitle | — |
| `section` | Section divider / chapter break | `default`, `diagonal`, `side-panel` |
| `columns` | 1-4 column layout with optional icons, headings, and body text | `default`, `cards`, `numbered`, `bordered` |
| `stat-grid` | Big-numbers row: 2-4 stats each with a large number and label | `default`, `accent-bg`, `inline` |
| `split` | Half-image, half-text: image left or right with bullets | — |
| `full-bleed` | Full-slide background image with optional overlay and quote text | — |
| `quote` | Centered pull quote with attribution | — |
| `agenda` | Bulleted agenda / table of contents | — |
| `table` | Data table with headers and rows | — |
| `timeline` | Horizontal step-by-step flow (3–5 steps) with numbered nodes | — |
| `icon-grid` | 2×2 or 3×2 grid of feature cards (exactly 4 or 6 items) | — |
| `comparison` | Two-panel side-by-side (left vs right) with checkmark bullets | — |
| `thank-you` | Branded closing slide | — |
| `free` | Raw pptxgenjs element array (last resort escape hatch) | — |

#### Content Limits

| Field | Limit |
|-------|-------|
| `title` | 60 chars |
| `subtitle` | 80 chars |
| `body` / bullet items | 120 chars each, 6 items max |
| `heading` (columns) | 40 chars |
| `number` (stat-grid) | 10 chars |
| `label` (stat-grid) | 40 chars |
| `quote` | 200 chars |
| `notes` | 4000 chars |
| Total slides | 50 max |

#### Content Quality Rules

- **Never use em dashes or en dashes** (`—` U+2014, `–` U+2013) in any content field. Replace em dashes with a colon (`:`) or semicolon (`;`); replace en dashes with a hyphen (`-`). Example: `"No lock-in: run on any cloud"` not `"No lock-in — run on any cloud"`. This applies to all fields: titles, subtitles, bullets, headings, body text, notes, quotes, and attributions.

- **Prefer `columns: 2` or higher** over `columns: 1`. Use single-column only when the content is genuinely list-like with no parallel structure.

- **Use `stat-grid` for quantitative facts**: When you have 2-4 statistics (percentages, counts, dollar figures, time reductions), use `stat-grid` rather than burying them in bullets.

- **Use `split` for visual workflows**: When describing an architecture, process, or showing a screenshot concept, use `split` with `image_position: left` or `right`.

- **Use `full-bleed` for high-impact moments**: Use before a key section to set the mood, or to highlight a compelling customer quote.

- **Use `section` to signal transitions**: Insert a `section` slide at the start of each major chapter in medium and long decks.

- **Image resolution for `split` and `full-bleed`**: Filenames without a path separator (e.g., `"data-center.jpg"`) are resolved against the JPG catalog at `.cortex/skills/pptx/scripts/snowflake/assets/jpg/`. Use filenames from that catalog when a stock image is appropriate.

#### YAML Spec Format

The top-level YAML structure:

```yaml
mood: "clean and minimal"
slides:
  - layout: title
    title: "Presentation Title"
    subtitle: "Presenter Name | Company | Date"
    notes: "..."
  - layout: agenda
    title: "Agenda"
    items:
      - "Introduction"
      - "Topic One"
      - "Topic Two"
      - "Next Steps"
    notes: "..."
  # ... content slides ...
  - layout: thank-you
    title: "Thank You"
    subtitle: "presenter@snowflake.com"
    notes: "..."
```

> **IMPORTANT - Write tool usage**: When writing the YAML spec file with the `Write` tool, the `content` parameter MUST be a plain YAML string. Do NOT pass a JSON array or content block object (e.g. `[{"text": "...", "type": "text"}]`). Pass the raw YAML text directly as the string value of `content`.

---

### Phase 4: Build

After generating the YAML spec, build the presentation.

**Step 1: Validate the spec first.**

```bash
node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  validate spec.yaml
```

If validation fails (non-zero exit), fix the errors in the spec and re-validate before proceeding. Do not build a spec that fails validation.

**Step 2: Build the PPTX.**

```bash
node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  build --spec spec.yaml --out output.pptx
```

Both commands must be run from the repo root. Substitute `spec.yaml` and `output.pptx` with the actual paths you have chosen.

**IMPORTANT - Save the spec YAML**: Always save the YAML spec file alongside the output PPTX (e.g., `snowflake-creative-{topic-slug}-spec.yaml` next to `snowflake-creative-{topic-slug}.pptx`). This is required for:
- Reproducibility: regenerating the deck with fixes
- Future edits: modifying content without starting over
- Version control: tracking deck changes over time

**Print the output path** to the user once the build completes successfully.

---

### Phase 5: QA & Deliver

Read `.cortex/skills/pptx/references/qa-workflow.md` for the full QA process.
Read `.cortex/skills/pptx/references/gotchas.md` for known pitfalls.

**Step 1: Convert to images** using the shared export script:

```cmd
mkdir temp\qa 2>nul
powershell -ExecutionPolicy Bypass -File .cortex\skills\pptx\scripts\export-slides.ps1 -PptxPath output.pptx -OutDir temp\qa
```

Substitute `output.pptx` with the actual output path. Do NOT use LibreOffice for PPTX rendering — it renders differently to Microsoft PowerPoint.

**Step 2: Dispatch a subagent for visual inspection.**

Ask a subagent with fresh eyes to review all slide images for:
- Layout correctness (no overlapping text, no missing content)
- Visual consistency (mood colors applied uniformly, fonts correct)
- Content accuracy (titles match content, stats are legible)
- Brand compliance (Snowflake logo present on title and thank-you)
- Truncation (any text cut off or overflowing its bounds)

**Step 3: Fix any issues found.** Edit the YAML spec, re-validate, and rebuild for any slide with issues.

**Step 4: Re-verify affected slides.** Re-export those specific slides to images and confirm the fix.

**Step 5: Deliver the final `.pptx` to the user.** Print the full output path.

---

## Layout Field Reference

### `title`
```yaml
- layout: title
  background: dark         # dark | light (optional, default: dark)
  title: "Modernising the Data Stack"
  subtitle: "Acme Corp | Q1 2026"
  headshot: "/Users/jane/Pictures/headshot.jpg"   # optional; from presenter_headshot in config
  notes: "..."
```

### `section`
```yaml
- layout: section
  background: dark         # dark | light (optional, default: dark)
  title: "Chapter One"
  subtitle: "Optional tagline"
  notes: "..."
```

### `columns`
```yaml
- layout: columns
  columns: 3               # 1-4; items count must not exceed this value
  title: "Three Core Pillars"
  subtitle: "Optional tagline"
  items:
    - icon: data-engineering.svg   # optional; filename from SVG catalog
      heading: "Ingest"
      body: "Real-time pipelines from any source"
    - icon: data-monetization.svg
      heading: "Transform"
      body: "SQL-native transformations at scale"
    - icon: trusted.svg
      heading: "Govern"
      body: "Policy-based access and lineage"
  notes: "..."
```

Items count must match `columns`. If `items` count exceeds `columns`, the validator rejects the spec. If `items` count is less than `columns`, the validator warns but proceeds.

### `stat-grid`
```yaml
- layout: stat-grid
  title: "By the Numbers"
  subtitle: "Optional tagline"
  stats:
    - number: "9,000+"
      label: "Customers worldwide"
    - number: "42%"
      label: "YoY revenue growth"
    - number: "3,590+"
      label: "Marketplace listings"
  notes: "..."
```

2-4 stats required. More than 4 is a validation error. Numbers render large (48-64pt bold) in accent color.

### `split`
```yaml
- layout: split
  title: "Product Architecture"
  subtitle: "Optional tagline"
  image: "data-center.jpg"     # filename → resolved against JPG catalog
  image_position: left         # left | right
  bullets:
    - "Point one"
    - "Point two"
    - "Point three"
  notes: "..."
```

### `full-bleed`
```yaml
- layout: full-bleed
  image: "snowflake-bg.jpg"    # filename → resolved against JPG catalog
  overlay: dark                # dark | light | none
  quote: "We went from weeks to minutes."
  attribution: "CTO, Acme Corp"
  notes: "..."
```

`full-bleed` requires either `image` or `quote` (or both). When `overlay: none`, text defaults to white; ensure legibility against the background.

### `quote`
```yaml
- layout: quote
  quote: "The data cloud changes everything."
  attribution: "Jane Doe | VP Data, Acme Corp"
  background: dark             # dark | light (optional, default: dark)
  notes: "..."
```

### `agenda`
```yaml
- layout: agenda
  title: "Agenda"
  items:
    - "Introduction"
    - "Snowflake Data Cloud"
    - "Customer Stories"
    - "Next Steps"
  notes: "..."
```

2-6 items required. All items render as top-level bullets (no indentation).

### `table`
```yaml
- layout: table
  title: "Feature Comparison"
  subtitle: "Optional tagline"
  headers: ["Feature", "Legacy", "Snowflake"]
  rows:
    - ["Real-time ingest", "No", "Yes"]
    - ["Zero-copy sharing", "No", "Yes"]
    - ["Multi-cloud native", "No", "Yes"]
  notes: "..."
```

### `thank-you`
```yaml
- layout: thank-you
  title: "Thank You"
  subtitle: "questions@snowflake.com"
  notes: "..."
```

### `free` (last resort escape hatch)
```yaml
- layout: free
  elements:
    - type: text
      value: "Custom text"
      options: { x: 1, y: 1, w: 8, h: 1, fontSize: 36, color: "29B5E8" }
    - type: shape
      shape: "rect"
      options: { x: 0, y: 0, w: 10, h: 0.1, fill: { color: "29B5E8" } }
  notes: "..."
```

Use `free` only when no other layout type fits the content. The `shape` value is passed directly to pptxgenjs `addShape()` as the ShapeType argument.

### `timeline`

Horizontal step-by-step flow (3–5 steps). Each step has a numbered circle node on a connector line, with heading and body below.

```yaml
- layout: timeline
  title: "Adoption Journey"
  accent_color: starBlue
  steps:
    - number: "1"
      heading: "Pilot"
      body: "Run a proof of concept"
    - number: "2"
      heading: "Scale"
      body: "Expand to production workloads"
    - number: "3"
      heading: "Optimize"
      body: "Tune cost and performance"
```

### `icon-grid`

2×2 or 3×2 grid of feature cards (exactly 4 or 6 items). Each card has an accent-colored top banner, optional SVG icon, heading, and body.

```yaml
- layout: icon-grid
  title: "Platform Capabilities"
  items:
    - icon: "data-engineering.svg"
      heading: "Data Engineering"
      body: "Build reliable pipelines at scale"
    - heading: "Data Science"
      body: "ML-native compute and storage"
    - heading: "Data Sharing"
      body: "Zero-copy collaboration across clouds"
    - heading: "Governance"
      body: "Unified policy and lineage"
```

Exactly 4 or 6 items required. 5 items is a validation error.

**Best practice**: Always include an `icon` field for each item when possible. Items without icons render with a compact banner, which looks less polished than the full accent-colored banner with an icon.

### `comparison`

Two-panel side-by-side (left vs right). Each panel has a colored header and up to 5 checkmark bullets. Accent color applies to the left panel header; right panel always uses navy.

```yaml
- layout: comparison
  title: "Legacy vs. Snowflake"
  accent_color: snowflakeBlue
  left:
    heading: "Traditional DWH"
    bullets:
      - "Fixed capacity, manual scaling"
      - "High licensing fees"
  right:
    heading: "Snowflake"
    bullets:
      - "Elastic, pay-per-second"
      - "No infrastructure management"
```

Maximum 5 bullets per panel. More than 5 is a validation error.

---

## SVG Icon Catalog

Icon files are in `.cortex/skills/pptx/scripts/snowflake/assets/svg/`. Reference them by filename in `columns` item `icon` fields (e.g., `icon: "data-engineering.svg"`).

There are 150 SVG files total: 149 kebab-case icons (e.g., `analytics.svg`, `secure-data.svg`, `cloud.svg`) plus the Snowflake wordmark logo (`graphic_snowflake_logo_blue.svg`).

---

## Validation Rules (per layout)

| Layout | Required fields |
|--------|----------------|
| `title` | `title` |
| `section` | `title` |
| `columns` | `columns` (1-4), `items` (non-empty); items count > `columns` is an error; items count < `columns` is a warning |
| `stat-grid` | `stats` (2-4 items, each with `number` + `label`) |
| `split` | `image`, `bullets` (non-empty) |
| `full-bleed` | `image` OR `quote` |
| `quote` | `quote` |
| `agenda` | `items` (2-6) |
| `table` | `headers`, `rows` (non-empty) |
| `timeline` | `steps` (3-5 items, each with `number` + `heading` + `body`) |
| `icon-grid` | `items` (exactly 4 or 6, each with `heading` + `body`; `icon` optional) |
| `comparison` | `left` and `right` (each with `heading` + `bullets`; max 5 bullets per panel) |
| `thank-you` | `title` |
| `free` | `elements` (non-empty) |
