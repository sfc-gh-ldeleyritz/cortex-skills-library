---
name: sfdc-entry
description: "Generate SFDC (Salesforce) activity log entries from call transcripts or meeting notes. Use when: writing SFDC entry, salesforce update, log a call, CRM entry, activity log, call notes for salesforce. Triggers: SFDC entry, salesforce entry, log call, CRM update, activity log, write up the call, SFDC update."
---

# SFDC Entry Generator

Generate Salesforce activity log entries from call transcripts or meeting notes in Louise de Leyritz's standard format.

## Format Specification

Every entry follows this exact format:

```
[DD-MM-YYYY LOUISE DE LEYRITZ] <single dense paragraph>
```

**Rules:**
- Date format: DD-MM-YYYY (European format, hyphen-separated)
- Author: LOUISE DE LEYRITZ (all caps)
- Body: ONE single paragraph, no line breaks, no bullet points, no headers
- Tone: factual, concise, third-person where referencing "we"
- Content order: who was on the call, what was discussed/presented, key findings or decisions, next steps
- No emojis
- Keep it dense but readable - typically 3-8 sentences
- Use past tense for what happened, future tense for next steps
- Reference people by first name + last name on first mention, then first name only
- End with "Next steps:" followed by concrete actions

## Workflow

### Step 1: Extract Key Information

From the transcript or meeting notes, identify:
1. **Date** of the call
2. **Participants** (names, titles, company)
3. **Who was absent** that was expected (if relevant)
4. **What was discussed** - main topics covered
5. **Key findings/decisions** - what was learned or agreed
6. **Next steps** - concrete actions with owners

### Step 2: Draft the Entry

Write the entry following the format specification above. Prioritize:
- What matters for the deal progression
- Customer pain points or requirements uncovered
- Decisions made or commitments given
- Competitive intelligence
- Commercial signals (budget, timeline, decision makers)
- Concrete next steps

**Do NOT include:**
- Small talk, technical difficulties, scheduling chatter
- Verbatim quotes (unless commercially critical)
- Internal Snowflake commentary (QBR prep, Cortex Code evangelism between Snowflake employees)
- Overly technical detail that doesn't move the deal forward

### Step 3: Present for Review

Present the draft entry to the user. If the user provides corrections, update and present again.

## Examples

**Good entry:**
```
[10-03-2026 LOUISE DE LEYRITZ] Technical discovery call with Ross Buttery to understand ICE's BI estate and plan the March 23 workshop. Darren could not attend. Ross detailed the current per-customer architecture: 2 AWS Windows VMs with SQL Server 2022 per customer, overnight batch ETL via stored procedures, and a fact/dimension warehouse with ~5,000 fields but only ~100 used in 90% of queries. Confirmed multiple customers have built their own analytics from the event feed because ICE lacks a real-time offering. The event framework already feeds into Snowflake from the September POC. Agreed the March 23 workshop will focus on building a real-time analytics pipeline from events, evaluated on three pillars: speed of development, cost comparison vs SQL Server VMs, and tangible customer-facing outcome. Next steps: Louise to propose workshop agenda via email (cc Darren), Ross to get ballpark VM costs and check with Al/Andrew on priority customer needs.
```

**Good entry (shorter call):**
```
[06-03-2026 LOUISE DE LEYRITZ] Follow-up call with Anatole Gosset (new Tech Lead) and Mathis Pasteau (new Data Engineer Lead). Organizational changes: Anatole promoted from Data Engineer, Mathis now leads data. Key opportunities identified: OpenFlow to replace Estuary Flow (~$2K/month savings), Snowflake Postgres to replace AWS RDS, and DBT Projects to replace DBT Core + Airflow. Sent OpenFlow sizing questionnaire, Postgres docs, and Cortex Code CLI guide. Next steps: follow-up session week of March 17 to review sizing and demo OpenFlow.
```

**Bad entry (too long, has bullets):**
```
[10-03-2026 LOUISE DE LEYRITZ]
- Called Ross Buttery
- He explained the architecture
- They use SQL Server
- Next steps: send agenda
```

**Bad entry (too casual, missing structure):**
```
[10-03-2026 LOUISE DE LEYRITZ] Great call with Ross today! He was really engaged and seemed excited about what we can do. Looking forward to the workshop!
```

## Stopping Points

- After Step 2: Present draft entry for user review before finalizing

## Output

A single SFDC-formatted entry ready to paste into Salesforce.
