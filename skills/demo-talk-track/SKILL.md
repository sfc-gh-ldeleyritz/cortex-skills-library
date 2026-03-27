---
name: demo-talk-track
description: "Generate demo talk tracks for live Snowflake demonstrations. Use when: preparing for customer demos, creating walkthrough scripts, running live demos. Triggers: demo script, demo talk track, walkthrough, live demo, customer demo."
---

# Snowflake Demo Talk Track Generator

Transform demo scripts or feature walkthroughs into compelling, customer-centric talk tracks for live demonstrations.

## Overview

This skill creates demo talk tracks that connect every click to customer value. It's NOT just what to show—it's what to SAY while showing it, how to create wow moments, and how to handle questions mid-demo.

## Workflow

### Step 1: Gather Inputs

**Demo Content (required):**
- Demo script or list of features to demonstrate
- Screenshots/descriptions of what's being shown
- Or describe the capability to demo

**Customer Context:**
- Industry
- Audience roles (Engineer, Analyst, Executive, mixed)
- Technical level (Low / Medium / High)
- Use case they want to accomplish
- Current pain points
- Current stack (if known)
- Anticipated objections

### Step 2: Select Demo Style

| Audience | Lead Style | Demo Characteristics |
|----------|------------|---------------------|
| Technical (Engineers, Architects) | **Samer** | Live SQL writing, under-the-hood explanations, contrast with alternatives |
| Mixed/Business-Technical | **Nicholas** | Step-by-step building, patient explanations, frequent check-ins |
| Business Users/Executives | **Ahmed** | Minimal code visibility, business outcome focus, "watch this" moments |
| Hands-on Practitioners | **Anish** | Build together, precise terminology, interactive Q&A |

### Step 3: Structure Each Demo Section

For EACH feature being demonstrated, use this 6-part structure:

```
1. CONTEXT BRIDGE (5-10 seconds)
   - "So you mentioned [their pain]... let me show you how we solve that"
   - "Building on what we just saw..."

2. SETUP (10-15 seconds)
   - What you're about to show
   - Why it matters to THEM
   - "This will take about 30 seconds"

3. ANCHOR (10-15 seconds)
   - "[New thing] is like [familiar thing], but [key difference]"
   - Reduce cognitive load before showing something new

4. EXECUTE & NARRATE (30-90 seconds)
   - Perform the action
   - Narrate what's happening
   - Pause on results for impact

5. BUSINESS CONNECTION (15-20 seconds)
   - "In your [industry], this means..."
   - Concrete example using their context

6. CHECKPOINT (5-10 seconds)
   - "Does that make sense?"
   - "Any questions before we move on?"
```

### Step 4: Apply Demo Patterns

**Pattern A: The "Watch This" Moment**
- Brief setup: "Watch what happens when I..."
- Execute (minimal talking)
- Pause 2-3 seconds on result
- "That just [impressive thing] in [timeframe]"

**Pattern B: The Live Build**
- "Let me build this right now, live"
- Type/click while narrating
- "Notice I didn't have to [painful thing]"
- "And there it is - took us [time]"

**Pattern C: The Before/After**
- "Here's the raw data / current state"
- Execute transformation
- "And here's what we get - [impressive result]"

**Pattern D: The "What You Don't Have To Do"**
- "Notice I'm NOT doing [painful traditional step]"
- "There's no [burden 1], no [burden 2]"
- "Snowflake handles [thing] automatically"

**Pattern E: The Security Reassurance**
- Brief demo of capability
- "Your data never leaves Snowflake"
- "The model comes to your data, not the other way around"

### Step 5: Narration Phrases

**Starting a Demo Section:**
- "Let me show you this live..."
- "Watch what happens when..."
- "I'm going to build this right now..."

**During Demo Execution:**
- "What's happening under the hood is..."
- "Notice that I didn't have to..."
- "This is effectively..."

**Highlighting Results:**
- "And there it is - [time] later..."
- "That just happened in [milliseconds/seconds]"

**Connecting to Business Value:**
- "In your case, this would..."
- "For [industry], this means..."
- "Instead of your team spending [time] on [task]..."

**Checkpoints:**
- "Does that make sense?"
- "Any questions on that?"
- "Is this what you were expecting to see?"

### Step 6: Handle Transitions

**Concept to Concept:**
- "Building on that, let me show you..."
- "Now here's where it gets interesting..."

**Demo to Questions:**
- "Before I move on, any questions?"
- "I'm going to pause here - what resonates so far?"

**Recovering from Hiccups:**
- "Interesting - let me try something else..."
- "Let me show you another way to see this..."

