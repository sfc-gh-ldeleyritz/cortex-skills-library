---
name: coco-demo-kit
version: "1.0.0"
description: |
  **[USER-INVOCABLE]** Bootstrap and run the CoCo Demo Kit to build fully deployed, customer-tailored
  Snowflake Cortex AI demos. This skill sets up the demo framework in the current directory (if not
  already present) and delegates to the internal skills: /demo-wizard, /demo-from-transcript,
  /demo-deploy, /demo-generate, /demo-enrich, /demo-research, /demo-script.
  Use this when you want to create a quick demo for a prospect or customer. Works from a URL,
  call transcript, or interactive wizard.
  Triggers: demo kit, coco demo, demo wizard, build demo, create demo, prepare demo, demo for customer,
  demo from transcript, quick demo, set up demo, deploy demo, demo pipeline, show snowflake to customer.
allowed-tools:
  - Read
  - Write
  - Edit
  - Bash
  - Glob
  - Grep
  - snowflake_sql_execute
  - ask_user_question
  - web_fetch
  - task
  - skill
user-invocable: true
---

# CoCo Demo Kit

Build and deploy fully customized Snowflake Cortex AI demos from a single command.

## Baseline Location

The clean baseline framework lives at:
```
~/.snowflake/cortex/coco-demo-kit-baseline/
```

## Workflow

### Step 1: Check for Existing Framework

Check if the coco-demo-kit framework is already present in the current working directory:

```bash
ls .cortex/framework/config_loader.py 2>/dev/null && echo "FRAMEWORK_PRESENT" || echo "FRAMEWORK_MISSING"
```

**If FRAMEWORK_PRESENT:** Skip to Step 3.
**If FRAMEWORK_MISSING:** Proceed to Step 2.

### Step 2: Bootstrap Framework

Copy the clean baseline into the current directory:

```bash
rsync -a ~/.snowflake/cortex/coco-demo-kit-baseline/.cortex/ .cortex/
rsync -a ~/.snowflake/cortex/coco-demo-kit-baseline/requirements.txt ./
rsync -a ~/.snowflake/cortex/coco-demo-kit-baseline/AGENTS.md ./
rsync -a ~/.snowflake/cortex/coco-demo-kit-baseline/docs/ docs/
mkdir -p demos
```

Verify:
```bash
ls .cortex/framework/config_loader.py && echo "BOOTSTRAP_OK"
```

### Step 3: Detect User Intent

Determine what the user wants to do. Route to the appropriate internal skill:

| User Intent | Route |
|---|---|
| Has a customer URL, wants guided setup | **Load** `.cortex/skills/demo-wizard/SKILL.md` |
| Has a call transcript or meeting notes | **Load** `.cortex/skills/demo-from-transcript/SKILL.md` |
| Has existing config, wants to generate | **Load** `.cortex/skills/demo-generate/SKILL.md` |
| Has generated artifacts, wants to deploy | **Load** `.cortex/skills/demo-deploy/SKILL.md` |
| Wants to enrich an existing config | **Load** `.cortex/skills/demo-enrich/SKILL.md` |
| Wants a demo script/README | **Load** `.cortex/skills/demo-script/SKILL.md` |
| General "build me a demo" | **Load** `.cortex/skills/demo-wizard/SKILL.md` |

If intent is unclear, ask the user:

**Options:**
1. "I have a customer URL" -> demo-wizard
2. "I have a call transcript" -> demo-from-transcript
3. "I already have a config" -> demo-generate
4. "I need to deploy existing artifacts" -> demo-deploy

### Step 4: Execute

Follow the loaded skill's workflow. All skills use the same framework at `.cortex/framework/`.

**Key commands reference:**
- Detect Python: `PYTHON=$(bash .cortex/framework/helpers/detect_python.sh)`
- Validate config: `PYTHONPATH=.cortex $PYTHON -m framework.config_loader <config_path>`
- Generate: `PYTHONPATH=.cortex $PYTHON -m framework.generators.generate_all <config_path>`
- Deploy chain: preflight -> deploy_snowflake -> deploy_dbt -> deploy_agent -> deploy_streamlit -> postflight
- Teardown: `PYTHONPATH=.cortex $PYTHON -m framework.teardown <config_path>`

## Prerequisites

- Snowflake CLI (`snow`) installed and configured
- Python 3.9+ with `pip install pyyaml jsonschema jinja2 faker`
- Snowflake account with ACCOUNTADMIN or equivalent permissions
- Cortex features enabled in account region

## Available Industry Starters

57 industry starters at `.cortex/framework/industry_starters/`:
- Financial Services (banking, fintech, insurance, capital markets, etc.)
- Healthcare (providers, life sciences, health tech, insurance)
- Retail (ecommerce, grocery, CPG, hardlines, specialty)
- Technology (B2B, B2C, dev services, software publisher)
- Media (streaming, gaming, sports, adtech, entertainment)
- Manufacturing (automotive, energy, hardware, industrial, logistics)
- Government, Education, Telecom, Travel & Hospitality, and more

## Known Issues & Workarounds

| Issue | Workaround |
|---|---|
| Framework hardcodes warehouse `XSMALL` | Create WH before deploy: `CREATE WAREHOUSE IF NOT EXISTS XSMALL WAREHOUSE_SIZE='XSMALL'` |
| Hero records with apostrophes cause SQL errors | Escape as double apostrophe: `O''Neill` |
| `deploy_agent` smoke test fails without `snowflake-snowpark-python` | Agent still deploys correctly; ignore smoke test failure |
| dbt SYSADMIN permission issues | Grant ownership: `GRANT OWNERSHIP ON ALL TABLES IN SCHEMA ... TO ROLE SYSADMIN COPY CURRENT GRANTS` |

## Output

A fully deployed Snowflake demo environment:
- Database with RAW, STAGING, INTERMEDIATE, ANALYTICS, AGENTS schemas
- Synthetic data tailored to the customer's industry
- dbt pipeline with Cortex AI enrichment
- Semantic view for natural language queries
- Cortex Agent with Analyst + Search tools
- Branded Streamlit dashboard

## Stopping Points

- After Step 2 (bootstrap): Confirm framework is in place
- Delegated to internal skill stopping points
