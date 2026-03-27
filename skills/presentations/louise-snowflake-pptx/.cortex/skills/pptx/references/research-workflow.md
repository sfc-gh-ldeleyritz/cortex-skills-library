# Research Workflow

Shared research process for both pixel-perfect and creative deck generation.

## Capped Agent Search

Launch ONE research agent with a strict cap:

```
Agent prompt template:
  "Search for [topic] to support a Snowflake [purpose] deck for [audience].
   Do at most 5 web searches. Return:
   - 3-5 key statistics with source URLs
   - Recent Snowflake product announcements (2025-2026) relevant to topic
   - 2-3 customer success examples
   - Competitive differentiators (if applicable)
   Be concise. Stop after 5 searches."
```

For user-provided URLs: use `WebFetch` inline: do not delegate to the agent.

If the agent stalls or takes too long: cancel it, proceed from training knowledge,
and add "(unverified: update before presenting)" to any uncertain facts in speaker notes.

## Glean MCP (MANDATORY CHECK)

Before starting research, verify whether `mcp__glean_default__search` is available in your tool list.

- **If Glean IS available**: You MUST use it for at least 2 internal searches targeting
  field guides, battle cards, competitive intel, or technical deep-dives relevant to the
  topic. Log which Glean queries you ran in a comment before proceeding. Do NOT skip this.
- **If Glean is NOT available**: Add this note to the first content slide's speaker notes:
  "Internal sources unavailable: verify claims with field team before presenting."

Do not ask the user about Glean availability; check your own tool list.

## Source Tracking

**Track your sources**: Keep a running list of URLs and references as you research. These go into speaker notes.
