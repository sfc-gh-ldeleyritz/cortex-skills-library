---
name: call-summary-generator
description: "Generate structured call summaries from meeting transcripts. Produces TL;DR, decisions, action items, key points, and next steps. Use when: summarizing a call, writing up a meeting, call recap, meeting notes, call debrief. Triggers: call summary, summarize call, meeting summary, call recap, meeting notes, write up the call, debrief call, summarize meeting, call write-up, meeting recap."
---

# Universal Call Summary Generator

## When to Load

User asks to:

* "Summarize this call"
* "Write up meeting notes"
* "Give me a call recap"
* "Summarize this meeting"
* "Call debrief"
* "Meeting summary from this transcript"

**Important:** If the user asks to **extract** or **distill** a transcript (preserving conversation flow, exact quotes, tagged information types), route to the `transcript-extractor` skill instead. This skill produces a **concise structured summary**, not a detailed extraction.

## Workflow

### Step 0: Receive Transcript

**Goal:** Get the transcript from the user.

**Actions:**

1. If the user has not provided a transcript, ask:
   > "Please paste the call transcript and I'll generate a structured summary."
2. If optional context is available, note:
   - Call name/topic
   - Date
   - Participant names/roles

### Step 1: Analyze and Extract

**Goal:** Read through the entire transcript and extract the six sections.

**Actions:**

Work through the transcript and extract:

1. **TL;DR** — 2-3 sentences capturing the essence of the call
2. **Decisions made** — What was agreed and why (include brief rationale if discussed)
3. **Action items** — Tasks with owners and deadlines if mentioned
4. **Key points** — Main topics grouped by theme, with context (this is the meat — be detailed here)
5. **Open questions** — Unresolved items that need follow-up
6. **Next steps** — Follow-ups scheduled with dates if known

### Step 2: Format Output

**Goal:** Produce the final summary in the exact format below.

**Output Format:**

```markdown
# Call Summary: [Call Name/Topic]
**Date:** [Date] | **Participants:** [Names]

## TL;DR
[2-3 sentences]

## Decisions
- [Decision 1 + brief rationale if discussed]
- [Decision 2]

## Action Items
- [ ] [Task] → [Owner] (deadline if mentioned)
- [ ] [Task] → [Owner]

## Key Points
[Group by topic. Each topic gets a bold header and 2-4 detailed bullets underneath. Include context, numbers, pain points, and implications.]

**[Topic 1]**
- [Detailed point with context]
- [Supporting detail or implication]

**[Topic 2]**
- [Detailed point with context]
- [Supporting detail or implication]

## Open Questions
- [Question 1]

## Next Steps
- [Next step with date if known]
```

### Step 3: Verify

**Goal:** Quality-check the summary before delivering.

**Verification checklist:**

- [ ] Bullets only, no paragraphs
- [ ] Key Points is the longest section — this is where context lives
- [ ] All specific numbers, timelines, and names mentioned in the call are captured
- [ ] No "Quotes" section (not part of this format)
- [ ] No priority ratings (not part of this format)
- [ ] No customer signals analysis (not part of this format)
- [ ] Any section with no content is skipped entirely (not left empty)
- [ ] Someone who missed the call would understand not just WHAT was discussed but WHY it matters

## Constraints

- Bullets only, no paragraphs
- Key Points section should be the longest — this is where context lives
- Include specific numbers, timelines, and names when mentioned
- No quotes section
- No priority ratings
- No customer signals analysis
- Skip any section that has no content
- Do NOT add interpretation or analysis — capture what was discussed
