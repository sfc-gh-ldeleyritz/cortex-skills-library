# Cortex Skills Library

A shared collection of Cortex Code skills for the team. Each skill lives in `skills/<skill-name>/` and contains a `SKILL.md` with full usage instructions.

## How to Use

Add this repo as a skill source in your Cortex Code configuration:

```
github:sfc-gh-ldeleyritz/cortex-skills-library
```

## Skills Catalog

### Discovery & Research

| Skill | Description |
|-------|-------------|
| **technical-discovery-research** | Creates decision-grade, citation-backed company intelligence briefs for Snowflake technical discovery using Gap Selling methodology. |
| **gong-transcript-fetcher** | Fetches Gong call transcripts by customer name via Raven search and saves them as local markdown files. |
| **transcript-extractor** | Extracts actionable intelligence from discovery calls, POC discussions, and customer meetings while preserving context and exact meaning. |
| **call-summary-generator** | Generates structured call summaries from meeting transcripts: TL;DR, decisions, action items, key points, and next steps. |

### Architecture & Sizing

| Skill | Description |
|-------|-------------|
| **architecture-pipeline** | End-to-end orchestrator: chains current-state, ingestion-patterns, future-state, and draw.io generators to go from a raw transcript to a polished architecture diagram. |
| **current-state-generator** | Generates current-state architecture diagrams (Mermaid) from customer discovery transcripts, including pain points and senior POV analysis. |
| **future-state-generator** | Generates evidence-based, maturity-aware Snowflake future-state architectures from customer discovery transcripts. |
| **drawio-architecture-generator** | Transforms future-state architecture docs into clean, enterprise-quality draw.io XML diagrams with anti-arrow-crossing layout. |
| **ingestion-pattern-recommender** | Analyzes transcripts and recommends optimal Snowflake ingestion patterns with source analysis, connector recommendations, and implementation roadmaps. |
| **consumption-estimate** | Generates comprehensive Snowflake consumption estimates including warehouse sizing, credit breakdowns, and Birdbox tables. |
| **sdr-consumption-estimate** | Quick ballpark Snowflake consumption estimates for SDR deal qualification from minimal discovery info. |
| **sizing-discovery** | Generates interactive HTML questionnaires for Snowflake sizing/pricing discovery calls. |
| **sizing-email** | Generates sizing questions emails that ask only what's missing for a consumption estimate. |

### Demo Creation

| Skill | Description |
|-------|-------------|
| **coco-demo-kit** | Bootstraps and runs the CoCo Demo Kit to build fully deployed, customer-tailored Snowflake Cortex AI demos. Orchestrates all demo sub-skills. |
| **demo-briefing** | Generates a polished HTML pre-demo briefing document synthesizing discovery notes, deployed assets, and sales context. |
| **demo-talk-track** | Creates demo talk tracks that connect every click to customer value for live Snowflake demonstrations. |

### Communication & CRM

| Skill | Description |
|-------|-------------|
| **customer-email** | Writes customer-facing emails in a warm, direct, peer-to-peer technical tone for follow-ups, intros, recaps, and next steps. |
| **sfdc-entry** | Generates Salesforce activity log entries from call transcripts or meeting notes. |
| **map-generator** | Generates a Mutual Action Plan (MAP) from discovery transcripts with objectives, use cases, week-by-week POC table, and open questions. |
| **poc-quickstart-generator** | Generates a personalized POC repo with starter SQL/Python scripts organized by Snowflake feature family. |

### Presentations & Content

| Skill | Description |
|-------|-------------|
| **snowflake-pptx** | Creates or edits Snowflake-branded PowerPoint presentations using the official January 2026 template. |
| **talk-track-generator** | Generates natural, conversational explanations for Snowflake features in a patient, step-by-step style. |
| **prompt-generator** | Generates optimized, XML-structured prompts for LLMs with built-in anti-hallucination safeguards. |

### Utilities

| Skill | Description |
|-------|-------------|
| **pipeline-hygiene** | Pipeline hygiene and maintenance utilities. |

## Contributing

1. Fork this repo
2. Add your skill under `skills/<your-skill-name>/`
3. Include a `SKILL.md` with usage instructions
4. Submit a pull request

### Skill Structure

```
skills/
  your-skill-name/
    SKILL.md          # Required: skill instructions and metadata
    ...               # Any supporting files
```
