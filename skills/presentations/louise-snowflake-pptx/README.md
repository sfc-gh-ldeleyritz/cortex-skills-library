# Snowflake PPTX Generator

Cortex Code skills for generating on-brand Snowflake `.pptx` presentations.

---

## Usage

### `/snowflake-pptx`

Pixel-perfect slides cloned from the official Snowflake January 2026 template. Best for customer pitches, partner decks, and any presentation that must match Snowflake brand standards exactly.

```
/snowflake-pptx create a technical overview deck for Acme Corp
```

### `/snowflake-pptx-creative`

Visually distinctive layouts with varied compositions, custom palettes, and creative slide structures. Best when you want a polished deck that stands out beyond the standard template.

```
/snowflake-pptx-creative build a 10-slide executive pitch on data sharing
```

---

## Setup

Both skills require a config file at `.cortex/snowflake-pptx-config.json`:

```json
{
  "presenter_name": "Jane Doe",
  "presenter_title": "Sr. Solutions Engineer",
  "presenter_headshot": "~/Pictures/headshot.jpg",
  "company": "Snowflake",
  "output_dir": "~/Documents/Decks",
  "deck_length": "medium",
  "safe_harbor": true
}
```

`presenter_name` and `presenter_title` are required. All other fields are optional. Both skills read this file as their first action and stop with a setup prompt if either required field is missing.

---

## How It Works

Both skills follow the same five-phase workflow. Shared workflow steps (research, speaker notes, config handling, QA) are extracted into reference files under `.cortex/skills/pptx/references/` to avoid duplication.

### Phase 1 — Gather Requirements

The skill reads the config file, then asks a small number of targeted questions via `AskUserQuestion` (skipping anything already answered by the config or the initial message):
- **Topic / purpose** (customer pitch, partner deck, internal enablement, event talk)
- **Target audience** (C-suite, technical leadership, engineers, analysts)
- **Source material** (optional URLs, documents, or key points)
- **Style direction** *(creative skill only)* — `"dark and premium"`, `"clean and minimal"`, or `"bold and energetic"`

### Phase 2 — Research

A capped subagent runs up to 5 web searches to collect key statistics, recent Snowflake product announcements, customer success examples, and competitive differentiators. User-provided URLs are fetched inline with `WebFetch` rather than delegated to the agent.

**Glean MCP** — see below.

### Phase 3 — Generate YAML Spec

The skill writes a YAML spec describing every slide. Both renderers are YAML-in, PPTX-out.

**Template skill** (`/snowflake-pptx`): 22 slide types mapped to slots in the January 2026 master template. Fixed structure: `title_headshot` → `safe_harbor` → `agenda` → content slides → `thank_you`.

**Creative skill** (`/snowflake-pptx-creative`): 14 flexible layout types (`columns`, `stat-grid`, `split`, `full-bleed`, `quote`, `timeline`, `icon-grid`, `comparison`, etc.) with a top-level `mood` field controlling the color scheme. No safe harbor slide. Strict composition rules enforce layout variety — no two consecutive slides with the same layout, at least one visually striking slide per five slides, max 20% single-column slides.

Every content slide must include speaker notes of at least 500 characters (target 1500–2500), with talking points, transition cues, and a `References:` block.

### Phase 4 — Build

**Template skill**: C# .NET 9 CLI deep-copies slides from the `.pptx` master template via OOXML, then injects text and images. Use wrapper scripts for reliable builds:

```bash
# Windows — recommended full pipeline (validate → build → validate → export for QA)
.cortex\skills\pptx\scripts\build-and-qa.cmd spec.yaml output.pptx

# Windows — validate + build + validate (no QA export)
.cortex\skills\pptx\scripts\build-and-validate.cmd spec.yaml output.pptx

# Windows — build only
.cortex\skills\pptx\scripts\build.cmd spec.yaml output.pptx

# macOS/Linux
.cortex/skills/pptx/scripts/build.sh spec.yaml output.pptx
```

**Creative skill**: Node.js renderer (pptxgenjs) generates slides programmatically from the YAML spec. Use wrapper scripts for reliable cross-platform builds:

