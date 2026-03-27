#!/usr/bin/env python3
"""
PostToolUse hook: blocks Write/Edit tool calls that use removed or invalid
slide types in YAML presentation spec files.

Triggered after Write or Edit tool use. Parses `type:` values from the YAML
content and rejects any that are not in the 22-slide template.
"""
import json
import re
import sys


# The 22 valid slide types in the January 2026 Snowflake template.
VALID_TYPES = frozenset({
    "title", "title_headshot", "agenda", "safe_harbor", "section", "title_wave",
    "title_customer_logo", "chapter_particle", "content",
    "two_column_titled", "three_column_titled", "three_column_icons",
    "four_column_numbers", "four_column_icons", "split",
    "quote", "quote_photo", "quote_simple",
    "table_styled", "table_striped", "speaker_headshots", "thank_you",
})

# Removed types that used to exist — give targeted advice.
REMOVED_TYPES = {
    "two_column": "Use 'two_column_titled' instead (or prefer 'three_column_icons' for visual impact).",
    "three_column": "Use 'three_column_titled' or 'three_column_icons' instead.",
    "title_particle": "This slide type was removed. Use 'title_wave' or 'chapter_particle' instead.",
    "comparison": "Use 'two_column_titled' or 'three_column_titled' instead.",
}

# Pattern: matches `type: <value>` in YAML (handles optional quotes).
TYPE_RE = re.compile(
    r"""^\s*-?\s*type\s*:\s*['"]?([a-z_]+)['"]?\s*$""",
    re.MULTILINE | re.IGNORECASE,
)


def get_yaml_content(data: dict) -> tuple:
    """Extract (file_path, content_to_check) from the tool input."""
    tool_name = data.get("tool_name", "")
    tool_input = data.get("tool_input", {})

    if tool_name == "Write":
        return tool_input.get("file_path", ""), tool_input.get("content", "")

    if tool_name == "Edit":
        return tool_input.get("file_path", ""), tool_input.get("new_string", "")

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


# Divider types that should use ALL CAPS titles and subtitles.
DIVIDER_TYPES = frozenset({"chapter_particle", "section", "section_dots"})

# Pattern: matches `title: <value>` or `subtitle: <value>` in YAML.
TITLE_RE = re.compile(
    r"""^\s*(?:title|subtitle)\s*:\s*['"](.+?)['"]""",
    re.MULTILINE,
)


def find_invalid_types(content: str) -> list[tuple[int, str, str]]:
    """Return list of (line_number, type_value, advice) for invalid types."""
    issues = []
    for match in TYPE_RE.finditer(content):
        type_val = match.group(1).lower()
        if type_val in VALID_TYPES:
            continue

        # Calculate line number.
        line_num = content[:match.start()].count("\n") + 1

        if type_val in REMOVED_TYPES:
            advice = REMOVED_TYPES[type_val]
        else:
            advice = f"Unknown slide type. Valid types: {', '.join(sorted(VALID_TYPES))}"

        issues.append((line_num, type_val, advice))

    return issues


def check_divider_capitalization(content: str) -> list[str]:
    """Warn if divider slides (chapter_particle, section) have non-uppercase titles."""
    warnings = []
    # Split content into slide blocks by looking for `- type:` markers.
    slide_blocks = re.split(r"(?=^\s*-\s*type\s*:)", content, flags=re.MULTILINE)
    for block in slide_blocks:
        type_match = TYPE_RE.search(block)
        if not type_match:
            continue
        slide_type = type_match.group(1).lower()
        if slide_type not in DIVIDER_TYPES:
            continue
        # Check title and subtitle fields within this block.
        for field_match in TITLE_RE.finditer(block):
            val = field_match.group(1)
            if val != val.upper():
                field_name = "title" if field_match.group(0).strip().startswith("title") else "subtitle"
                warnings.append(
                    f"  {slide_type} {field_name} should be ALL CAPS: \"{val}\" -> \"{val.upper()}\""
                )
    return warnings


def check_slide_variety(content: str) -> list[str]:
    """Warn if 'content' type exceeds 30% of slides or >2 consecutive."""
    warnings = []
    types_found = [m.group(1).lower() for m in TYPE_RE.finditer(content)]
    if len(types_found) < 6:
        return warnings  # Too few slides to worry about variety.
    content_count = sum(1 for t in types_found if t == "content")
    pct = content_count / len(types_found) * 100
    if pct > 30:
        warnings.append(
            f"  Slide variety: 'content' used {content_count}/{len(types_found)} "
            f"({pct:.0f}%) - exceeds 30% guideline. Mix in column, split, quote, or table types."
        )
    # Check consecutive content slides.
    consecutive = 0
    for i, t in enumerate(types_found):
        if t == "content":
            consecutive += 1
            if consecutive > 2:
                warnings.append(
                    f"  Slide {i + 1}: {consecutive} consecutive 'content' slides. "
                    "Insert a visual break (column layout, quote, split, etc.)."
                )
        else:
            consecutive = 0
    return warnings


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

    # Quick check: does this look like a presentation spec?
    if "type:" not in content or "slides" not in content.lower():
        sys.exit(0)

    issues = find_invalid_types(content)
    if not issues:
        # No blocking issues — check for non-blocking warnings.
        all_warnings = check_divider_capitalization(content) + check_slide_variety(content)
        if all_warnings:
            output = {
                "decision": "approve",
                "additionalContext": (
                    "YAML spec warnings (non-blocking):\n"
                    + "\n".join(all_warnings)
                    + "\n\nThese are recommendations, not errors. Fix if possible before building."
                ),
            }
            print(json.dumps(output))
        sys.exit(0)

    lines = []
    for line_num, type_val, advice in issues:
        lines.append(f"  Line {line_num}: type: {type_val} -- {advice}")

    message = (
        "BLOCKED: Invalid slide type(s) found in YAML spec.\n"
        "The Snowflake template has 22 valid slide types. "
        "The following types are not valid:\n\n"
        + "\n".join(lines)
        + "\n\nFix all invalid types, then re-issue the Write/Edit tool call."
    )

    output = {
        "decision": "block",
        "reason": message,
    }
    print(json.dumps(output))
    sys.exit(0)


if __name__ == "__main__":
    main()
