# Config File Schema

**REQUIRED — the skill cannot proceed without this file.**

Read `.cortex/snowflake-pptx-config.json` using the `Read` tool as your very first action.

- If the file **does not exist**, or if either `presenter_name` or `presenter_title` is missing, stop immediately and display:

  > "No config file found (or missing required fields) at `.cortex/snowflake-pptx-config.json`. Please create it before generating a deck:"
  > ```json
  > {
  >   "presenter_name": "Jane Doe",
  >   "presenter_title": "Sr. Solutions Engineer",
  >   "presenter_headshot": "~/Pictures/headshot.jpg",
  >   "company": "Snowflake",
  >   "output_dir": "~/Documents/Decks",
  >   "deck_length": "medium",
  >   "safe_harbor": true
  > }
  > ```
  > Required fields: `presenter_name`, `presenter_title`. All others are optional.

  Do not continue until the user confirms the file exists with both required fields.

- If the file exists and has both required fields, use its values as defaults for all subsequent steps. Skip any `AskUserQuestion` prompts that are already answered by the config.

## Field Reference

| Field | Required | Description |
|-------|----------|-------------|
| `presenter_name` | Yes | Full name of the presenter |
| `presenter_title` | Yes | Job title of the presenter |
| `presenter_headshot` | No | Path to headshot image (supports `~`) |
| `company` | No | Company name; used as subtitle prefix on cover slide |
| `output_dir` | No | Output directory (supports `~`); defaults to current directory |
| `deck_length` | No | `"short"`, `"medium"`, or `"long"`; defaults to `"medium"` |
| `safe_harbor` | No | `true`/`false`; defaults to `true` (ignored by creative skill) |

All path fields support `~`: expand them when building the YAML spec.
