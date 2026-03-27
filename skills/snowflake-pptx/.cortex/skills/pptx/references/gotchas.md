# Gotchas

Operational knowledge from past sessions. Read before starting any deck generation.

## Pixel-Perfect Gotchas

1. **Use wrapper scripts, NOT raw `dotnet` commands.** Environment variable propagation fails in cmd.exe with `&&` chaining. Always use `.cortex\skills\pptx\scripts\build.cmd` (Windows) or `.cortex/skills/pptx/scripts/build.sh` (macOS/Linux).

2. **QA subagents MUST read image files directly.** Do not trust subagent findings that don't reference specific visual observations. If a subagent says "looks good" without mentioning concrete details from the image, it likely didn't load the image.

3. **NEVER edit spec.yaml with parallel `edit` calls.** Parallel edits to the same YAML file will corrupt it. If you need to fix 3+ fields, use the `write` tool to rewrite the entire file with all fixes pre-applied. This is the single most common cause of file corruption.

4. **`quote_simple` attribution wraps if longer than ~30 chars.** Keep attributions short (validator allows 40, but visual wrap starts earlier).

5. **`split` slide `title` is half-width: max 30 chars.** Move detail into `content_title`.

6. **Table slides: 5 rows max.** 5 columns causes tight spacing; prefer 3-4 columns.

7. **`export-slides.ps1` output filenames** are `Slide1.JPG` through `SlideN.JPG` (capital S, no leading zeros, `.JPG` extension). The QA section references `slide-01.jpg` format; glob for both patterns: `glob "temp/qa/slide-*.jpg"` and `glob "temp/qa/Slide*.JPG"`.

8. **`dotnet` not found in WSL/bash.** The .NET SDK is installed on Windows, not in WSL. Use the full Windows path: `"/mnt/c/Program Files/dotnet/dotnet.exe"`. The wrapper scripts handle this automatically — always prefer wrapper scripts over direct `dotnet` invocation.

9. **Divider slide capitalization**: Both `section` and `chapter_particle` auto-uppercase titles and subtitles. If they don't match visually, check ContentInjector.cs for the uppercase transform list.

10. **Agenda items use single-key dict format.** Each agenda item with subitems must be `"Title": {subitems: [...]}` (one key = title text). The format `{title: "...", subitems: [...]}` (two keys) is accepted as a fallback but is error-prone and generates a validator warning.

11. **`four_column_numbers` font sizing is dynamic.** Content descriptions use a sliding scale (16pt -> 14pt) based on text length. Validator allows up to 90 chars (23 chars/line x 3 lines). Target ~40-55 chars for best visual fill at 16pt. Number values use `NumberSizing.NumberThresholds`. Do NOT flatten the font table to a single size — it removes graceful degradation for longer text. If the user wants bigger fonts, reduce the text length instead.

12. **Title slide subtitle overlaps headshot** when subtitle exceeds ~50 chars. If the presenter has a headshot (`title_headshot`), keep the subtitle under 50 characters. The validator enforces this limit.

13. **Divider subtitles should be short phrases, not sentences.** `section` and `chapter_particle` subtitles are limited to 60 chars. Use 6-8 words max — these slides are visual breaks, not content carriers.

14. **Content slide titles wrap at ~40-45 chars** causing subtitle overlap. The validator limits content titles to 45 chars and warns when text would wrap to 2+ lines at the title font size (~30 chars/line).

15. **macOS first-run: scripts may lack execute permission.** Run `chmod +x .cortex/skills/pptx/scripts/*.sh` if you get "Permission denied". The `build-and-validate.sh` script self-heals this on subsequent runs.

## Creative Gotchas

1. **Must `npm install` before first build.** Check for `node_modules/` directory. If missing: `cd .cortex/skills/pptx/scripts/node/SnowflakeCreativePptx && npm install`.

2. **`icon-grid` requires exactly 4 or 6 items.** 5 is a validation error; there is no 5-item grid layout.

3. **`timeline` requires 3-5 steps.** 2 steps is not supported; use `section` + `columns` instead.

4. **`comparison` max 5 bullets per panel.** More than 5 is a validation error.

5. **Use the shared `export-slides.ps1` for image export**, not inline PowerShell COM snippets. The script handles COM timing issues and process cleanup.

6. **cmd.exe path failures**: Do not chain commands with `&&` on Windows when paths contain spaces or special characters. Use the `creative-build.cmd` wrapper script instead of invoking `node` directly from cmd.exe.

7. **Glob for exported slide assets**: PowerPoint exports use `Slide1.JPG` (capital S, no zero-padding, `.JPG`), while LibreOffice uses `slide-01.jpg`. Always glob for both patterns: `Slide*.JPG` and `slide-*.jpg`.

8. **Character limit headroom**: Timeline step body text truncates visually above ~50 characters even though the validator allows 120. Keep timeline bodies short and punchy. Similarly, comparison panels with <=2 bullets per side look sparse; aim for 3 bullets.
