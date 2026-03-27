#!/usr/bin/env python3
"""
PostToolUse hook: blocks Write/Edit tool calls that introduce em-dash (U+2014)
or en-dash (U+2013) characters into YAML spec files.

Triggered after Write or Edit tool use. Inspects the content being written/edited.
If dashes are found in a .yaml file, emits a blocking decision with a clear message.
"""
import json
import sys


EM_DASH = "\u2014"
EN_DASH = "\u2013"


def get_yaml_content(data: dict) -> tuple:
    """
    Extract (file_path, content_to_check) from the tool input.
    Returns (None, None) if not a YAML file write/edit.
    """
    tool_name = data.get("tool_name", "")
    tool_input = data.get("tool_input", {})

    if tool_name == "Write":
        path = tool_input.get("file_path", "")
        content = tool_input.get("content", "")
        return path, content

    if tool_name == "Edit":
        path = tool_input.get("file_path", "")
        new_string = tool_input.get("new_string", "")
        return path, new_string

    if tool_name == "MultiEdit":
        path = tool_input.get("file_path", "")
        edits = tool_input.get("edits", [])
        combined = "\n".join(e.get("new_string", "") for e in edits)
        return path, combined

    return None, None


def is_yaml_spec_file(path: str) -> bool:
    if not path:
        return False
    p = path.lower()
    return p.endswith(".yaml") or p.endswith(".yml")


def find_dash_lines(content: str) -> list:
    issues = []
    for i, line in enumerate(content.splitlines(), 1):
        if EM_DASH in line or EN_DASH in line:
            marker = []
            if EM_DASH in line:
                marker.append("em-dash \u2014")
            if EN_DASH in line:
                marker.append("en-dash \u2013")
            issues.append(f"  Line {i} ({', '.join(marker)}): {line.strip()[:120]}")
    return issues


def main():
    try:
        data = json.load(sys.stdin)
    except (json.JSONDecodeError, ValueError):
        sys.exit(0)

    path, content = get_yaml_content(data)

    if path is None or content is None:
        sys.exit(0)

    if not is_yaml_spec_file(path):
        sys.exit(0)

    issues = find_dash_lines(content)
    if not issues:
        sys.exit(0)

    message = (
        "BLOCKED: em-dash or en-dash characters found in YAML spec.\n"
        "The PPTX builder and Snowflake brand guidelines prohibit these characters.\n\n"
        "Replace every em-dash (\u2014) with a colon (:) or semicolon (;).\n"
        "Replace every en-dash (\u2013) with a hyphen (-).\n\n"
        "Offending lines:\n" + "\n".join(issues) + "\n\n"
        "Fix all instances above, then re-issue the Write/Edit tool call."
    )

    output = {
        "decision": "block",
        "reason": message,
    }
    print(json.dumps(output))
    sys.exit(0)


if __name__ == "__main__":
    main()
