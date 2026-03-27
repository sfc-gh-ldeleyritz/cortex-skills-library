#!py -3.12
"""
Snowflake PPTX preflight hook (UserPromptSubmit).
Injects a Glean MCP reminder for PPTX-related prompts so the agent
uses internal sources during research.
"""
import json
import sys
from pathlib import Path


def config_exists() -> bool:
    """Check whether the snowflake-pptx config file exists."""
    candidates = [
        Path.home() / ".cortex" / "snowflake-pptx-config.json",
        Path(".cortex") / "snowflake-pptx-config.json",
    ]
    return any(p.exists() for p in candidates)


def is_snowflake_pptx_prompt(prompt: str) -> bool:
    text = prompt.lower()
    if "/snowflake-pptx" in text or "$snowflake-pptx" in text:
        return True
    deck_keywords = ["presentation", "deck", "slides", "pptx", "pitch"]
    return "snowflake" in text and any(k in text for k in deck_keywords)


def main():
    try:
        data = json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        sys.exit(0)

    prompt = data.get("prompt", "")

    if not is_snowflake_pptx_prompt(prompt):
        sys.exit(0)

    glean_reminder = (
        "GLEAN MCP REMINDER: Before starting research, check whether "
        "mcp__glean_default__search is available in your tool list. "
        "If it IS available, you MUST use it for at least 2 internal searches "
        "(field guides, battle cards, competitive intel). Log which Glean queries you ran. "
        "If Glean tools are NOT available, note this in the first speaker note: "
        "'Internal sources unavailable: verify claims with field team.'"
    )

    # Scan for em/en dashes that break YAML rendering
    dash_chars = []
    if "\u2014" in prompt:
        dash_chars.append("em dash (\u2014)")
    if "\u2013" in prompt:
        dash_chars.append("en dash (\u2013)")

    if config_exists():
        additional_context = glean_reminder
        if dash_chars:
            dash_warning = (
                f"\u26a0\ufe0f WARNING: Your prompt contains {', '.join(dash_chars)}. "
                "These characters break YAML rendering in the PPTX generator. "
                "Replace em dashes with ':' or ';', and en dashes with '-'. "
                "Example: 'No lock-in: run on any cloud' not 'No lock-in \u2014 run on any cloud'."
            )
            additional_context += "\n\n" + dash_warning
        output = {
            "hookSpecificOutput": {
                "hookEventName": "UserPromptSubmit",
                "additionalContext": additional_context,
            }
        }
        print(json.dumps(output))
        sys.exit(0)

    # Config missing: redirect Claude to run setup first
    additional_context = (
        "PREFLIGHT CHECK FAILED: ~/.cortex/snowflake-pptx.yaml is missing.\n"
        "Before generating any Snowflake presentation, you MUST run the setup flow first.\n"
        "Immediately run the /snowflake-pptx setup flow to collect presenter details "
        "and write the config file.\n"
        "Once setup is complete, automatically continue with the user's original request: "
        + repr(prompt)
    )
    additional_context += "\n\n" + glean_reminder

    if dash_chars:
        dash_warning = (
            f"\u26a0\ufe0f WARNING: Your prompt contains {', '.join(dash_chars)}. "
            "These characters break YAML rendering in the PPTX generator. "
            "Replace em dashes with ':' or ';', and en dashes with '-'. "
            "Example: 'No lock-in: run on any cloud' not 'No lock-in \u2014 run on any cloud'."
        )
        additional_context += "\n\n" + dash_warning

    output = {
        "hookSpecificOutput": {
            "hookEventName": "UserPromptSubmit",
            "additionalContext": additional_context,
        }
    }
    print(json.dumps(output))
    sys.exit(0)


if __name__ == "__main__":
    main()
