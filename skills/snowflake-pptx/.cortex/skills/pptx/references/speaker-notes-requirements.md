# Speaker Notes Requirements

**Non-negotiable minimum**: Every content slide (`content`, `two_column_titled`, `three_column_titled`, `three_column_icons`, `four_column_numbers`, `four_column_icons`, `split`) MUST have speaker notes of at least 500 characters. Target 1500-2500 chars per content slide. This is a hard requirement: do not skip it.

Every content slide's speaker notes must include:
- Verbose talking points and delivery guidance (1500-2500 chars per slide)
- Source URLs for claims, statistics, and data points (full `https://` URLs when known; domain only when the exact path is uncertain, e.g. `snowflake.com`)
- Transition cues to the next slide
- A `References:` block at the end with at least one entry

When a full URL is unknown, use the domain only: `- [Description]: site-name.com` — do not guess full paths.

Structural slides (title, safe harbor, thank you, section dividers) don't need detailed references.

Format references at the end of notes:
```
References:
- [Description]: https://example.com/source
- [Description]: site-name.com
```

> **Checklist before writing the YAML**: Verify every content slide has notes with talking points AND a `References:` section. If any content slide is missing either, add them before proceeding.
