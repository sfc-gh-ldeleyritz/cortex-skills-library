---
name: technical-discovery-research
description: "Create a decision-grade, citation-backed company intelligence brief for a Snowflake technical discovery using Gap Selling. Use when: customer research, account research, prospect research, pre-call research, discovery prep, decision brief, stakeholder profile, org chart influence map. Triggers: technical discovery research, company intelligence report, discovery brief, account intelligence, stakeholder focus, gap selling, pre-call brief."
---

# Technical Discovery Research

## When to Load
Load this skill when the user wants a reusable, high-signal, fact-checked research brief on any company and a specific stakeholder, designed to drive a Snowflake discovery call.

## Operating Rules (Non-Negotiable)
- Do **not** invent facts, metrics, customers, quotes, dates, or citations.
- Every factual claim must have a credible citation marker like `‹#›` that maps to the **Source Index**.
- If a detail cannot be verified quickly from credible sources, label it **Speculation** or **Unknown**.
- Prefer primary sources (SEC filings, official site, investor materials, government registries) over blogs.
- Keep tone neutral and decision-grade (no marketing fluff).

## Inputs to Collect (Ask User)
Collect these as free-text fields:
- Company (legal / brand name)
- Stakeholder full name + title (or "Unknown")
- Meeting context (optional): why we are meeting, region, account segment
- Output length target (default: ~650 words)

## Research Workflow

### Step 1 — Source Gathering
Use web research to gather credible sources for:
- Company basics: legal name, HQ, incorporation year, employee count, revenue (if public), geographies
- Product + GTM: offerings, pricing model, customer segments, monetization
- Technology + data posture signals: job posts, engineering blogs, architecture posts, partner pages
- Growth signals: funding, investors, acquisitions, expansion, new exec hires
- Stakeholder signals: LinkedIn, talks, interviews, publications

**Minimum expectation:** at least 1 credible source per major claim.

### Step 2 — Build the Brief (Markdown)
Produce **exactly** this structure:

#### TL;DR (≤ 75 words)

### 1. Snapshot
- Legal name, HQ, incorporation year
- Industry & sub-sector
- Business model
- Size: revenue, employee count, global presence
- Who Snowflake is talking to (titles + dept)

### 2. Core Product & Value Proposition
- Flagship products or services
- Primary users and customer segments
- Stated value or mission (business terms)
- Pricing / go-to-market model
- API / data monetization angle (if relevant)

### 3. Tech Stack & Data Posture
- Known tools: warehouse, BI, ETL/ELT, orchestration, CDP, API gateway (only if sourced)
- Architecture patterns: lake/lakehouse/mesh/monolith/microservices (label unknown if not sourced)
- Cloud providers, multi-cloud, hybrid
- Current warehouse (if known)
- AI/ML maturity: none / experimenting / production (must be supported)
- Known pain points: latency, duplication, governance, complexity (only if evidenced)
- Analyst personas and BI tools

### 4. Growth Stage & Strategic Direction
- Stage: seed/series/public/PE-owned (cite)
- Major funding rounds and investors
- Acquisitions / pivots
- Expansion signals
- Where they are trying to go (explicitly stated strategy or clearly signaled)

### 5. Org Chart & Influence Map
- Key stakeholders (names/titles if public)
- Decision-makers vs influencers
- IT vs business ownership of analytics
- Data team structure (centralized/decentralized/embedded) (speculation if not sourced)

#### Stakeholder Focus: {Full Name, Title}
- Role in buying process (DM/champion/evaluator) (speculation allowed, label)
- Tenure, prior roles/companies (cite)
- Public signals (posts/interviews/blogs)
- Technical vs business orientation
- Likely objections
- Quote/POV that hints at mindset (only if sourced)
- Rapport hooks (schools/interests/mutuals) (only if sourced)

### 6. Public Signals & Reputation
- Analyst coverage and review signals (Gartner/Forrester/G2 etc.)
- Strengths (sourced)
- Negative signals (outages, layoffs, lock-in concerns) (sourced)
- Awards / PR credibility signals

### 7. Recent Moves & Catalysts (Last 18 Months)
- Exec hires
- Tech migrations / architecture shifts
- New launches
- Partnerships
- Job postings that hint at direction

### 8. Strategic Implications for My Discovery Call
Provide 3–5 bullets:
- Topics to probe (tie to gaps between current vs future)
- Likely Snowflake fits (map to Snowflake capabilities)
- Red flags / blockers
- Likely partners/competitors in their stack

### 9. Parallel Case Studies & Success Stories
Provide 2–3 Snowflake customer examples with:
- Customer name
- Before Snowflake (stack/pain)
- What they implemented
- Outcomes / KPIs
- Source link `‹#›`
Then 2–3 sentences each on why it parallels this prospect.

### 10. Intel Gaps & Open Questions
- Unverified assumptions
- Questions to ask
- Potential blockers

### 11. Rapport Hooks (Optional)
1–3 niche details with citations.

### Source Index
Numbered list of all sources referenced.

### Deal Sizing Signals
Estimate likely deal size using:
- Data volume
- Ingestion frequency/complexity
- BI usage (internal vs customer-facing)
- AI/ML intent
- Funding/growth stage
- Product scope/customer count
- Technical buying power
- Budget likelihood and urgency (3-month risk)

Conclude with **Deal Potential Verdict**:
- Likely >$25K (invest time)
- Grey zone ($10–25K, explore ROI upside)
- Likely <$10K (flag to AE and consider no-go)

## Output
A single markdown report that is decision-grade, ~650 words (unless user overrides), with `‹#›` citations and a numbered **Source Index**.
