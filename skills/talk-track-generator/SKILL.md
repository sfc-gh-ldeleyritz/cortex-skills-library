---
name: talk-track-generator
description: "Generate compelling verbal talk tracks for Snowflake features. Use when: explaining features to prospects, preparing demo scripts, sales engineering conversations. Triggers: talk track, demo script, explain feature, pitch snowflake."
---

# Snowflake Talk Track Generator

Generate natural, conversational explanations for Snowflake features that sound like a real person talking—not a LinkedIn post or marketing copy.

## Overview

This skill creates talk tracks in **Nicholas Style**—patient, step-by-step explanations that anchor to familiar concepts. The goal is to sound like a thoughtful colleague explaining something over coffee, not a salesperson delivering a pitch.

**CRITICAL TONE GUIDANCE:**
- Write how people actually talk in meetings
- Use natural transitions: "So...", "The thing is...", "And here's where it gets interesting..."
- Avoid punchy one-liners that sound clever but fake (e.g., "That problem? Gone." or "...and it's just included.")
- Don't be sassy or performative
- It's okay to be a bit meandering—real explanations aren't perfectly polished

## Workflow

### Step 1: Gather Context

Before generating, collect these inputs using ask_user_question:

**Required:**
- Feature to explain (e.g., "Dynamic Tables", "Cortex Analyst", "Snowpark Container Services")

