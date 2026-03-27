---
name: demo-briefing
description: |
  **[USER-INVOCABLE]** Generate a polished HTML pre-demo briefing document for a customer demo.
  Synthesizes discovery notes, deployed demo assets, and sales context into a single self-contained
  HTML page the SE can review before presenting. Covers: business context, personas, pain points,
  architecture, demo contents, need-to-demo mapping, suggested demo flow with talk tracks, prepared
  Talk2Data questions, deployed asset links, and points of attention.
  Use this skill whenever the user wants a pre-demo briefing, demo prep document, cheat sheet
  before a customer meeting, or an HTML summary of what a demo does and why it matters.
  Triggers: demo briefing, pre-demo briefing, briefing HTML, demo cheat sheet, demo prep,
  what does this demo do, prepare me for the demo, brief me, briefing document, explain the demo,
  demo summary HTML.
user-invocable: true
---

# Demo Briefing

**Slash Command:** `/demo-briefing`

**Output:** A self-contained HTML file (`demo_briefing_{customer}.html`) in the customer's project directory.

## When to Use

- After a demo has been deployed (via `/demo-deploy` or manually)
- When the SE needs to prepare for a customer presentation
- When someone asks "what does this demo do and why does it matter for this client?"

## Workflow

```
Step 1: Locate Sources (discovery notes + demo config + deployed assets)
Step 2: Extract Intelligence (business context, personas, pain points, architecture, use cases)
Step 3: Inspect Deployed Demo (entities, agent, search, streamlit, semantic view)
Step 4: Generate HTML Briefing
Step 5: Open in Browser & Confirm
```

### Step 1: Locate Sources

**Goal:** Find all available context about the customer and the demo.

**Actions:**

1. **Search** the current working directory for:
   - Discovery/deal notes (`.md`, `.txt`, `.docx` files with customer/deal/discovery/notes in the name)
   - `demo_config.yaml` (in `demos/*/` or current directory)
   - `research_summary.json` (from `/demo-research`)
   - `README.md` in the demo directory (from `/demo-script`)
   - Memory files in `/memories/` mentioning the customer

2. **Read** all found files. These are the raw intelligence sources.

3. If `demo_config.yaml` is found, extract:
   - `demo.name`, `demo.customer_name`, `demo.industry`
   - `demo.branding` (primary_color, secondary_color, accent_color)
   - `entities[]` with names, types, row_counts, columns
   - `agent.name`, `agent.system_prompt`, `agent.sample_questions`
   - `semantic_model.verified_queries`
   - `demo.locale`

4. If no demo_config.yaml exists, ask the user to point to:
   - The customer notes file
   - The Snowflake database name where the demo lives

**Output:** Collected raw intelligence.

### Step 2: Extract Intelligence

**Goal:** Structure the raw intelligence into briefing sections.

From the discovery notes and config, extract:

| Section | What to Extract |
|---------|----------------|
| **Business Context** | Company description, transformation/project, critical KPIs & targets, timeline, urgency level |
| **Personas** | Name, role, what they care about, what they want to see, tags (e.g., Decision maker, Technical, Champion) |
| **Pain Points** | Top 3-5 challenges ranked by priority, with concrete business impact for each |
| **Architecture** | Current stack (data sources), target architecture, ingestion layer, platform, output layers |
| **Use Cases** | Prioritized list of use cases mentioned in discovery |
| **Decision Criteria** | What matters most to the buyer (ease of use, speed, cost, etc.) |
| **Partners** | System integrator, consultants, champions involved |
| **Customer References** | Similar customer wins to mention |
| **Topics to Avoid** | Pricing details, future phases, out-of-scope features |

### Step 3: Inspect Deployed Demo

**Goal:** Get concrete details about what's deployed in Snowflake.

**Actions:**

1. From `demo_config.yaml`, derive the database name: `{DEMO_NAME}_DB` (uppercase, underscores).

2. **Run SQL** to verify deployed objects:

```sql
-- Check database exists
SHOW DATABASES LIKE '{DATABASE}';

-- List analytics tables with row counts
SELECT TABLE_NAME, ROW_COUNT
FROM {DATABASE}.INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'ANALYTICS' AND TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Check agent
SHOW CORTEX AGENTS IN {DATABASE}.AGENTS;

-- Check search services
SHOW CORTEX SEARCH SERVICES IN {DATABASE}.AGENTS;

-- Check semantic views
SHOW VIEWS LIKE '%SEMANTIC%' IN {DATABASE}.ANALYTICS;

-- Check streamlit
SHOW STREAMLIT LIKE '%{DEMO_NAME}%';
```

