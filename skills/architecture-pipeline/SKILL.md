---
name: architecture-pipeline
description: "End-to-end architecture pipeline: from a customer discovery transcript, generate current state, ingestion recommendations, future state architecture, and a final customer-facing draw.io diagram. Chains together current-state-generator, ingestion-pattern-recommender, future-state-generator, and drawio-architecture-generator in sequence. Use when: running the full architecture workflow, end-to-end architecture from transcript, full pipeline, discovery to draw.io, discovery to architecture, complete architecture package. Triggers: architecture pipeline, full pipeline, end to end architecture, discovery to drawio, discovery to architecture, complete architecture, run the pipeline, chain skills, full architecture workflow."
---

# Architecture Pipeline

End-to-end orchestrator that chains four skills in sequence to go from a raw customer discovery transcript to a polished, customer-facing draw.io architecture diagram. Each step produces a markdown file saved to disk, and each subsequent step reads the previous files as input.

## The Pipeline

```
┌─────────────────┐     ┌──────────────────────┐     ┌─────────────────────┐     ┌───────────────────────┐
│  STEP 1          │     │  STEP 2               │     │  STEP 3              │     │  STEP 4                │
│  Current State   │────▶│  Ingestion Patterns   │────▶│  Future State        │────▶│  Draw.io Diagram       │
│  Generator       │     │  Recommender          │     │  Generator           │     │  Generator             │
│                  │     │                       │     │                      │     │                        │
│  IN: transcript  │     │  IN: transcript +     │     │  IN: transcript +    │     │  IN: future state      │
│  OUT: 01-current │     │      01-current-state │     │      01 + 02 files   │     │      03 file           │
│       -state.md  │     │  OUT: 02-ingestion    │     │  OUT: 03-future      │     │  OUT: 04-drawio        │
│                  │     │       -patterns.md    │     │       -state.md      │     │       -diagram.md      │
└─────────────────┘     └──────────────────────┘     └─────────────────────┘     └───────────────────────┘
```

## What You Receive

**Required:**
- A customer discovery transcript (paste the full conversation)

**Optional:**
- Company name
- Any specific constraints or preferences (e.g., "they want to keep Airflow", "budget is tight")
- Which steps to run (default: all 4). User can say "skip to step 3" if they already have a current state.

---

## FILE SYSTEM PROTOCOL

### Output Directory
All files are saved to a single folder per customer:

```
~/Documents/architecture-pipeline/{company-name}/
├── 00-transcript.md          ← Original transcript (saved for reference)
├── 01-current-state.md       ← Step 1 output
├── 02-ingestion-patterns.md  ← Step 2 output
├── 03-future-state.md        ← Step 3 output
├── 04-drawio-diagram.md      ← Step 4 output (contains XML)
└── 04-architecture.drawio    ← Step 4 output (raw XML file, paste into draw.io)
```

If no company name is provided, use `unnamed-YYYY-MM-DD` as the folder name.

### File Naming
- Always use lowercase kebab-case
- Always prefix with step number (`00-`, `01-`, `02-`, `03-`, `04-`)
- This ensures files sort in pipeline order

### How Steps Read Previous Output
Each step MUST read the previous step's file(s) from disk using the Read tool before executing. This ensures context is carried forward even if the conversation context is long.

---

## Before Starting

1. Ask for the company name (for the folder name)
2. Create the output directory: `~/Documents/architecture-pipeline/{company-name}/`
3. Save the transcript as `00-transcript.md`
4. Confirm the plan:

> "Created `~/Documents/architecture-pipeline/{company-name}/`. I'll run 4 steps, saving each as a markdown file:
>
> 1. `01-current-state.md` — Current architecture + pain points
> 2. `02-ingestion-patterns.md` — Ingestion recommendations
> 3. `03-future-state.md` — Snowflake future state architecture
> 4. `04-drawio-diagram.md` + `04-architecture.drawio` — Customer-facing draw.io
>
> Ready to start?"

---

## STEP 1 — Current State Architecture

**Skill:** `current-state-generator`

**Read:** `00-transcript.md`

**Execute by following the current-state-generator methodology:**
- Extract architecture facts from the transcript (systems, layers, flows)
- Generate Mermaid diagram of current state
- Identify pain points (observed + inferred)
- Produce the Senior POV (FoDE-grounded)