**Prospect Context:**
- Industry (e.g., Financial Services, Healthcare, Retail)
- Role/Audience (Data Engineer, Analyst, Executive, Architect)
- Technical Level (Low / Medium / High)
- Current Pain Points (what problems they're solving)
- Current Stack (what tools they use today)
- Anticipated Objections (cost, security, complexity, lock-in)

### Step 2: Apply Nicholas Style (Default)

**Always use Nicholas Style** — patient, step-by-step, anchoring to familiar concepts.

Characteristics:
- Patient, unhurried explanations
- Anchor new concepts to things they already know
- Phrases like "the thing is...", "and here's where...", "the nice thing about this is..."
- Honest about limitations when relevant
- Sounds like you're explaining to a smart colleague, not pitching

**What Nicholas Style sounds like:**
> "So for S3, instead of you scheduling jobs to pick up files, Snowpipe just listens for S3 events. File lands, Snowpipe sees it, loads it. You don't have to manage that process."

**What to AVOID (too punchy/sassy):**
> "S3 files? Snowpipe handles it automatically. Zero cron jobs. Zero headaches. Done."

### Step 3: Generate Using 5-Part Structure

```
1. HOOK (1-2 sentences)
   - Connect to their specific pain point or goal
   - Make them lean in

2. ANCHOR (1-2 sentences)
   - Link to a concept they already know
   - "[New thing] is like [familiar thing], but [key difference]"

3. CORE EXPLANATION (3-5 sentences)
   - What it does (mechanically)
   - Why it matters (value)
   - What they DON'T have to do anymore

4. THEIR WORLD (2-3 sentences)
   - "In your case..." or "For [their industry]..."
   - Concrete example using their context

5. BRIDGE (1-2 sentences)
   - Addresses likely objection OR
   - Sets up next natural question OR
   - Opens door for deeper conversation
```

### Step 4: Apply Explanation Patterns

Weave in these proven patterns:

**Anchor to Familiar:**
> "[New concept] is like [familiar concept], but [key difference]"
- Dynamic Tables → "Like a materialized view, but intelligent"
- Cortex Analyst → "Like ChatGPT, but grounded in your data"
- Model Registry → "Like a Git repo for your ML models"

**Demystification:**
> "[Scary term] is just [simple explanation]"
- "Think of it as just a YAML file ultimately"
- "These are effectively just SQL functions"
- "It's just SQL"

**What You DON'T Have To Do:**
> "You don't need to [traditional burden]. Snowflake handles [automation]"
- "No scheduling, no external orchestration"
- "No cluster to manage, no DBA needed"

**Under the Hood Transparency** (for technical audiences):
> "Here's what's actually happening: [technical detail]"

**Security-First Positioning:**
> "Your data never leaves Snowflake"
> "The model comes to your data, not the other way around"

**Progressive Complexity:**
> "Start with [simple]. Once that's working, move to [advanced]"

### Step 5: Handle Objections

| Objection | Response Pattern |
|-----------|------------------|
| "We don't have technical expertise" | "If your team knows SQL, they're ready" |
| "What about cost?" | Reframe to TCO, highlight eliminated costs |
| "What about security?" | "Data never leaves Snowflake", contrast with API models |
| "We already have [competitor]" | Respect competitor, suggest coexistence |
| "Vendor lock-in concerns" | Lead with open formats (Iceberg), data portability |
| "Sounds complex" | Demystify, show what's automated, "just SQL" |

## Output Format

Generate as natural spoken language—how you would actually say it in a meeting:

1. **The Talk Track** (200-400 words, conversational)
   - Should sound like a real person explaining something
   - Use full sentences, not punchy fragments
   - Include natural transitions and slight tangents
   - Okay to be a bit verbose—real speech isn't perfectly edited

2. **Key Phrases to Emphasize** (2-3 moments that land naturally)
   - These should NOT be clever one-liners
   - They're the clearest, most helpful statements

3. **Anticipated Follow-Up Questions** (2-3 with conversational responses)

## Feature Anchors Reference

| Feature | Best Anchor | Key Differentiator |
|---------|-------------|-------------------|
| Dynamic Tables | Materialized view | Automatic incremental pipeline, declarative |
| Cortex Analyst | ChatGPT for data | High accuracy with semantic model |
| Semantic Views | Regular SQL views | Adds business meaning layer |
| ML Functions | SQL functions | No Python required, just SQL |
| Cortex Search | Vector search | Inherits Snowflake RBAC |
| Snowflake Intelligence | ChatGPT | Grounded in YOUR data, agentic |
| Snowpark Container Services | Docker deployment | Same security/governance |
| Iceberg Tables | Open table format | Zero lock-in |
| Notebooks | Jupyter | Native Snowflake, no setup |
| Streams & Tasks | CDC + Cron | Managed, no external scheduler |

## Final Checklist

Before delivering, verify:
- [ ] Opens with their pain point (not feature name)
- [ ] Anchors to something they already know
- [ ] Explains both WHAT and WHY
- [ ] Includes "what you don't have to do" element
- [ ] Contains concrete example from their context
- [ ] Addresses likely objection
- [ ] Sounds like natural speech, not marketing copy
- [ ] Under 300 words (concise is confident)

## Example

**Input:**
- Feature: Dynamic Tables
- Industry: E-commerce  
- Role: Data Engineering Manager
- Pain Point: Too much time maintaining ETL pipelines
- Current Stack: Airflow + DBT + Postgres

**Output (Nicholas Style):**

"So you mentioned your team spends a lot of time on those Airflow DAGs, debugging failures, figuring out why something didn't run. Dynamic Tables are basically Snowflake's answer to that.

The way I'd think about it is—it's like a materialized view, but smarter. You write the SQL transformation, same as you would today, nothing new to learn there. But instead of you setting up the schedule, managing dependencies, handling retries when something fails, you just tell Snowflake how fresh you need the data. Like, 'I want this refreshed within an hour.' And then Snowflake figures out the rest.

What it's actually doing under the hood is looking at your SQL, working out what the dependencies are, and only processing the rows that actually changed. So it's not just blindly re-running everything on a schedule.

For your e-commerce stuff—your order data, inventory, customer activity—instead of someone having to debug a DAG at 2am, the pipeline just kind of takes care of itself. Your team can work on other things.

And you're not giving up visibility. The SQL is still yours, you can see exactly what it's doing. You're just not writing all the orchestration code yourself anymore.

Does that make sense? Happy to show you what one looks like."

**Key Phrases:**
- "You just tell Snowflake how fresh you need the data... and then Snowflake figures out the rest."
- "It's not just blindly re-running everything on a schedule."
- "You're not giving up visibility. The SQL is still yours."
