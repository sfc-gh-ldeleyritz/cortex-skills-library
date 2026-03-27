---
name: pptx
description: "Create or edit Snowflake-branded PowerPoint presentations. Triggers for: snowflake deck, snowflake slides, snowflake pptx, customer pitch, partner deck, presentation, creating slides."
---

# PPTX Skill

## Routing

Determine which workflow the user needs:

- **Pixel-perfect template deck** -> Use `/snowflake-pptx` command
- **Creative / visually distinctive deck** -> Use `/snowflake-pptx-creative` command
- **Reading / parsing an existing .pptx** -> Use standard tools (Read, python-pptx, etc.) — no workflow needed

Ask the user if unclear which approach they want.

---

## Build Quick Reference

> **Platform note:** On Windows (including WSL running under Windows), always use the `.cmd`
> wrapper scripts. The `.sh` scripts require dotnet to be on the PATH, which is typically
> only true on macOS/Linux native installs.

### Pixel-Perfect (dotnet)

```cmd
REM Windows — recommended full pipeline (validate-spec -> build -> validate)
.cortex\skills\pptx\scripts\build-and-validate.cmd spec.yaml output.pptx

REM Windows — full pipeline with QA export
.cortex\skills\pptx\scripts\build-and-qa.cmd spec.yaml output.pptx

REM Windows — build only (skip validation — use only when iterating on known-good specs)
.cortex\skills\pptx\scripts\build.cmd spec.yaml output.pptx

REM Windows — validate spec only
.cortex\skills\pptx\scripts\validate-spec.cmd spec.yaml
.cortex\skills\pptx\scripts\validate-spec.cmd spec.yaml --strict

REM Windows — validate rendered PPTX
.cortex\skills\pptx\scripts\validate.cmd output.pptx
```

```bash
# macOS/Linux (or WSL)
.cortex/skills/pptx/scripts/build.sh spec.yaml output.pptx
.cortex/skills/pptx/scripts/build-and-validate.sh spec.yaml output.pptx
.cortex/skills/pptx/scripts/validate-spec.sh spec.yaml [--strict]
.cortex/skills/pptx/scripts/validate.sh output.pptx

# Build .NET project (first time or after code changes)
dotnet build .cortex/skills/pptx/scripts/dotnet/SnowflakePptx -c Release
```

> **WARNING:** Do NOT use `set SNOWFLAKE_TEMPLATE_PATH=... && dotnet ...` on Windows cmd.exe — use the wrapper scripts. Do NOT call `dotnet` directly in WSL — use the wrapper scripts which handle path resolution.

### Creative (Node.js)

```bash
node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  build --spec spec.yaml --out output.pptx

node .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/index.js \
  validate spec.yaml
```

---

## Export Slides for QA

```cmd
REM Windows — export to JPEG for visual inspection
powershell -ExecutionPolicy Bypass -File .cortex\skills\pptx\scripts\export-slides.ps1 -PptxPath output.pptx -OutDir temp\qa
```

---

## Fallback Strategy for Rendering Failures

If a slide type renders incorrectly, do NOT replace all broken types with `content`.
Use this priority order for replacements:
1. `content` — for data/facts that were in columns or tables
2. `quote_simple` — for key takeaways that were in split or highlight slides
3. `section` — to break up long runs of content slides
4. `chapter_particle` — for visual breaks between sections

Never have more than 2 consecutive slides of the same type. Ensure category coverage (column, quote, table, visual, bullet) before repeating any slide type. Mix `section` and `chapter_particle` dividers when using 3+ section breaks.

---

## Slide Types

**Pixel-perfect (22 types)**: See [snowflake-template.md](snowflake-template.md) for full reference.

`title`, `title_headshot`, `title_wave`, `title_customer_logo`, `section`, `chapter_particle`, `content`, `two_column_titled`, `three_column_titled`, `three_column_icons`, `four_column_numbers`, `four_column_icons`, `quote`, `quote_photo`, `quote_simple`, `split`, `table_styled`, `table_striped`, `speaker_headshots`, `agenda`, `safe_harbor`, `thank_you`

**Creative (14 layouts)**: See the `/snowflake-pptx-creative` command for layout reference.

---

## Column Content Limits (Quick Reference)

| Slide Type | col_content max | col_title max | col_number max |
|------------|-----------------|---------------|----------------|
| three_column_icons | 100 | 25 | — |
| three_column_titled | 120 | 25 | — |
| four_column_numbers | 90 | — | 7 |
| four_column_icons | 80 | 20 | — |

**ALWAYS validate-spec before building.** If 3+ errors, rewrite the full spec.yaml.

---

## Mandatory QA Phase

**DO NOT declare the task complete until visual QA has been performed.**

After every build:
1. Export slides to JPEG: `powershell -ExecutionPolicy Bypass -File .cortex\skills\pptx\scripts\export-slides.ps1 -PptxPath output.pptx -OutDir temp\qa`
2. Verify images exist: `glob "temp/qa/slide-*.jpg"` or `glob "temp/qa/Slide*.JPG"`
3. Dispatch a QA subagent to visually inspect every exported slide image
4. Fix any issues found, rebuild, and re-verify affected slides
5. Only declare success after a full pass reveals no new issues

Skipping this phase is the single most common cause of broken deliverables.

---

## References

- **QA workflow**: [references/qa-workflow.md](references/qa-workflow.md)
- **Gotchas**: [references/gotchas.md](references/gotchas.md)
- **Design ideas** (for creative decks): [references/design-ideas.md](references/design-ideas.md)
- **Research workflow**: [references/research-workflow.md](references/research-workflow.md)
- **Speaker notes requirements**: [references/speaker-notes-requirements.md](references/speaker-notes-requirements.md)
- **Config schema**: [references/config-schema.md](references/config-schema.md)
