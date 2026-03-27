# Snowflake Template Presentation Guide

Build pixel-perfect Snowflake-branded PowerPoint presentations using the January 2026 template.

---

## Architecture

```
YAML Spec → SnowflakePptx (.NET CLI) → PPTX Output
                       ↑
               Template PPTX (23 slides)
               + Asset Catalog (SVG/JPG)
```

Every slide is cloned from the official template: backgrounds, gradients, grouped shapes, and brand elements are perfectly preserved.

---

## Two-Stage Workflow

### Stage 1: Generate YAML Spec

Extract content from source material and generate a YAML presentation spec for user review.

```yaml
slides:
  - type: title_customer_logo
    customer_logo: "acme_logo.png"
    title_line1: "ACME CORP"
    title_line2: "DATA TRANSFORMATION"
    subtitle: "Breaking Down Data Silos"
    notes: "Welcome John, Sarah. Today we'll walk through..."

  - type: safe_harbor

  - type: agenda
    items:
      - "Current Challenges"
      - "Snowflake Solution"
      - "Expected Outcomes"
      - "Next Steps"
    notes: "Quick overview - 4 sections, ~20 minutes total."

  - type: content
    title: "Current State: Data Silos"
    bullets:
      - "5 legacy systems with disconnected data"
      - "10M+ daily transactions need real-time processing"
      - "Manual reporting taking days instead of minutes"
    notes: "Source: Feb 15 meeting with John (CTO)."

  - type: four_column_numbers
    title: "Business Impact"
    col1_number: "5"
    col1_content: "Siloed legacy systems"
    col2_number: "10M+"
    col2_content: "Daily transactions"
    col3_number: "$2M"
    col3_content: "Current annual cost"
    col4_number: "40%"
    col4_content: "Target cost reduction"
    notes: "The $2M figure and 40% target came from John."

  - type: three_column_icons
    title: "Snowflake Solution"
    col1_icon: "data-engineering.svg"
    col1_title: "Unified Data"
    col1_content: "Single source of truth across all systems"
    col2_icon: "analytics.svg"
    col2_title: "Real-Time Analytics"
    col2_content: "Instant insights on 10M+ transactions"
    col3_icon: "secure-data.svg"
    col3_title: "Enterprise Security"
    col3_content: "SOC2 & HIPAA compliant"

  - type: thank_you
```

### Stage 2: Build PPTX

After user reviews and approves the YAML spec, validate first then build:

### Before Finalizing Each Slide

For every `col_content` field, count the characters. If your text is:
- **Under the minimum** → expand with specific detail (add qualifiers, metrics, or context)
- **Over the maximum** → trim to the essentials
- **Within range** → proceed

```bash
# Validate spec (catches overflow issues before building)
dotnet .cortex/skills/pptx/scripts/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll \
  validate-spec spec.yaml

# Build (bash/zsh) -- or use wrapper script: .cortex/skills/pptx/scripts/build.sh spec.yaml output.pptx
SNOWFLAKE_TEMPLATE_PATH=".cortex/skills/pptx/scripts/snowflake/templates/SNOWFLAKE TEMPLATE JANUARY 2026.pptx" \
  dotnet .cortex/skills/pptx/scripts/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll \
  build --spec spec.yaml --out output.pptx
```

**Windows cmd.exe** (set env var on its own line — do NOT use `&&` chaining):

```cmd
REM Validate spec
dotnet .cortex\skills\pptx\scripts\dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll validate-spec spec.yaml

REM Build -- or use wrapper: .cortex\skills\pptx\scripts\build.cmd spec.yaml output.pptx
set SNOWFLAKE_TEMPLATE_PATH=.cortex\skills\pptx\scripts\snowflake\templates\SNOWFLAKE TEMPLATE JANUARY 2026.pptx
dotnet .cortex\skills\pptx\scripts\dotnet\SnowflakePptx\bin\Release\net9.0\SnowflakePptx.dll build --spec spec.yaml --out output.pptx
```

Validate the output:

```bash
dotnet .cortex/skills/pptx/scripts/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll \
  validate output.pptx
```

---

## Slide Types Reference

### Title Slides

