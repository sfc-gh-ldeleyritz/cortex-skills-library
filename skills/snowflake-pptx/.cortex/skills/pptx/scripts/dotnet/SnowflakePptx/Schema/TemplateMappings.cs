namespace SnowflakePptx.Schema;

// ── Data type ─────────────────────────────────────────────────────────────────

/// <summary>
/// Configuration for a single template slide source.
/// Maps a logical slide type to its 0-based index in the template PPTX and
/// provides the text patterns used for find-and-replace content injection.
/// </summary>
/// <param name="SlideIndex">0-based slide index in the template PPTX.</param>
/// <param name="TextPatterns">
///   Dictionary of content-key → sample text found in the template slide.
///   Used for case-insensitive substring matching during injection.
/// </param>
/// <param name="Description">Human-readable description of the slide layout.</param>
/// <param name="PreserveShapes">
///   Names of shapes that must be preserved during XML cloning (logos, icons, etc.).
/// </param>
public sealed record SlideMapping(
    int SlideIndex,
    Dictionary<string, string> TextPatterns,
    string Description = "",
    IReadOnlyList<string>? PreserveShapes = null);

// ── Static mapping table ──────────────────────────────────────────────────────

/// <summary>
/// Static dictionary mapping every supported slide-type name to its
/// <see cref="SlideMapping"/>.  Ported 1-to-1 from the Python
/// <c>TEMPLATE_SLIDE_MAPPINGS</c> in <c>template_mappings.py</c>.
/// </summary>
public static class TemplateMappings
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>All slide-type → mapping entries.</summary>
    public static readonly IReadOnlyDictionary<string, SlideMapping> Mappings =
        BuildMappings();

    /// <summary>Set of every valid slide type string.</summary>
    public static readonly HashSet<string> AllTypes =
        new(BuildMappings().Keys, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the <see cref="SlideMapping"/> for <paramref name="slideType"/>,
    /// or <c>null</c> if the type is not registered.
    /// </summary>
    public static SlideMapping? Get(string slideType) =>
        Mappings.TryGetValue(slideType, out var m) ? m : null;

    // ── Private builder ───────────────────────────────────────────────────────

    private static Dictionary<string, SlideMapping> BuildMappings() => new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Title & Cover Slides ─────────────────────────────────────────────
        // Template: 23 slides (0-22).
        // Verified via dump-template command against actual PPTX (2026-03-21).

        ["title"] = new SlideMapping(
            SlideIndex: 0,
            TextPatterns: new()
            {
                ["title_line1"] = "SNOWFLAKE",
                ["title_line2"] = "TEMPLATE 2026",
                ["subtitle"]    = "Layouts, Icons",
                ["date"]        = "January 2026",
            },
            Description: "Blue title slide with Snowflake logo and wave pattern",
            PreserveShapes: ["logo", "wave", "snowflake"]),

        ["title_wave"] = new SlideMapping(
            SlideIndex: 4,
            TextPatterns: new()
            {
                ["title"]       = "TITLE IN ARIAL BOLD,44PT ALL CAPS",
                ["subtitle"]    = "Subtitle in Arial Bold, 18pt",
                ["attribution"] = "First Lastname  |  Date",
            },
            Description: "Blue title slide with wave pattern at bottom",
            PreserveShapes: ["wave", "pattern"]),

        ["title_customer_logo"] = new SlideMapping(
            SlideIndex: 5,
            TextPatterns: new()
            {
                ["title"]       = "COVER SLIDE WITH CUSTOMER LOGO",
                ["subtitle"]    = "Useful for customized sales presentations",
                ["attribution"] = "First Lastname  |  Date",
            },
            Description: "Split blue/white title slide with Snowflake and customer logos. " +
                         "Use 'image' or 'customer_logo' field to specify logo file path.",
            PreserveShapes: ["logo", "customer_logo", "snowflake"]),

        // ── Section & Chapter Dividers ───────────────────────────────────────

        ["section"] = new SlideMapping(
            SlideIndex: 3,
            TextPatterns: new()
            {
                ["title"]    = "SAMPLE",
                ["subtitle"] = "SLIDES",
            },
            Description: "Dark blue section divider with branded graphics",
            PreserveShapes: ["wave", "pattern", "graphic"]),

        ["chapter_particle"] = new SlideMapping(
            SlideIndex: 6,
            TextPatterns: new()
            {
                ["title"]    = "CH. TITLE, ALL CAPS",
                ["subtitle"] = "OPTION 4",
            },
            Description: "Dark blue chapter divider with particle/wave pattern",
            PreserveShapes: ["particle", "wave", "pattern"]),

        // ── Content Slides ───────────────────────────────────────────────────

        ["content"] = new SlideMapping(
            SlideIndex: 7,
            TextPatterns: new()
            {
                ["title"]    = "One Column Layout",
                ["subtitle"] = "Subtitle if needed",
                ["body"]     = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
                               "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            },
            Description: "Standard content slide with title and body"),

        // ── Multi-Column Layouts ─────────────────────────────────────────────

        ["two_column_titled"] = new SlideMapping(
            SlideIndex: 8,
            TextPatterns: new()
            {
                ["title"]        = "Two Column Layout w/ Paragraph Titles",
                ["subtitle"]     = "Subtitle if needed",
                ["col1_title"]   = "Paragraph Title",
                ["col1_content"] = "Lorem ipsum dolor sit amet, consectur",
                ["col2_title"]   = "Paragraph Title",
                ["col2_content"] = "Lorem ipsum dolor sit amet, consectur",
            },
            Description: "Two columns with paragraph titles"),

        ["three_column_titled"] = new SlideMapping(
            SlideIndex: 9,
            TextPatterns: new()
            {
                ["title"]        = "Three Column Layout w/ Paragraph Titles",
                ["subtitle"]     = "Subtitle if needed",
                ["col1_title"]   = "Paragraph Title",
                ["col1_content"] = "Lorem ipsum dolor sit amet, adipiscing elit, eiusmod tempor " +
                                   "sed incididunt ut labore et dolore magna.",
                ["col2_title"]   = "Paragraph Title",
                ["col2_content"] = "Lorem ipsum dolor sit amet, adipiscing elit, eiusmod tempor " +
                                   "sed incididunt ut labore et dolore magna.",
                ["col3_title"]   = "Paragraph Title",
                ["col3_content"] = "Lorem ipsum dolor sit amet, adipiscing elit, eiusmod tempor " +
                                   "sed incididunt ut labore et dolore magna.",
            },
            Description: "Three columns with paragraph titles"),

        // three_column shares the Three Column Layout w/ Paragraph Titles template (index 9).
        // No dedicated plain three-column slide exists in the template.
        // col1/col2/col3 patterns match the body shapes for positional replacement.
        ["three_column"] = new SlideMapping(
            SlideIndex: 9,
            TextPatterns: new()
            {
                ["title"]    = "Three Column Layout w/ Paragraph Titles",
                ["subtitle"] = "Subtitle if needed",
                ["col1"]     = "Lorem ipsum dolor sit amet, adipiscing elit, eiusmod tempor " +
                               "sed incididunt ut labore et dolore magna.",
                ["col2"]     = "Lorem ipsum dolor sit amet, adipiscing elit, eiusmod tempor " +
                               "sed incididunt ut labore et dolore magna.",
                ["col3"]     = "Lorem ipsum dolor sit amet, adipiscing elit, eiusmod tempor " +
                               "sed incididunt ut labore et dolore magna.",
            },
            Description: "Three columns (no individual column titles)"),

        ["three_column_icons"] = new SlideMapping(
            SlideIndex: 10,
            TextPatterns: new()
            {
                ["title"]        = "Three Column Layout w/ Icons",
                ["subtitle"]     = "Subtitle if needed",
                ["col1_title"]   = "Paragraph Title",
                ["col1_content"] = "Lorem ipsum dolor sit",
                ["col2_title"]   = "Paragraph Title",
                ["col2_content"] = "Lorem ipsum dolor sit",
                ["col3_title"]   = "Paragraph Title",
                ["col3_content"] = "Lorem ipsum dolor sit",
            },
            Description: "Three columns with icons and paragraph titles",
            PreserveShapes: ["icon"]),

        ["four_column_numbers"] = new SlideMapping(
            SlideIndex: 11,
            TextPatterns: new()
            {
                ["title"]        = "Four Column Layout w/ Big Numbers",
                ["subtitle"]     = "Subtitle if needed",
                ["col1_number"]  = "67%",
                ["col1_content"] = "Lorem ipsum dolor sit amet, consectetur",
                ["col2_number"]  = ">10M",
                ["col2_content"] = "Lorem ipsum dolor sit amet, consectetur",
                ["col3_number"]  = "+55%",
                ["col3_content"] = "Lorem ipsum dolor sit amet, consectetur",
                ["col4_number"]  = "110B",
                ["col4_content"] = "Lorem ipsum dolor sit amet, consectetur",
            },
            Description: "Four columns with big numbers stats"),

        ["four_column_icons"] = new SlideMapping(
            SlideIndex: 12,
            TextPatterns: new()
            {
                ["title"]        = "Four Column Layout w/ Icons",
                ["subtitle"]     = "Subtitle if needed",
                ["col1_title"]   = "Education On Snowflake",
                ["col1_content"] = "Lorem ipsum dolor sit",
                ["col2_title"]   = "Mining On Snowflake",
                ["col2_content"] = "Lorem ipsum dolor sit",
                ["col3_title"]   = "Data Providers On Snowflake",
                ["col3_content"] = "Lorem ipsum dolor sit",
                ["col4_title"]   = "BankingOn Snowflake",
                ["col4_content"] = "Lorem ipsum dolor sit",
            },
            Description: "Four columns with icons and titles",
            PreserveShapes: ["icon"]),

        ["split"] = new SlideMapping(
            SlideIndex: 13,
            TextPatterns: new()
            {
                ["title"]         = "Split Slide",
                ["subtitle"]      = "One side for graphic, the other for text",
                ["content_title"] = "Lorem ipsum dolor sit amet adipiscing elit eiusmod tempor incididunt ut labore",
                ["content"]       = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor ed incididunt ut labore et dolore magna aliqua.",
            },
            Description: "Half image, half content with blue background",
            PreserveShapes: ["image", "graphic"]),

        // ── Quote Slides ─────────────────────────────────────────────────────

        ["quote"] = new SlideMapping(
            SlideIndex: 14,
            TextPatterns: new()
            {
                ["quote"]       = "The wisdom of the wise, and the experience of ages, may be preserved by quotation.",
                ["attribution"] = "Name  |  Title",
                ["company"]     = "Company Name",
            },
            Description: "Blue background quote without photo",
            PreserveShapes: ["quotation", "quote"]),

        ["quote_photo"] = new SlideMapping(
            SlideIndex: 15,
            TextPatterns: new()
            {
                ["quote"]       = "Quotation lorem ipsum dolor sit amet, elit consectetur adipiscing.",
                ["attribution"] = "Name  |  Title",
                ["company"]     = "Company Name",
            },
            Description: "Blue background quote with person photo",
            PreserveShapes: ["quotation", "quote", "photo"]),

        ["quote_simple"] = new SlideMapping(
            SlideIndex: 16,
            TextPatterns: new()
            {
                ["quote"]       = "The wisdom of the wise, and the experience of ages, may be preserved by quotation.",
                ["attribution"] = "Name  |  Title",
                ["company"]     = "Company Name",
            },
            Description: "White background quote with large blue quotation marks",
            PreserveShapes: ["quotation", "quote_mark"]),

        // ── Agenda Slides ────────────────────────────────────────────────────

        ["agenda"] = new SlideMapping(
            SlideIndex: 1,
            TextPatterns: new()
            {
                ["title"] = "Template Contents",
                ["body"]  = "Template Instructions",
            },
            Description: "Agenda slide with bullet items (Template Contents layout)"),

        // ── Special Slides ───────────────────────────────────────────────────

        ["safe_harbor"] = new SlideMapping(
            SlideIndex: 2,
            TextPatterns: new()
            {
                ["title"] = "Safe Harbor and Disclaimers",
                ["body"]  = "2026 Snowflake Inc. All rights reserved.",
                ["rev"]   = "REV 12.16.25",
            },
            Description: "Safe Harbor and Disclaimers legal slide",
            PreserveShapes: ["accent", "bar"]),

        ["table_styled"] = new SlideMapping(
            SlideIndex: 17,
            TextPatterns: new()
            {
                ["title"]    = "Table Example 1",
                ["subtitle"] = "Subtitle if needed",
            },
            Description: "Styled table with row headers in first column",
            PreserveShapes: ["table"]),

        ["table_striped"] = new SlideMapping(
            SlideIndex: 18,
            TextPatterns: new()
            {
                ["title"]    = "Table Example 2",
                ["subtitle"] = "Subtitle if needed",
            },
            Description: "Striped table with emphasized header rows",
            PreserveShapes: ["table"]),

        ["speaker_headshots"] = new SlideMapping(
            SlideIndex: 19,
            TextPatterns: new()
            {
                ["title"]   = "Speaker Headshots",
                ["name"]    = "Firstname Lastname",
                ["title_"]  = "Title",
                ["company"] = "Company",
            },
            Description: "Speaker headshots slide with up to 4 circular headshot photos",
            PreserveShapes: ["photo", "headshot", "circle"]),

        ["two_column"] = new SlideMapping(
            SlideIndex: 20,
            TextPatterns: new()
            {
                ["title"]      = "Two Column Layout",
                ["subtitle"]   = "Subtitle if needed",
                ["body_left"]  = "Lorem ipsum dolor sit amet, consectur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna.",
                ["body_right"] = "Lorem ipsum dolor sit amet, consectur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna.",
            },
            Description: "Two columns without paragraph titles"),

        ["title_headshot"] = new SlideMapping(
            SlideIndex: 21,
            TextPatterns: new()
            {
                ["title_line1"]   = "SNOWFLAKE",
                ["title_line2"]   = "TEMPLATE 2026",
                ["subtitle"]      = "Layouts, Icons, and Frequently Used Slides",
                ["date"]          = "January 2026",
                ["name"]          = "Alex Ross",
                ["speaker_title"] = "Principal Solution Engineer",
                ["company"]       = "",  // handled positionally by ReplaceTitleCompanyShape; must not collide with title_line1 "SNOWFLAKE"
            },
            Description: "Blue title slide with presenter headshot photo",
            PreserveShapes: ["logo", "wave", "snowflake"]),

        ["thank_you"] = new SlideMapping(
            SlideIndex: 22,
            TextPatterns: new()
            {
                ["title"]       = "THANK",
                ["title_line2"] = "YOU",
            },
            Description: "Blue background Thank You slide",
            PreserveShapes: ["logo", "wave", "snowflake"]),
    };
}
