---
name: sizing-email
description: "Generate a sizing questions email for a prospect based on discovery call context. Asks only what's missing for a consumption estimate, never re-asks known information. Use when: writing sizing questions, follow-up email after discovery, pre-estimate questionnaire, gathering sizing inputs, consumption estimate prep email. Triggers: sizing email, sizing questions, follow-up questions, consumption estimate email, write the sizing email, send sizing questions, pre-estimate email."
---

# Sizing Questions Email Generator

Generate a sizing questions email that asks a prospect only the questions needed to run a Snowflake consumption estimate, without re-asking anything already known from prior discovery.

## Tone

**Load** the `customer-email` skill for tone and formatting rules. All emails produced by this skill must follow those rules exactly.

**Additional sizing-specific rules:**
- Use numbered questions so they're easy to reply to inline
- Give brief context inline with each question so the recipient understands why you're asking without it feeling like a form
- Group questions under bold section headers that make it easy to delegate (e.g. "Stephen might be best for these, Mark for those")
- Where possible, offer examples or ranges to make answering easier (e.g. "would a nightly sync be sufficient or do you need near real time?")
- Close with a realistic timeline and an offer to jump on a call instead of typing answers
- Reference the board date or decision timeline if known, to frame urgency naturally
- Mention what the answers will be used for (the three-scenario consumption estimate)

**What NOT to do:**
- Do not ask about information already gathered from discovery calls or prior context
- Do not ask generic discovery questions (pain points, use cases, goals) as these are already known
- Do not include questions about OpenFlow unless explicitly told to include it
- Do not ask about Snowflake edition if it can be reasonably inferred from context
- Do not pad with unnecessary questions just to seem thorough

## Workflow

### Step 1: Gather Context

**Goal:** Understand what's already known and what's missing for the consumption estimate.

**Actions:**

1. **Read** the user's memory directory for any existing customer context files
2. **Read** any call transcripts or discovery notes the user provides or references
3. **Read** the consumption-estimate skill at `$HOME/.snowflake/cortex/skills/consumption-estimate/SKILL.md` to understand the full list of required inputs

### Step 2: Build the Known vs Missing Matrix

**Goal:** Map every consumption estimate input to either KNOWN (with source) or MISSING.

**Actions:**

1. From the consumption estimate skill, extract all required inputs:
   - Customer name, edition, cloud provider, region, credit rate
   - Data sources (type, count, sizes)
   - Ingestion method and frequency per source
   - Daily/monthly data volumes
   - Transformation complexity (tools, model count, frequency)
   - BI tool and mode (Import/DirectQuery/Mixed)
   - Concurrent users (peak and average)
   - Operating hours and days
   - Dev/test requirements
   - Storage requirements and retention
   - Growth projections
   - AI/ML workloads
   - Serverless features
   - Budget/target spend (if available)

2. For each input, check if it's already known from:
   - Call transcripts
   - Memory files
   - Prior conversations
   - Reasonable inference (e.g. edition can be inferred from compliance needs and feature requirements)

3. Present the matrix to yourself (do not show user). Only the MISSING items become email questions.

### Step 3: Draft the Email

**Goal:** Write the email containing only the questions for MISSING inputs.

**Actions:**

1. Write the email following the tone specification above
2. Number all questions sequentially across sections
3. For each question, include just enough context for the recipient to understand what you need and why (one sentence max)
4. Group questions under thematic bold headers
5. Where possible, offer examples or ranges to make answering easier (e.g. "would a nightly sync be sufficient or do you need near real time?")
6. Reference the board date or decision timeline if known, to frame urgency naturally
7. Mention what the answers will be used for (the three-scenario consumption estimate)

### Step 4: Present for Review

**Goal:** Get user approval before finalizing.

Present the draft email. If the user provides corrections, update and present again.

## Stopping Points

- After Step 3: Present draft email for user review before finalizing

## Output

A markdown file containing the email, ready to copy-paste into an email client. Save to the customer's working directory if one is apparent from context.
