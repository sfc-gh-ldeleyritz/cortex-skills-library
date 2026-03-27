# Cortex Skills Library

A shared collection of Cortex Code skills for the team. Skills are organized by **category** and prefixed with the **author's name** to avoid collisions and make ownership clear.

## How to Use

Add this repo as a skill source in your Cortex Code configuration:

```
github:sfc-gh-ldeleyritz/cortex-skills-library
```

## Repo Structure

```
skills/
  <category>/
    <author>-<skill-name>/
      SKILL.md              # Required: skill instructions and metadata
      ...                   # Supporting files (references, templates, etc.)
```

### Categories

| Category | Purpose |
|----------|---------|
| `discovery/` | Customer research, call transcripts, meeting intelligence |
| `architecture/` | Current/future state diagrams, ingestion patterns, architecture pipelines |
| `sizing/` | Consumption estimates, pricing, sizing questionnaires |
| `demos/` | Demo creation, deployment, briefings, talk tracks |
| `communication/` | Customer emails, SFDC entries, mutual action plans, POC repos |
| `presentations/` | PowerPoint generation, feature talk tracks |
| `utilities/` | Prompt engineering, pipeline maintenance, general-purpose tools |

## Skills Catalog

### Discovery

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-technical-discovery-research` | Louise | Decision-grade, citation-backed company intelligence briefs using Gap Selling methodology |
| `louise-transcript-extractor` | Louise | Extracts actionable intelligence from discovery calls and customer meetings |
| `louise-call-summary-generator` | Louise | Structured call summaries: TL;DR, decisions, action items, key points, next steps |

### Architecture

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-architecture-pipeline` | Louise | End-to-end orchestrator: transcript to polished draw.io architecture diagram |
| `louise-current-state-generator` | Louise | Current-state architecture diagrams (Mermaid) with pain points and senior POV analysis |
| `louise-future-state-generator` | Louise | Evidence-based, maturity-aware Snowflake future-state architectures |
| `louise-drawio-architecture-generator` | Louise | Transforms architecture docs into enterprise-quality draw.io XML diagrams |
| `louise-ingestion-pattern-recommender` | Louise | Recommends optimal Snowflake ingestion patterns with connector recommendations |

### Sizing

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-consumption-estimate` | Louise | Comprehensive Snowflake consumption estimates with warehouse sizing and Birdbox tables |
| `louise-sdr-consumption-estimate` | Louise | Quick ballpark estimates for SDR deal qualification from minimal discovery info |
| `louise-sizing-discovery` | Louise | Interactive HTML questionnaires for sizing/pricing discovery calls |
| `louise-sizing-email` | Louise | Sizing questions emails that ask only what's missing for a consumption estimate |

### Demos

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-coco-demo-kit` | Louise | Bootstraps and deploys customer-tailored Snowflake Cortex AI demos end-to-end |
| `louise-demo-briefing` | Louise | Polished HTML pre-demo briefing synthesizing discovery notes and sales context |
| `louise-demo-talk-track` | Louise | Demo talk tracks connecting every click to customer value |

### Communication

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-customer-email` | Louise | Customer-facing emails in a warm, direct, peer-to-peer technical tone |
| `louise-sfdc-entry` | Louise | Salesforce activity log entries from call transcripts or meeting notes |
| `louise-map-generator` | Louise | Mutual Action Plans with objectives, use cases, and week-by-week POC tables |
| `louise-poc-quickstart-generator` | Louise | Personalized POC repos with starter SQL/Python scripts by Snowflake feature family |

### Presentations

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-snowflake-pptx` | Louise | Creates/edits Snowflake-branded PowerPoint presentations (January 2026 template) |
| `louise-talk-track-generator` | Louise | Natural, conversational explanations for Snowflake features |

### Utilities

| Skill | Author | Description |
|-------|--------|-------------|
| `louise-prompt-generator` | Louise | Optimized, XML-structured prompts for LLMs with anti-hallucination safeguards |
| `louise-pipeline-hygiene` | Louise | Pipeline hygiene and maintenance utilities |

## Contributing

### Adding a New Skill

1. Fork this repo
2. Pick the right **category** folder (or create a new one if none fits)
3. Create your skill folder: `skills/<category>/<your-name>-<skill-name>/`
4. Add a `SKILL.md` with usage instructions
5. Submit a pull request

### Naming Convention

```
skills/<category>/<author-firstname>-<skill-name>/
```

Examples:
- `skills/discovery/alex-competitor-analysis/`
- `skills/sizing/mark-warehouse-calculator/`
- `skills/demos/sarah-industry-demo-kit/`

### Adding a New Category

If your skill doesn't fit existing categories, create a new folder under `skills/` and update this README with the category description.

### Skill Requirements

Every skill folder **must** contain:
- `SKILL.md` — Instructions, triggers, and usage documentation
- Any supporting files (references, templates, pricing data, etc.)
