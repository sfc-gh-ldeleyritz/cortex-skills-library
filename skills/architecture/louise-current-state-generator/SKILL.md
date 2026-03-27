---
name: current-state-generator
description: "Generate current-state architecture diagrams from customer discovery transcripts. Use when: analyzing customer conversations, creating as-is architecture diagrams, preparing discovery summaries. Triggers: current state, architecture diagram, discovery transcript, as-is architecture."
---

# Current State Architecture Generator

Generate precise, evidence-based current-state architecture diagrams from customer discovery conversations, grounded in data engineering best practices.

## Overview

This skill transforms customer discovery transcripts into:
1. **Mermaid diagram** of their current architecture
2. **Flow explanation** of how data moves end-to-end
3. **Pain points** (observed and inferred)
4. **Senior POV** grounded in "Fundamentals of Data Engineering" principles
5. **Image generation spec** for polished Lucidchart-style rendering

## Reference Book

This skill uses `fundamentals-of-data-engineering.pdf` as its knowledge base.

**Key sections to reference:**
- **Chapter 2**: Data Engineering Lifecycle + "undercurrents" (security, data management, DataOps, data architecture, orchestration, software engineering)
- **Chapter 3**: "Designing Good Data Architecture" + the 9 Principles of Good Data Architecture

When applying a principle, cite it as: `(FoDE Ch. X, principle name)`

## Workflow

### Step 1: Gather Inputs

**Required:**
- Customer discovery transcript (paste full conversation)

**Optional:**
- Company name
- Detail level: low | medium | high (default: high)
- Max nodes: 80 (default), hard limit 120

### Step 2: Extract Architecture Facts

Identify systems by layer:
| Layer | Examples |
|-------|----------|
| Sources | CRM, ERP, APIs, databases, SaaS apps |
| Ingestion/Transport | Kafka, Fivetran, Airbyte, custom ETL |
| Landing/Storage | Data lake, S3, Azure Blob, on-prem |
| Transform/Compute | Spark, dbt, Python, stored procs |
| Orchestration | Airflow, Prefect, cron, manual |
| Governance/Security | Access controls, encryption, lineage |
| Serving/Consumption | BI tools, dashboards, APIs, ML |
| Observability | Monitoring, alerting, logging |

**Capture for each data flow:**
- Protocol (API/JDBC/SFTP)
- Cadence (real-time/hourly/daily)
- Format (JSON/CSV/Parquet)
- Hosting (on-prem/cloud/provider)

**Evidence rule:** If uncertain, mark as `TBD` and create an Open Question.

### Step 3: Create Architecture Fact Sheet

Normalize into nodes and edges:

**Nodes:**
- id, label, category/layer
- owner (customer/vendor)
- hosting (if known)
- evidence snippet from transcript

**Edges:**
- from, to, verb (ingest/replicate/query)
- protocol/cadence/format labels
- evidence snippet

### Step 4: Generate Mermaid Diagram

Use subgraphs for each layer in order:
1. Sources
2. Ingestion & Transport
3. Landing / Storage
4. Transform / Compute
5. Serving / Consumption
6. Governance & Security
7. Orchestration & Observability

**Mermaid conventions:**
- Concise node IDs: `s1, i1, st1, t1, c1, g1, o1`
- Short but explicit labels: `"PostgreSQL (on-prem)"`, `"Kafka event bus"`
- Edge labels MUST include: cadence/protocol/format
  - Example: `"API pull, hourly, JSON"`
  - Example: `"JDBC, daily"`
- Left-to-right flow
- If layer not mentioned: include subgraph with `"Not mentioned (TBD)"` node

### Step 5: Explain the Flow