## Output Format

### 1. DEMO OPENING (30-60 seconds)
- Hook connecting to their situation
- What they'll see and why it matters
- Timing expectations

### 2. MAIN DEMO FLOW
For each section:
```
[DEMO SECTION: Feature Name]

CONTEXT BRIDGE:
"[spoken text]"

SETUP:
"[what you're about to show]"

ANCHOR:
"[familiar concept comparison]"

EXECUTE & NARRATE:
- Click: [action]
- Say: "[narration]"
- Wait: [for result]
- Say: "[result commentary]"

BUSINESS CONNECTION:
"[how this applies to them]"

CHECKPOINT:
"[comprehension check]"
```

### 3. WOW MOMENTS
2-3 peak moments to:
- Slow down
- Let the result speak
- Connect to business impact

### 4. ANTICIPATED QUESTIONS
3-5 likely questions with suggested responses

### 5. OBJECTION PREEMPTION
Language to address concerns (cost, complexity, security, lock-in)

### 6. DEMO CLOSE (30-60 seconds)
- Summarize what was shown
- Connect to their goals
- Bridge to next step (POC, deeper dive)

## Demo Anchors Reference

| Feature | Anchor | Key Demo Differentiator |
|---------|--------|------------------------|
| Dynamic Tables | Materialized views | Show automatic refresh, no orchestration |
| Cortex Analyst | ChatGPT | Show semantic model grounding, 96% accuracy |
| ML Functions | SQL functions | Show no Python required |
| Cortex Search | Vector search | Show RBAC applies automatically |
| Snowflake Intelligence | ChatGPT | Show multi-step agentic queries |
| Warehouses | Cloud compute | Show instant startup, auto-suspend |
| Time Travel | Undo/version history | Query at specific timestamp |
| Cloning | Git fork | Show zero-copy, instant |
| Iceberg Tables | Open format | Show external tool access |
| Notebooks | Jupyter | Native, no setup required |
| Streams & Tasks | CDC + Cron | Managed, no external scheduler |
| Container Services | Docker | Same security/governance, no infra |

## Final Checklist

- [ ] Opens with their pain point, not feature name
- [ ] Each section has anchor to familiar concept
- [ ] "What you don't have to do" mentioned at least once
- [ ] Business connection uses THEIR industry/context
- [ ] Wow moments identified and given space
- [ ] Anticipates 3+ likely questions
- [ ] Addresses known objection
- [ ] Closes with clear next step
- [ ] Sounds like natural speech
- [ ] Includes checkpoint questions
- [ ] Under 10 minutes per major section

## Example

**Input:**
- Demo: Cortex Analyst text-to-SQL
- Industry: E-commerce
- Audience: VP Analytics + 2 Senior Analysts
- Pain: Marketing always waiting on data team for queries
- Objection: Worried about AI-generated SQL accuracy

**Output:**

**DEMO OPENING:**
"So you mentioned your marketing team is constantly waiting on analysts for data pulls - simple questions that take days because of the queue. Let me show you how we solve that in about 5 minutes."

**[DEMO SECTION: Semantic Model]**

CONTEXT BRIDGE:
"Before we ask questions, let me show you what makes this accurate - because AI generating SQL sounds scary until you understand how we control it."

SETUP:
"I'm going to show you the semantic model first - this is the secret sauce. Takes 30 seconds."

ANCHOR:
"Think of this like a data dictionary, but one the AI actually uses. It's the guardrails that make this 96% accurate instead of 40-60% from a raw LLM."

EXECUTE & NARRATE:
- Click: Open semantic model
- Say: "This is just a YAML file. See these dimensions and measures? This tells the AI that 'revenue' means this specific calculation."
- Wait: Let them read 3-4 seconds
- Say: "Your team defines this once. Marketing users never see it."

BUSINESS CONNECTION:
"Your analysts would define what 'conversion rate' means, what 'attributed revenue' means - all those metrics where definitions matter. Then marketing asks and gets the RIGHT calculation every time."

CHECKPOINT:
"Does the semantic layer concept make sense before we see it in action?"

**WOW MOMENTS:**
1. First query result - Let 3-second response speak for itself
2. Showing generated SQL - Addresses accuracy concern directly
3. Complex follow-up - Shows it maintains context

**ANTICIPATED QUESTIONS:**

Q: "How accurate is this?"
A: "96% with a well-defined semantic model. Without it, raw LLMs are 40-60%. And if it can't answer, it says so rather than guessing."

Q: "Who maintains the semantic model?"
A: "Your data team - same people defining metrics in Looker today. Actually easier because it's just code, version controlled."
