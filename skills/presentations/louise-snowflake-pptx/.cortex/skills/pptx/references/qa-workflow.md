# QA Workflow (Required)

**Assume there are problems. Your job is to find them.**

Your first render is almost never correct. Approach QA as a bug hunt, not a confirmation step. If you found zero issues on first inspection, you weren't looking hard enough.

## Content QA

**Step 1: Validate spec before building** (catches content overflow before the build step):

```cmd
REM Windows
.cortex\skills\pptx\scripts\validate-spec.cmd spec.yaml

REM macOS/Linux/WSL
.cortex/skills/pptx/scripts/validate-spec.sh spec.yaml
```

Or use the combined pipeline that does everything:

```cmd
.cortex\skills\pptx\scripts\build-and-validate.cmd spec.yaml output.pptx
```

Fix any errors (overflow-critical fields) and warnings before proceeding. Errors will block generation.

### Fixing Validation Errors

**CRITICAL: If `validate-spec` returns 3+ errors, do NOT use individual `edit` calls on spec.yaml.
Instead, rewrite the entire `spec.yaml` using the `write` tool with all fixes applied.
Individual edits are unreliable when YAML has repeated patterns across slides.**

If `validate-spec` returns **3+ errors**, do NOT use individual `edit` calls on spec.yaml.
Instead, rewrite the entire `spec.yaml` using the `write` tool with all fixes applied.
Individual edits are unreliable when YAML has repeated patterns across slides.

After ANY edit to spec.yaml, ALWAYS re-run `validate-spec` to confirm the fix took effect.
If the same error persists, the edit did not apply — use `write` to replace the full file.

**Step 2: After building, validate the rendered PPTX for structural issues:**

```cmd
REM Windows
.cortex\skills\pptx\scripts\validate.cmd output.pptx

REM macOS/Linux/WSL
.cortex/skills/pptx/scripts/validate.sh output.pptx
```

**When using templates, check for leftover placeholder text** by opening the file in PowerPoint or converting to text and grepping for `xxxx`, `lorem`, `ipsum`.

## Visual QA

**Use subagents**: even for 2-3 slides. You've been staring at the code and will see what you expect, not what's there. Subagents have fresh eyes.

### Batched QA for Large Decks

For decks with **10+ slides**, dispatch multiple QA subagents in parallel, each covering 6-8 slides:
- Agent 1: slides 1-6
- Agent 2: slides 7-12
- Agent 3: slides 13-17 (or remaining)

For decks with **<10 slides**, a single subagent is sufficient. If a subagent fails or cannot load all images, re-dispatch for the missing slides individually.

### Visual QA Pre-Check

Before inspecting slides, verify the images exist:
1. Run `glob "temp/qa/slide-*.jpg"` (or `*.JPG`)
2. If no files found, the export failed — re-run the export script
3. Count the images — should match your slide count (exclude template/hidden slides)

## Converting to Images

Convert presentations to individual slide images for visual inspection. Output to `temp/qa/` so images are co-located with the deck under review.

Use PowerPoint's own renderer for pixel-accurate output identical to what presenters see.

### Windows — PowerShell via bundled script

Use the pre-built script that handles COM startup timing and avoids known HRESULT errors:

```cmd
mkdir temp\qa 2>nul
powershell -ExecutionPolicy Bypass -File .cortex\skills\pptx\scripts\export-slides.ps1 -PptxPath output.pptx -OutDir temp\qa
```

The script automatically kills existing PowerPoint processes, uses correct integer constants (not COM enums), and adds startup delays to prevent `HRESULT 0x80048240` errors.

### macOS — LibreOffice + PyMuPDF

Requires LibreOffice (`brew install libreoffice`) and PyMuPDF (`pip install pymupdf`).
Converts via PDF (preserves fonts and layout faithfully) then renders each page to JPEG.

```bash
mkdir -p temp/qa
python3 - << 'PYEOF'
import fitz, pathlib, subprocess, sys, tempfile

pptx = str(pathlib.Path("output.pptx").resolve())
out_dir = pathlib.Path("temp/qa")
out_dir.mkdir(parents=True, exist_ok=True)

with tempfile.TemporaryDirectory() as tmp:
    r = subprocess.run(
        ["soffice", "--headless", "--convert-to", "pdf", "--outdir", tmp, pptx],
        capture_output=True, text=True
    )
    pdfs = list(pathlib.Path(tmp).glob("*.pdf"))
    if not pdfs:
        print("ERROR: LibreOffice produced no PDF\n" + r.stderr); sys.exit(1)
    doc = fitz.open(str(pdfs[0]))
    for i, page in enumerate(doc):
        mat = fitz.Matrix(1280 / page.rect.width, 720 / page.rect.height)
        page.get_pixmap(matrix=mat).save(str(out_dir / f"slide-{i+1:02d}.jpg"))
        print(f"  slide-{i+1:02d}.jpg")
    page_count = len(doc)
    doc.close()
print(f"Done — {page_count} slides written to {out_dir}/")
PYEOF
```

This creates `temp/qa/slide-01.jpg`, `temp/qa/slide-02.jpg`, etc.

To re-render after fixes, re-run the same command — PowerPoint/LibreOffice overwrites existing files.

## Visual Inspection Prompt

Convert slides to images (see above), then dispatch a subagent. See [qa-agent-prompt.md](qa-agent-prompt.md) for the inspection prompt template.

## Verification Loop

1. Generate slides -> Convert to images -> Inspect
2. **List issues found** (if none found, look again more critically)
3. Fix issues
4. **Re-verify affected slides**: one fix often creates another problem
5. Repeat until a full pass reveals no new issues

**Do not declare success until you've completed at least one fix-and-verify cycle.**

## Creative-Specific QA Checklist

When reviewing creative PPTX output, additionally check:

- [ ] **Timeline**: Step body text is fully visible (not cut off at column edges)
- [ ] **Section dividers**: Subtitle text is legible size (>=20pt) and vertically centered
- [ ] **Agenda**: Items fill the vertical space proportionally (no large empty gaps)
- [ ] **Icon-grid**: Cards without icons have a compact banner (not a large blank accent bar)
- [ ] **Comparison**: Bullets are spaced to fill the panel; text is not shrunk unreadably small
- [ ] **Columns bordered**: Two-column bordered layouts use 16pt body text (not 14pt)
- [ ] **Content density**: Run `validate` and review warnings for sparse layouts or long text