Write numbered step-by-step walkthrough:
1. What triggers ingestion?
2. Where does data land first?
3. How do transformations happen (batch vs streaming)?
4. How do results reach consumers?
5. Where does governance/monitoring happen (or what's missing)?

### Step 6: Identify Pain Points

**Observed** (directly from transcript):
- Quote or paraphrase the issue

**Inferred** (logically implied, clearly labeled):
- Justify why this is likely a problem

Categories to consider:
- Reliability/fragility
- Manual steps
- Scalability bottlenecks
- Tight coupling
- Latency mismatch
- Data quality gaps
- Lineage blind spots
- Access control gaps
- Cost opacity
- Environment drift

### Step 7: Senior POV (FoDE-Grounded)

#### A) Undercurrents Assessment (FoDE Ch.2)

Evaluate against the six undercurrents:
- Security
- Data Management
- DataOps
- Orchestration
- Software Engineering
- Architecture

#### B) Nine Principles Assessment (FoDE Ch.3)

For each principle, provide:
- **Score**: Strong / Mixed / Weak / Unknown
- **Justification**: 1-3 sentences referencing transcript facts
- **Probe question**: What to ask next

| Principle | Score | Justification | Probe |
|-----------|-------|---------------|-------|
| 1. Choose common components wisely | | | |
| 2. Plan for failure | | | |
| 3. Architect for scalability | | | |
| 4. Architecture is leadership | | | |
| 5. Always be architecting | | | |
| 6. Build loosely coupled systems | | | |
| 7. Make reversible decisions | | | |
| 8. Prioritize security | | | |
| 9. Embrace FinOps | | | |

### Step 8: Generate Image Spec

Produce a rendering spec for image generation:

**Layout:**
- Left-to-right swimlanes
- Vertical zones with headers (Sources → Ingestion → Storage → Consumption)
- Zones enclosed with subtle dashed borders

**Visual style:**
- Rounded rectangles, thin neutral borders
- White background, light gray containers
- Simple sans-serif font
- Real tooling logos (Salesforce, MySQL, Power BI, etc.)

**Connections:**
- Straight or right-angle arrows
- Clear arrowheads
- Edge labels where meaningful

**Forbidden:**
- No Snowflake branding
- No title text
- No emojis
- No marketing language

## Output Format

Return exactly these sections:

### SECTION 1 — Mermaid (CURRENT STATE)
```mermaid
{MERMAID_CODE}
```

### SECTION 2 — Architecture Flow Explanation
Numbered steps, end-to-end.

### SECTION 3 — Pain Points
- **Observed**: (from transcript)
- **Inferred**: (marked + justified)

### SECTION 4 — Senior POV (FoDE-grounded)
- A) Undercurrents assessment
- B) Nine principles assessment with scores

### SECTION 5 — Evidence Map
| Diagram Element | Transcript Evidence | Confidence |
|-----------------|---------------------|------------|
| ... | ... | High/Med/Low |

### SECTION 6 — Assumptions & Open Questions
- Assumptions (bullets, minimal)
- Open Questions (numbered, max 8)

### SECTION 7 — Image Generation Prompt
Copy-paste ready prompt for image generation.

## Quality Checks

Before finalizing, verify:
- [ ] Mermaid compiles (no syntax errors)
- [ ] LTR flow enforced
- [ ] Every node/edge has evidence OR marked TBD + open question
- [ ] No vendor/tool hallucinations
- [ ] Pain points distinguish Observed vs Inferred
- [ ] FoDE POV references principles correctly
- [ ] No future-state or Snowflake recommendations (unless asked)

## Non-Negotiables

1. **CURRENT STATE ONLY** - Do NOT propose future state unless explicitly asked
2. **Evidence-based** - No "typical" tools unless transcript confirms
3. **TBD over guessing** - If detail missing, mark TBD and add Open Question
4. **Valid Mermaid** - Must render without syntax errors
5. **Left-to-right flow** - Always LTR architecture

## Example Trigger Phrases

- "Generate current state from this discovery call"
- "Create an as-is architecture diagram"
- "Analyze this transcript and show me their architecture"
- "What does their current data stack look like?"