**Write to `01-current-state.md`:**
```markdown
# Current State Architecture — {Company Name}
Generated: {date}

## Current State Mermaid

```mermaid
{MERMAID_CODE}
```

## Architecture Flow Explanation
{Numbered steps, end-to-end}

## Systems Inventory
| System | Layer | Protocol | Cadence | Hosting |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |

## Pain Points

### Observed (from transcript)
- {pain point with quote/paraphrase}

### Inferred (logically implied)
- {pain point with justification}

## Senior POV (FoDE-Grounded)

### Undercurrents Assessment
{Assessment against 6 undercurrents}

### Nine Principles Assessment
| Principle | Score | Justification |
|---|---|---|
| ... | Strong/Mixed/Weak/Unknown | ... |

## Evidence Map
| Diagram Element | Transcript Evidence | Confidence |
|---|---|---|
| ... | ... | High/Med/Low |

## Assumptions & Open Questions
- Assumptions: {bullets}
- Open Questions: {numbered}
```

**Present to user:** Summary of what was written + key pain points. Ask to proceed.

---

## STEP 2 — Ingestion Pattern Recommendations

**Skill:** `ingestion-pattern-recommender`

**Read:** `00-transcript.md` AND `01-current-state.md`

**Execute by following the ingestion-pattern-recommender methodology:**
- Use the source systems identified in Step 1 + the original transcript
- Categorize each source using the taxonomy
- Map each source to a Snowflake ingestion method
- Assess complexity and risks
- Generate implementation roadmap

**Write to `02-ingestion-patterns.md`:**
```markdown
# Ingestion Pattern Recommendations — {Company Name}
Generated: {date}
Input: 01-current-state.md

## Executive Summary
{2-3 sentences}

## Ingestion Map
| Source System | Category | Snowflake Method | Priority | Complexity | Phase |
|---|---|---|---|---|---|
| ... | ... | ... | ... | ... | ... |

## Source Analysis

### Source 1: {Name}
- **Category:** {taxonomy category}
- **Current state:** {how it works today}
- **Recommended method:** {Snowflake method}
- **Rationale:** {why this method}
- **Pain points addressed:** {which pain points from Step 1}

{repeat for each source}

## Complexity Assessment
- **Overall complexity:** {Low/Medium/High}
- **Rationale:** {why}

## Key Risks
| Risk | Severity | Mitigation |
|---|---|---|
| ... | ... | ... |

## Implementation Roadmap

### Phase 1: {Name} ({duration})
- {items}

### Phase 2: {Name} ({duration})
- {items}

## Discovery Questions
{Prioritized list of questions still to answer}
```

**Present to user:** Summary of ingestion map + key risks. Ask to proceed.

---

## STEP 3 — Future State Architecture

**Skill:** `future-state-generator`

**Read:** `00-transcript.md` AND `01-current-state.md` AND `02-ingestion-patterns.md`

**Key instruction:** The future state MUST use the ingestion methods recommended in `02-ingestion-patterns.md`. Do not contradict the ingestion recommendations.

**Execute by following the future-state-generator methodology:**
- Assess customer data maturity
- Decide on phasing (single-step vs phased) based on evidence
- Define the Snowflake future state architecture
- Select and validate features
- Generate future state Mermaid diagram

**Write to `03-future-state.md`:**
```markdown
# Future State Architecture — {Company Name}
Generated: {date}
Input: 01-current-state.md, 02-ingestion-patterns.md

## Architecture Summary
{2-3 sentences describing the target architecture}

## Customer Data Maturity Assessment
| Dimension | Level | Evidence |
|---|---|---|
| Technology | Low/Med/High | ... |
| Process | Low/Med/High | ... |
| Organizational | Low/Med/High | ... |

## Phasing Decision
**Decision:** {Single-step / Phased}
**Rationale:** {why}

## Future State Mermaid

```mermaid
{MERMAID_CODE}
```

## Component Inventory
| Component | Zone | Phase | Pain Point Addressed |
|---|---|---|---|
| ... | Sources / Ingestion / Snowflake / Consumption | 1 or 2 | ... |

## Snowflake Features Selected
| Feature | Category | Pain Point Addressed | Phase |
|---|---|---|---|
| ... | ... | ... | ... |

## Data Flow Description
{End-to-end numbered walkthrough}

## Architecture Explanation
{How each pain point is addressed, key trade-offs, why feasible for this customer}

## Features Intentionally Deferred
| Feature | Reason for Deferral |
|---|---|
| ... | ... |

## Open Questions
{Any remaining gaps}
```

