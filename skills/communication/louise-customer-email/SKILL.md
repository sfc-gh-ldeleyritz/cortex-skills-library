---
name: customer-email
description: "Write customer-facing emails in Louise's tone: warm, direct, peer-to-peer technical. Use for: follow-up emails, intro emails, recap emails, next steps emails, thank you emails, any prospect or customer email. Triggers: write an email, draft email, customer email, follow-up email, send email, email to customer, email to prospect, write to, email mark, email stephen."
---

# Customer Email Generator

Write customer-facing emails in Louise de Leyritz's standard tone and style.

## Tone Specification

The tone is **warm, direct, peer-to-peer technical**. The email reads like a knowledgeable colleague writing to someone they respect, not a vendor running a process or an AI generating content.

**Voice rules:**
- Conversational but professional. Contractions are fine, first names are fine after first mention.
- Direct and to the point. Say what you need to say, then stop.
- Confident without being salesy. State facts, don't oversell.
- Assume intelligence in the reader. Don't over-explain things they already know.
- Warm without being sycophantic. A brief genuine reference to the last interaction is good. "It was great to meet you" is fine. "I truly appreciated the wonderful opportunity to connect with you" is not.

**Formatting rules:**
- Never use "-" (dashes/hyphens) as bullet points or list markers. Use numbered lists where structure is needed.
- No bullet-point walls. Prefer short paragraphs with natural flow.
- Bold section headers are fine when the email has distinct sections (e.g. a recap email with multiple topics), but don't force structure onto a short email.
- If attaching or linking things, weave them naturally into sentences rather than listing them at the bottom.
- No emojis.

**Language rules:**
- No corporate fluff: "I hope this email finds you well", "as per our conversation", "please don't hesitate to reach out", "I wanted to circle back", "just looping in", "per our discussion"
- No filler openings. Start with something real: reference the call, the topic, or what you're sending.
- No padded closings. End with a concrete next step or a simple offer to chat, then sign off.
- Use "we" for Snowflake, first names for people, company name for the customer's org.
- Past tense for what happened, future tense for next steps.

**Sign-off:** Always sign as Louise. Keep it simple: "Best, Louise" or "Thanks, Louise" or "Speak soon, Louise" depending on context.

**What NOT to do:**
- Do not pad the email to seem longer or more thorough
- Do not repeat information the recipient already knows just to show you were paying attention
- Do not use AI-telltale phrases: "I'd be happy to", "feel free to", "please let me know if you have any questions", "I look forward to hearing from you"
- Do not write more than is needed. A three-sentence email is perfectly fine if that's all the situation calls for.

## Workflow

### Step 1: Understand the Ask

**Goal:** Determine what kind of email is needed and gather context.

**Actions:**

1. Identify the email type: follow-up, recap, introduction, sharing resources, asking questions, scheduling, thank you, etc.
2. Identify the recipient(s) and their role/context
3. Check memory and conversation history for any prior context about this customer
4. Identify what specific content the user wants in the email

### Step 2: Draft the Email

**Goal:** Write the email following the tone specification.

**Actions:**

1. Open with something specific and real (reference a call, a topic, or the reason for writing)
2. Deliver the core content concisely
3. Close with a clear next step or offer
4. Sign off as Louise

**Length guide:**
- Simple follow-up or share: 3 to 6 sentences
- Recap or multi-topic: short paragraphs with bold headers if needed
- Questions email: numbered questions with inline context, grouped under headers

### Step 3: Present for Review

Present the draft. If the user provides corrections, update and present again.

## Examples

**Good: Short follow-up**
```
Hi Mark,

Great chatting earlier. As promised, here's the link to the Snowflake pricing overview we discussed: [link]. I've also attached the zero-copy data sharing doc that covers the Salesforce Data Cloud integration you asked about.

Let me know if any questions come up once you've had a look, happy to jump on a quick call.

Best,
Louise
```

**Good: Recap email**
```
Hi Stephen,

Quick recap from our session this morning so you have it in writing.

**Architecture** We walked through the current state diagram and confirmed the Postgres/Heroku setup is the priority source. You mentioned the Stripe scheme fee reports are a secondary priority once the core pipeline is running.

**Next steps** I'll send over a cost estimate by end of next week. On your side, if you could check whether the Postgres instance has logical replication enabled (wal_level = logical), that will save us time when we get to ingestion setup.

Speak soon,
Louise
```

**Bad: Corporate fluff**
```
Hi Mark,

I hope this email finds you well. I wanted to take a moment to thank you for your time on yesterday's call. It was truly a pleasure to connect with you and learn more about TeamFeePay's exciting journey.

As per our discussion, I am attaching some resources that I believe you will find valuable. Please don't hesitate to reach out if you have any questions or concerns.

I look forward to hearing from you soon.

Best regards,
Louise de Leyritz
Solution Engineer, Snowflake
```

**Bad: Dash-heavy AI email**
```
Hi Stephen,

Following up on our call. Here's a summary:

- We discussed the architecture
- You mentioned Stripe volumes
- Next steps include a cost estimate
- I'll also send the data sharing doc

Let me know if you need anything else!
```

## Stopping Points

- After Step 2: Present draft email for user review

## Output

The email text, ready to copy-paste.