| Type | Description | Key Fields |
|------|-------------|------------|
| `title` | Standard title (dual-color) | `title_line1`, `title_line2`, `subtitle`, `date` |
| `title_headshot` | Title with presenter headshot | `title_line1`, `title_line2`, `subtitle`, `date`, `name`, `speaker_title`, `company`, `photo` |
| `title_wave` | Title with wave background | `title`, `subtitle`, `attribution` |
| `title_customer_logo` | Title with customer logo | `title_line1`, `title_line2`, `subtitle`, `customer_logo` |

### Section Dividers

All divider slide types (`section`, `section_dots`, `chapter_particle`) **auto-uppercase titles and subtitles** at render time. Write them in ALL CAPS in the spec for clarity, or the builder will uppercase them automatically.

| Type | Description | Key Fields |
|------|-------------|------------|
| `section` | Plain section divider | `title`, `subtitle` |
| `chapter_particle` | Chapter break with particle/wave pattern | `title`, `subtitle` |

> **Design intent**: Divider slides are visual pauses. Titles max 40 chars (~5 words, ALL CAPS). Subtitles max 60 chars (~8 words) — short phrases, not sentences.

### Content Slides

| Type | Description | Key Fields |
|------|-------------|------------|
| `content` | Standard bullet content | `title`, `bullets` (list) |

### Multi-Column

**Prefer 3- and 4-column layouts** over 2-column for visual variety. Use `three_column_icons` when you want icons alongside text (SVG icons are auto-injected from the asset catalog preserving aspect ratio). Use `three_column_titled` for text-only columns without icons.

| Type | Description | Key Fields |
|------|-------------|------------|
| `two_column_titled` | Two columns with headers | `title`, `col1_title`, `col1_content`, `col2_title`, `col2_content` |
| `three_column_titled` | Three columns with headers | `title`, `col1_title`, `col1_content`, `col2_title`, `col2_content`, `col3_title`, `col3_content` |
| `three_column_icons` | Columns with icons (preferred for visual impact) | `title`, `col1_icon`, `col1_title`, `col1_content`, `col2_icon`, `col2_title`, `col2_content`, `col3_icon`, `col3_title`, `col3_content` |
| `four_column_numbers` | Big numbers with descriptions | `title`, `col1_number`, `col1_content`, ..., `col4_number`, `col4_content` |
| `four_column_icons` | Four columns with icons | `title`, `col1_icon`, `col1_title`, `col1_content`, ..., `col4_icon`, `col4_title`, `col4_content` |

#### Column Content Guidelines

Column text boxes are narrow — overflow is a common failure mode. Follow these rules:

**`three_column_icons`** (~25 chars/line, max 4 lines):
- `col*_title`: max 25 chars
- `col*_content`: **70-100 chars** (2-3 complete sentences, enforced range: 50-100)
- BAD: `"ML/AI pipelines with deep Spark expertise."` (43 chars — too sparse)
- GOOD: `"Industry-leading ML/AI pipeline orchestration with deep Apache Spark integration and built-in MLflow experiment tracking."` (96 chars)

**`three_column_titled`** (~28 chars/line, max 5 lines):
- `col*_title`: max 25 chars
- `col*_content`: **70-120 chars** (2-3 sentences, enforced range: 50-120)

**`four_column_numbers`** (~23 chars/line at 16pt, max 3 lines):
- `col*_number`: max 7 chars
- `col*_content`: **30-90 chars** (1-3 sentences, target ~40-55 chars for best visual fill at 16pt)
- Font scales dynamically: 16pt (<=55 chars), 15pt (56-69), 14pt (70-90)
- BAD: `"Cloud DW market share"` (21 chars — too sparse)
- GOOD: `"Cloud data warehouse market share, leading all competitors in enterprise adoption"` (80 chars)

**`four_column_icons`** (~18 chars/line, max 4 lines):
- `col*_title`: max 20 chars
- `col*_content`: **50-80 chars** (1-2 sentences, enforced range: 40-80)
- BAD: `"Multi-cloud SQL analytics with governed sharing."` (49 chars — too sparse)
- GOOD: `"Multi-cloud SQL analytics engine with cross-cloud governed data sharing and zero-copy clones."` (73 chars)

**General column rules:**
- Write `col*_content` as **prose sentences**, not lists of items
- Never use line breaks or multi-item lists in column content
- The `col*_number` / icon is the visual anchor; the description provides context

### Special

