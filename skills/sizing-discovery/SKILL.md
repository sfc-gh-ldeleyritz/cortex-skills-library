---
name: sizing-discovery
description: "Generate sizing discovery questionnaires for Snowflake consumption estimates. Use when: preparing for customer sizing calls, building discovery questionnaires, estimating Snowflake costs. Triggers: sizing call, sizing questions, discovery questionnaire, consumption estimate prep, sizing discovery, prepare for sizing, customer sizing."
---

# Sizing Discovery Questionnaire Generator

## Purpose

Generate interactive HTML questionnaires for Snowflake sizing/pricing discovery calls. Produces professional, editable questionnaires tailored to specific Snowflake features.

## Prerequisites

- Reference document: Load `sizing_discovery_reference.md` from the current project or ask user for location

## Workflow

### Step 1: Identify Relevant Features

**Ask user:**
```
Which Snowflake features need sizing?

1. Compute - Warehouses
2. Ingestion - Openflow (CDC/ETL)
3. Ingestion - Snowpipe
4. OLTP - Snowflake Postgres
5. Serverless - Dynamic Tables
6. Serverless - Tasks
7. Containers - SPCS
8. AI - Cortex AI Functions
9. AI - Cortex Search
10. AI - Cortex Analyst
11. AI - Cortex Agents
12. Transformation - dbt Projects
13. Storage
14. Data Sharing
15. Iceberg Tables

Select all that apply (e.g., "2, 4, 12"):
```

**⚠️ STOP**: Wait for feature selection.

### Step 2: Gather Context (Optional)

**Ask user:**
```
Any known context about this customer? (optional)
- Current stack (databases, ETL tools, cloud)
- Current costs
- Database sizes
- Region

Or "skip" to proceed without context.
```

Pre-fill known values in the questionnaire if provided.

### Step 3: Generate HTML Questionnaire

**For each selected feature:**
1. Load questions from `sizing_discovery_reference.md`
2. Include pricing model summary
3. Include gotchas section
4. Add input fields for each discovery question

**HTML Structure:**
```html
<!DOCTYPE html>
<html>
<head>
  <title>Sizing Discovery - [Customer Name]</title>
  <style>
    /* Snowflake branding: #29B5E8 accent, clean sans-serif */
    body { font-family: -apple-system, sans-serif; max-width: 900px; margin: 0 auto; padding: 20px; }
    .section { background: #f8f9fa; border-radius: 8px; padding: 20px; margin: 20px 0; border-left: 4px solid #29B5E8; }
    .section h2 { color: #29B5E8; margin-top: 0; }
    .question { margin: 15px 0; }
    .question label { display: block; font-weight: 600; margin-bottom: 5px; }
    .question input, .question select, .question textarea { width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px; }
    .gotcha { background: #fff3cd; padding: 10px; border-radius: 4px; margin: 10px 0; font-size: 14px; }
    .gotcha::before { content: "⚠️ "; }
    .pricing { background: #e8f4f8; padding: 15px; border-radius: 4px; margin: 10px 0; }
    .pricing table { width: 100%; border-collapse: collapse; }
    .pricing td, .pricing th { padding: 5px 10px; text-align: left; border-bottom: 1px solid #ddd; }
    .context-box { background: #e8f4f8; padding: 15px; border-radius: 8px; margin-bottom: 20px; }
    .resources { background: #f0f0f0; padding: 15px; border-radius: 8px; margin-top: 30px; }
    .resources a { color: #29B5E8; }
    @media print { .no-print { display: none; } }
  </style>
</head>
<body>
  <h1>🎯 Sizing Discovery: [Customer Name]</h1>
  <p>Date: [Date] | SE: [Your Name]</p>
  
  <!-- Context box if known info provided -->
  <div class="context-box">
    <strong>Known Context:</strong>
    <ul>
      <li>Current stack: [pre-filled]</li>
      <li>Region: [pre-filled]</li>
    </ul>
  </div>
  
  <!-- Repeat for each feature section -->
  <div class="section">
    <h2>[Feature Name]</h2>
    
    <div class="pricing">
      <strong>Pricing Model:</strong>
      <!-- Pricing table from reference -->
    </div>
    
    <div class="question">
      <label>[Question from reference]</label>
      <input type="text" placeholder="[hint]" value="[pre-filled if known]">
    </div>
    <!-- More questions... -->
    
    <div class="gotcha">[Gotcha from reference]</div>
  </div>
  
  <!-- Resources section -->
  <div class="resources">
    <strong>Internal Resources:</strong>
    <ul>
      <li><a href="#">[Relevant calculators/docs]</a></li>
      <li>Slack: #channel-name</li>
    </ul>
  </div>
  
  <!-- Next steps checklist -->
  <div class="section">
    <h2>Next Steps</h2>
    <label><input type="checkbox"> Send consumption estimate</label>
    <label><input type="checkbox"> Schedule follow-up</label>
    <label><input type="checkbox"> Share with SA team</label>
  </div>
</body>
</html>
```

### Step 4: Save and Present

1. Save HTML to user-specified location (default: same directory as conversation context)
2. Provide path to file
3. Remind user they can open in browser for interactive use

**Output:**
```
✅ Generated: [path]/sizing-discovery-[customer].html

Open in browser to:
- Fill in answers during the call
- Print or save as PDF
- All inputs are editable
```

## Question Selection Guidelines

**For each feature, include:**
1. **Required Inputs** - Questions that directly feed pricing calculations
2. **Compatibility** - Version/feature compatibility checks (Postgres extensions, dbt packages)
3. **Current State** - Baseline for comparison (current costs, instance types)
4. **Architecture** - Decisions that affect pricing model (HA, multi-cluster, BYOC)

**Skip:**
- Questions already answered by known context
- Deep technical questions not relevant to sizing

## Feature-Specific Notes

### Openflow
- Always ask: SPCS or BYOC deployment preference
- Critical gotcha: pricing is per SERVER not per database

### Snowflake Postgres
- Always ask: HA requirement (doubles cost)
- Get current RDS/Aurora instance type for mapping

### dbt Projects
- Always ask: current orchestration (Airflow cost = savings opportunity)
- No license fee - only compute

### Dynamic Tables
- Always ask: chaining depth (cost explosion risk)
- Target lag × DT count = key driver

### Cortex AI
- Always ask: model preference (10× cost difference)
- Output tokens often more expensive than input

## Stopping Points

- ✋ After Step 1: Feature selection
- ✋ After Step 4: Review generated questionnaire

## Output

Interactive HTML questionnaire file with:
- Editable input fields for all sizing questions
- Pricing model summaries per feature
- Gotchas/warnings highlighted
- Internal resource links
- Next steps checklist