3. Build the **Deployed Assets** inventory:
   - Database name & schemas
   - Agent fully-qualified name
   - Semantic View fully-qualified name
   - Cortex Search service name
   - Streamlit app name & URL (derive from account: `https://app.snowflake.com/{ORG}/{ACCOUNT}/#/streamlit-apps/{APP_FQN}`)

4. Build the **Data Entities** table:
   - For each analytics table: name, type (dimension/fact/document), row count, key columns, why it matters

### Step 4: Generate HTML Briefing

**Goal:** Produce a polished, self-contained HTML file.

**Load** `references/html-template-guide.md` for the HTML structure and CSS.

**The HTML MUST contain these sections (in order):**

1. **Header** — Customer name, date, SE name, partner name. Use customer branding colors from config (or Snowflake blue/navy default).

2. **Business Context** — Highlight box with company situation, key stats (revenue targets, employee counts, timeline) as stat cards.

3. **Personas** — Card grid with avatar initials, name, role, what they care about, and tags. One card per person identified.

4. **Pain Points / Problems** — Danger/warning cards for each pain point with concrete business impact bullets.

5. **Target Architecture** — Visual flow diagram (HTML/CSS, no images) showing: Sources → Ingestion → Snowflake Hub → Output layers. Use `arch-box` styled divs with arrows.

6. **What the Demo Does** — Success cards for each component:
   - Data entities table (entity, type, row count, description, why it matters)
   - Pipeline description (dbt layers)
   - Agent description (what it combines: Analyst + Search)
   - Streamlit app pages

7. **Need-to-Demo Mapping Table** — 3-column table: `Identified Need / Pain Point` | `What We Show` | `Key Message`. One row per need. Use colored tags for priority (PRIO, CORP, UC, FUT).

8. **Suggested Demo Flow** — Numbered steps with:
   - Step title and duration
   - Description of what to show
   - Talk track in italics (a quote the SE can say verbatim)
   - Typical flow: Context → Dashboard → Pain Point demo → Talk2Data (THE STAR, longest) → CoCo/Ease → Next Steps

9. **Prepared Talk2Data Questions** — Question boxes with the question text and a "why this question" note explaining which persona/need it addresses. 5-8 questions.

10. **Links & Access** — Link boxes for Streamlit app, Agent, Database with descriptions.

11. **Points of Attention** — Warning banner for synthetic data caveat. Cards for topics to avoid and customer references to share.

12. **One-Sentence Summary** — Highlight box with THE key message for this customer in one sentence.

13. **Footer** — Date, SE name, "Snowflake".

**Language:** Match the customer's locale from config. If `fr_FR`, write the briefing in French. If `en_US` or unspecified, write in English.

**Branding:** Use customer colors from `demo.branding` for highlight boxes and accent elements. Always keep Snowflake blue (#29B5E8), navy (#0D2137), dark (#11567F) as the primary palette.

**File name:** `demo_briefing_{customer_name_lowercase}.html` in the customer's project directory (same level as the notes files, NOT inside demos/).

### Step 5: Open in Browser & Confirm

**Actions:**

1. **Write** the HTML file
2. **Open** it in the browser using `open_browser` tool
3. **Confirm** to the user: file path, what sections are included, and that it's ready

## Stopping Points

- After Step 1 if no discovery notes AND no demo_config found (ask user)
- After Step 5 for final review

## Output

A single self-contained HTML file with:
- Professional Snowflake-branded design (CSS variables, responsive grid, cards)
- Zero external dependencies (no CDN, no images, pure HTML+CSS)
- All intelligence synthesized from discovery notes + deployed demo
- Ready to open in any browser, print to PDF, or share

## Notes

- The HTML should be ~400-500 lines. It's a briefing, not a novel.
- Prefer concrete numbers and quotes from discovery notes over generic statements.
- Talk tracks should sound natural and conversational, not corporate.
- If the demo hasn't been deployed yet, skip Step 3 SQL queries and mark deployed assets as "TBD".
- If branding colors aren't available, default to Snowflake palette with a neutral dark accent.