| Type | Description | Key Fields |
|------|-------------|------------|
| `split` | Half image, half content | `title`, `subtitle`, `content_title`, `content`, `image` — **IMPORTANT: `image` MUST be a JPG file from `/assets/jpg` (not SVG)**. Do NOT use `bullets`; use `content_title` (short header) + `content` (prose paragraph) instead. **`title` is half-width (30 chars max) — keep it short; move detail into `content_title`** |
| `quote` | Quote with attribution | `quote`, `author`, `role`, `company` |
| `quote_photo` | Quote with headshot | `quote`, `author`, `role`, `photo` |
| `quote_simple` | Minimal quote | `quote`, `attribution` |
| `agenda` | Numbered agenda | `title`, `items` (list of strings or single-key dicts — see below) |
| `table_styled` | Styled table with row headers | `title`, `subtitle`, `table` (`headers`, `rows`) — max 5 data rows recommended |
| `table_striped` | Striped table with header rows | `title`, `subtitle`, `table` (`headers`, `rows`) — max 5 data rows recommended |
| `speaker_headshots` | Up to 4 presenter headshots | `title` (optional), `speakers` (list: `name`, `speaker_title`, `company`, `photo`) — unfilled slots are removed automatically |
| `safe_harbor` | Legal safe harbor text | (no fields — uses template text) |
| `thank_you` | Thank you / closing | (optional: `title`, `title_line2`) |

#### Agenda Item Formats

```yaml
# Preferred format (single-key dict):
items:
  - "Current Challenges":
      subitems:
        - "Data silos across 5 systems"
  - "Next Steps"

# Also accepted (explicit title key):
items:
  - title: "Current Challenges"
    subitems:
      - "Data silos across 5 systems"
```

---

## Content Limits

These are enforced by the schema validator. Exceeding them triggers warnings or auto-truncation.

| Field | Limit | Notes |
|-------|-------|-------|
| `title` | 45-50 chars | 45 for `content`, 50 for column slides, 40 for dividers, 30 for `split`, 80 for tables |
| `subtitle` | 50-80 chars | 50 for `title_headshot`, 60 for `section`/`chapter_particle`, 80 for other slides |
| `bullets` | 6 items max, 120 chars each | Content slides (5+ bullets or >120 chars triggers font scaling; keep under 100 chars for best readability) |
| `col*_title` | 20-25 chars | Column headers |
| `col*_content` (3-col) | 50-120 chars | Three-column body text (min 50 enforced) |
| `col*_content` (4-col numbers) | 30-90 chars | Four-column-numbers body text (min 30 enforced) |
| `col*_content` (4-col icons) | 40-80 chars | Four-column-icons body text (min 40 enforced) |
| `col*_number` | 7 chars | Big number display |
| `quote` | 200 chars | Quote slides |
| `attribution` (quote_simple) | 40 chars | Single line only — bottom-positioned, limited space |
| `notes` | 4000 chars | Speaker notes |
| `table.rows` | 5 rows max | Denser tables are hard to read |
| Total slides | 50 max | Sanity limit |

---

## Text Fitting (Three-Layer Defense)

1. **Schema limits**: hard character caps prevent obviously too-long text
2. **Pre-emptive font sizing**: threshold tables calculate optimal font sizes based on character count
3. **normAutofit fallback**: PowerPoint's built-in auto-shrink (75% floor) catches edge cases

This ensures text never overflows its container while maintaining visual consistency.

---

## Speaker Notes

Every slide should have speaker notes. Good notes include:
- **Context**: Where did this data come from? Who said this?
- **Talking points**: What should the presenter emphasize?
- **Transitions**: How does this slide connect to the next?
- **References**: Where did this information come from? include hyperlinks where relevant
- **Audience cues**: Questions to ask, pauses to take

---

## Assets

Icons and graphics are bundled in `scripts/snowflake/assets/`:

```
scripts/snowflake/assets/
├── svg/   # 150 SVG icons (kebab-case names + logo)
├── jpg/   # 34 JPG photos (photo_*, background_*)
└── png/   # 1 PNG (arrow.png)
```

Reference icons by filename in slide specs (e.g., `col1_icon: "data-engineering.svg"`). All icons use kebab-case naming. One special file `graphic_snowflake_logo_blue.svg` is the Snowflake wordmark logo.

---

## Template Info

- **File**: SNOWFLAKE TEMPLATE JANUARY 2026.pptx
- **Slides**: 23
- **Dimensions**: 13.333" × 7.5" (widescreen 16:9)
- **Brand color**: #29B5E8 (Snowflake Blue)
- **Font**: Arial (heading and body)
