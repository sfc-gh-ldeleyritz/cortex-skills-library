---
name: gong-transcript-fetcher
description: "Fetch Gong call transcripts for a customer and save them locally. Given a customer/account name (and optionally a date), searches Raven for the matching Gong meeting, retrieves the full transcript, and saves it as a markdown file in a local transcripts/ folder. Triggers: gong transcript, get transcript, fetch transcript, pull transcript, download transcript, save transcript, transcript for [customer]."
---

# Gong Transcript Fetcher

Fetch Gong call transcripts by customer name and save them locally.

## Trigger Phrases

- "gong transcript for [customer]"
- "get the transcript for [customer]"
- "fetch transcript [customer]"
- "pull transcript for [customer]"
- "download transcript [customer]"
- "save transcript for [customer]"
- "transcript for [customer]"

## Workflow

### Step 1: Parse the Request

Extract from the user's message:
- **Customer name** (required) — the account/company name
- **Date** (optional) — specific meeting date; if not provided, default to the most recent meeting
- **Meeting title keywords** (optional) — e.g. "discovery", "technical deep dive"

### Step 2: Fetch the Transcript via Raven (Single Call)

CRITICAL: Use ONE `mcp_raven_raven_gtm_assistant` call with this exact prompt to minimise latency. The prompt is designed to make Raven return the raw transcript with zero analysis/summarisation:

```
For account "[CUSTOMER_NAME]", query the Gong meeting [on DATE / most recent if no date]. 
Return ONLY: 
1. Meeting title 
2. Meeting date 
3. Participant names and companies 
4. The COMPLETE RAW_CONTENT transcript verbatim — every line, every speaker ID. 
Do NOT summarise, analyse, or omit anything. Just dump the raw text.
```

Do NOT make a second Raven call. One call is enough.

### Step 3: Save the Transcript

1. Create `transcripts/` in the current working directory if it doesn't exist: `mkdir -p transcripts`

2. Extract the transcript from Raven's response. The response is cached as a large text file. Use Python to:
   - Parse the JSON response (skip the metadata header line)
   - Extract the `text` content blocks
   - Pull out the transcript lines (speaker ID + colon + text format)

3. Save as markdown:

**Filename:** `transcripts/{YYYY-MM-DD}_{customer_name_snake_case}_{short_title_snake_case}.md`

**File format:**

```markdown
# {Meeting Title}

**Date:** {YYYY-MM-DD}
**Participants:** {comma-separated list of names and roles/companies}
**Source:** Gong (via Raven)

---

## Transcript

{Full transcript text — keep speaker IDs as-is since we cannot reliably map them all}
```

Use the `write` tool to save the file directly. Do NOT use Python file I/O.

### Step 4: Confirm to User

Tell the user:
- The file path where the transcript was saved
- Meeting title, date, and participants
- Offer to run the `transcript-extractor` or `call-summary-generator` skill on it

## Speed Notes

- The user does NOT have direct SQL access to `IT.RAW_GONG` or `SALES.REPORTING.GONG_ALL_CONV` (restricted roles). Raven is the only path.
- If SQL access is ever granted, replace Step 2 with a direct `snowflake_sql_execute` query against the Gong table — this would be instant vs the current Raven LLM overhead.

## Error Handling

- **No meetings found:** Suggest checking customer name spelling or broadening date range.
- **Multiple meetings found:** List all (title, date, participants) and ask user to pick one.
- **Transcript not available:** Inform user, suggest checking Gong UI directly.

## Constraints

- Always save to `transcripts/` in the current working directory
- Never modify or interpret the transcript content — save it as-is
- Use snake_case for filenames, lowercase
- Include the date prefix for easy sorting
- ONE Raven call only — do not make multiple calls