**Present to user:** Architecture summary + Mermaid + feature table. Ask to proceed.

---

## STEP 4 — Draw.io Architecture Diagram

**Skill:** `drawio-architecture-generator`

**Read:** `03-future-state.md`

**Execute by following the drawio-architecture-generator methodology:**
- Parse the Mermaid diagram and component inventory from `03-future-state.md`
- Apply the anti-crossing system (all 8 rules)
- Generate customer-facing draw.io XML

**Key instruction:** This is a 1:1 visual translation of `03-future-state.md`. Do NOT add or remove components.

**Write TWO files:**

**`04-drawio-diagram.md`:**
```markdown
# Draw.io Architecture Diagram — {Company Name}
Generated: {date}
Input: 03-future-state.md

## Architecture Summary
{Same summary from Step 3}

## Arrow Routing Plan
| # | From → To | Phase | Crossing? | Mitigation |
|---|-----------|-------|-----------|------------|
| ... | ... | ... | ... | ... |

## Draw.io XML

```xml
{COMPLETE DRAW.IO XML}
```

## How to Open
1. Go to app.diagrams.net
2. File → New
3. Extras → Edit Diagram (or Ctrl+Shift+X)
4. Paste the XML above
5. Click OK

## Fidelity Notes
{Any differences between the Mermaid and the draw.io: aggregated arrows, zone reassignments}
```

**`04-architecture.drawio`:**
The raw XML file (just the `<mxfile>...</mxfile>` content, no markdown wrapping). This file can be opened directly in draw.io.

**Present to user:** Confirm both files written + how to open the `.drawio` file.

---

## Final Deliverable

After all 4 steps, present:

```
## ARCHITECTURE PIPELINE COMPLETE — {Company Name}

### Files Created:
~/Documents/architecture-pipeline/{company-name}/
├── 00-transcript.md          ✅
├── 01-current-state.md       ✅
├── 02-ingestion-patterns.md  ✅
├── 03-future-state.md        ✅
├── 04-drawio-diagram.md      ✅
└── 04-architecture.drawio    ✅

### Stats:
- Sources: {N}
- Ingestion methods: {N}
- Snowflake features: {N}
- Consumers: {N}
- Arrows in diagram: {N}
- Phases: {1 or 2}

### Next Steps:
- Open `04-architecture.drawio` in draw.io to review the diagram
- Review `03-future-state.md` for the full architecture rationale
- Check `02-ingestion-patterns.md` for implementation roadmap
```

---

## Handling Partial Runs

Users may want to start mid-pipeline or re-run a single step:

| User says | What to do |
|---|---|
| "Run the full pipeline" | Steps 1 → 2 → 3 → 4 |
| "I already have a current state, start from ingestion" | Ask user to place their file as `01-current-state.md` in the folder, then Steps 2 → 3 → 4 |
| "I already have a future state, just make the draw.io" | Ask user to place their file as `03-future-state.md`, then Step 4 only |
| "Re-run step 3 with changes" | User edits `02-ingestion-patterns.md`, re-run Step 3 → 4 |
| "Skip ingestion, go straight to future state" | Steps 1 → 3 → 4 (Step 3 reads `01-current-state.md` directly) |

When re-running a step, OVERWRITE the existing file and re-run all subsequent steps too.

## Iteration Protocol

After EACH step, the user may:
- **Approve and continue** — proceed to next step
- **Request changes** — user edits the file on disk, then re-run from that step
- **Add context** — provide additional info, append it to the current step's file, re-run
- **Skip ahead** — jump to a later step

---

## Quality Rules

1. **File-based context** — ALWAYS read previous files from disk before executing a step. Never rely on conversation memory alone.
2. **Consistency across steps** — The future state (Step 3) must use the ingestion methods from `02-ingestion-patterns.md`. The draw.io (Step 4) must match the Mermaid from `03-future-state.md`. No contradictions.
3. **Grounding** — Every component in `04-architecture.drawio` must trace back to `00-transcript.md`. No invented systems.
4. **Pause between steps** — Always present results and ask before proceeding. Never auto-run all 4 steps without showing intermediate results.
5. **Overwrite on re-run** — When re-running a step, overwrite the file and cascade to all subsequent steps.
6. **Confirm file writes** — After writing each file, confirm the path and file size to the user.
