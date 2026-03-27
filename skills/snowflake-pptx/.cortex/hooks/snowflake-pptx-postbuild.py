#!/usr/bin/env python3
"""
Snowflake PPTX post-build hook (ToolResult).
Checks that the spec YAML file was saved alongside the generated PPTX.
If the spec file is missing, injects a reminder to save it.
"""
import json
import os
import re
import sys
from pathlib import Path


def extract_pptx_path(tool_result: str) -> str | None:
    """Extract .pptx output path from tool result text."""
    patterns = [
        r'--out\s+["\']?([^"\'>\s]+\.pptx)',
        r'saved\s+(?:to\s+)?["\']?([^"\'>\s]+\.pptx)',
        r'output[:\s]+["\']?([^"\'>\s]+\.pptx)',
        r'completed[:\s]+["\']?([^"\'>\s]+\.pptx)',
        r':\s*([/\\][^\s"\']+\.pptx)',
        r'([/\\][^\s"\']+\.pptx)',
        r'([A-Za-z]:[^\s"\']+\.pptx)',
    ]
    for pattern in patterns:
        match = re.search(pattern, tool_result, re.IGNORECASE)
        if match:
            return match.group(1)
    return None


def find_spec_yaml(pptx_path: str) -> str | None:
    """Find corresponding spec YAML file for a PPTX output."""
    pptx = Path(pptx_path)
    parent = pptx.parent
    stem = pptx.stem
    
    candidates = [
        parent / f"{stem}-spec.yaml",
        parent / f"{stem}.yaml",
        parent / f"{stem}_spec.yaml",
    ]
    
    for candidate in candidates:
        if candidate.exists():
            return str(candidate)
    return None


def main():
    try:
        data = json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        sys.exit(0)

    tool_name = data.get("toolName", "")
    tool_result = data.get("toolResult", "")

    if tool_name != "Bash":
        sys.exit(0)

    if "SnowflakePptx" not in tool_result and ".pptx" not in tool_result.lower():
        sys.exit(0)

    pptx_path = extract_pptx_path(tool_result)
    if not pptx_path:
        sys.exit(0)

    if not os.path.exists(pptx_path):
        sys.exit(0)

    # After successful build: inject reminder checklist
    reminders = [
        "📸 speaker_headshots: unused photo slots show placeholder stock images — delete them before presenting.",
        "📝 Verify all content slides (content, two_column, three_column, etc.) have a subtitle field.",
    ]
    reminder_text = "\n\n**Post-build reminders:**\n" + "\n".join(f"- {r}" for r in reminders)

    spec_path = find_spec_yaml(pptx_path)
    if spec_path:
        output = {
            "hookSpecificOutput": {
                "hookEventName": "ToolResult",
                "additionalContext": reminder_text,
            }
        }
        print(json.dumps(output))
        sys.exit(0)

    stem = Path(pptx_path).stem
    additional_context = (
        f"POST-BUILD CHECK: The spec YAML file was not found alongside {pptx_path}.\n"
        f"You MUST save the spec YAML file (e.g., {stem}-spec.yaml) in the same directory "
        "as the PPTX output. This is required for reproducibility and future edits.\n"
        "Please write the spec YAML content to a file now."
    )
    additional_context += reminder_text

    output = {
        "hookSpecificOutput": {
            "hookEventName": "ToolResult",
            "additionalContext": additional_context,
        }
    }
    print(json.dumps(output))
    sys.exit(0)


if __name__ == "__main__":
    main()