```bash
# Windows (recommended — avoids cmd.exe path issues)
.cortex\skills\pptx\scripts\creative-build.cmd build --spec spec.yaml --out output.pptx

# macOS/Linux
.cortex/skills/pptx/scripts/creative-build.sh build --spec spec.yaml --out output.pptx
```

Or invoke the Node.js entry point directly (requires `npm install` first):

```bash
node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  build --spec spec.yaml --out output.pptx
```

The YAML spec is always saved alongside the output `.pptx` for reproducibility and future edits.

### Phase 5 — QA & Deliver

The skill validates the output, converts slides to JPG images (via PowerPoint COM on Windows), dispatches a subagent for visual inspection, fixes any issues, and delivers the final file path.

---

## Glean MCP Integration

Both skills perform a mandatory check for the `mcp__glean_default__search` tool before starting research.

**If Glean is available**, the skill runs at least 2 internal searches targeting field guides, battle cards, competitive intel, or technical deep-dives relevant to the deck topic. Results are incorporated into slide content and speaker notes alongside web sources.

**If Glean is not available**, the skill proceeds using web search and training knowledge, and adds a note to the first content slide's speaker notes: *"Internal sources unavailable: verify claims with field team before presenting."*

The skill never asks the user about Glean availability — it checks its own tool list automatically.

---

## Slide Types

**Template skill** — 22 types:

`title`, `title_headshot`, `title_wave`, `title_customer_logo`, `section`, `chapter_particle`, `content`, `two_column_titled`, `three_column_titled`, `three_column_icons`, `four_column_numbers`, `four_column_icons`, `quote`, `quote_photo`, `quote_simple`, `split`, `table_styled`, `table_striped`, `speaker_headshots`, `agenda`, `safe_harbor`, `thank_you`

**Creative skill** — 14 layout types:

`title`, `section`, `columns`, `stat-grid`, `split`, `full-bleed`, `quote`, `agenda`, `table`, `timeline`, `icon-grid`, `comparison`, `thank-you`, `free`

See `.cortex/skills/pptx/snowflake-template.md` and `.cortex/commands/snowflake-pptx-creative.md` for full field references and content limits.

---

## Implementation

```
.cortex/
├── commands/
│   ├── snowflake-pptx.md              # /snowflake-pptx command definition
│   └── snowflake-pptx-creative.md     # /snowflake-pptx-creative command definition
├── hooks/
│   ├── yaml-slide-type-check.py       # Blocks invalid slide types; warns on capitalization + variety
│   ├── yaml-emdash-check.py           # Blocks em/en dashes in YAML specs
│   ├── snowflake-pptx-preflight.py    # Injects context on PPTX-related prompts
│   └── snowflake-pptx-postbuild.py    # Post-build spec YAML enforcement
└── skills/pptx/
    ├── SKILL.md                       # Routing skill (which command to use)
    ├── snowflake-template.md          # Full slide type / field reference
    ├── references/                    # Shared workflow docs (deduped)
    │   ├── research-workflow.md
    │   ├── speaker-notes-requirements.md
    │   ├── config-schema.md
    │   ├── qa-workflow.md
    │   ├── gotchas.md
    │   ├── design-ideas.md
    │   ├── research-agent-prompt.md
    │   └── qa-agent-prompt.md
    └── scripts/
        ├── build.cmd / build.sh       # Wrapper scripts for template builds
        ├── build-and-validate.cmd     # Validate-spec → build → validate pipeline
        ├── build-and-qa.cmd           # Full pipeline + slide export for visual QA
        ├── creative-build.cmd / .sh   # Wrapper scripts for creative builds
        ├── export-slides.ps1          # PowerPoint COM slide-to-JPG exporter
        ├── dotnet/SnowflakePptx/      # C# .NET 9 renderer (template skill)
        ├── node/SnowflakeCreativePptx/ # Node.js renderer (creative skill)
        └── snowflake/
            ├── templates/             # SNOWFLAKE TEMPLATE JANUARY 2026.pptx
            └── assets/svg/ jpg/       # 150 SVG icons + JPG photos
```
