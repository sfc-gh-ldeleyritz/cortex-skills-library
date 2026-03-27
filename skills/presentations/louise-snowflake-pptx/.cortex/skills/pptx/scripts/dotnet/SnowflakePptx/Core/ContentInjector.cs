using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using SnowflakePptx.Assets;
using SnowflakePptx.Formatting;
using SnowflakePptx.Schema;
using A = DocumentFormat.OpenXml.Drawing;

namespace SnowflakePptx.Core;

/// <summary>
/// Injects content into a cloned slide by replacing placeholder text.
///
/// C# port of XMLSlideCloner._inject_content and its helpers.
///
/// The algorithm mirrors the Python implementation:
///   1. Build a pattern→replacement dictionary (content_map).
///   2. First pass: match patterns against individual &lt;a:t&gt; run text.
///   3. Second pass: match patterns against the concatenated text of each shape
///      (handles text fragmented across multiple runs).
///   4. Apply autofit to large text shapes.
///   5. Delete shapes that still contain un-replaced template placeholder text.
/// </summary>
public class ContentInjector
{
    // ── Asset catalog (for image resolution) ─────────────────────────────────

    private readonly AssetCatalog _catalog;

    public ContentInjector() : this(new AssetCatalog()) { }
    public ContentInjector(AssetCatalog catalog) { _catalog = catalog; }

    // ── Autofit / preserve sets ──────────────────────────────────────────────

    // Shapes that should keep spAutoFit (grow-to-fit) rather than normAutofit.
    // Key: (slideType, shapeName)
    private static readonly HashSet<(string, string)> SpAutofitPreserve = new()
    {
        ("four_column_numbers", "PlaceHolder 4"),
        // three_column_icons body shapes use spAutoFit + 3 leading empty paragraphs
        // to position content below the floating title shapes. Converting to normAutofit
        // causes the text box to shrink-to-fit and render text from the top of the box,
        // colliding with the title labels that float at a fixed y-position mid-column.
        ("three_column_icons",  "PlaceHolder 2"),
        ("three_column_icons",  "PlaceHolder 3"),
        ("three_column_icons",  "PlaceHolder 4"),
        // four_column_icons content and title shapes use spAutoFit in the updated template
        ("four_column_icons", "PlaceHolder 2"),
        ("four_column_icons", "PlaceHolder 3"),
        ("four_column_icons", "PlaceHolder 4"),
        ("four_column_icons", "PlaceHolder 5"),
        ("four_column_icons", "Google Shape;755;p70"),
        ("four_column_icons", "Google Shape;756;p70"),
        ("four_column_icons", "Google Shape;757;p70"),
        ("four_column_icons", "Google Shape;759;p70"),
    };

    // Shapes that should keep noAutofit (decorative labels — don't auto-shrink).
    private static readonly HashSet<(string, string)> NoAutofitPreserve = new()
    {
        ("content",          "PlaceHolder 4"),
        ("two_column_titled","PlaceHolder 5"),
    };

    // Minimum run sz (hundredths of a point) per (slideType, shapeName).
    // Fixes template-baked undersized runs.
    private static readonly Dictionary<(string, string), int> RunSizeOverrides = new()
    {
        [("quote",              "PlaceHolder 3")] = 1800,
        [("quote_photo",        "PlaceHolder 3")] = 1800,
        [("quote_simple",       "PlaceHolder 2")] = 1800,
        // four_column_numbers content shapes: sized dynamically by
        // ApplyFourColumnNumbersContentFontSize (floor: 14pt / sz 1400).
        // Number shapes are sized by ApplyFourColumnNumbersNumberFontSize.
    };

    // fontScale / lnSpcReduction overrides keyed by (slideType, shapeName).
    private static readonly Dictionary<(string, string), Dictionary<string, string>> FontScaleOverrides = new()
    {
        [("three_column_titled","Google Shape;705;p67")] = new() { ["fontScale"] = "92500", ["lnSpcReduction"] = "9999" },
        [("three_column_titled","Google Shape;706;p67")] = new() { ["fontScale"] = "92500", ["lnSpcReduction"] = "9999" },
        [("three_column_titled","Google Shape;707;p67")] = new() { ["fontScale"] = "92500", ["lnSpcReduction"] = "9999" },
        [("split",              "PlaceHolder 1")]         = new() { ["lnSpcReduction"] = "9999" },
        [("quote_photo",        "PlaceHolder 1")]         = new() { ["fontScale"] = "85000", ["lnSpcReduction"] = "9999" },
        [("quote_photo",        "PlaceHolder 3")]         = new() { ["lnSpcReduction"] = "9999" },
        [("table_styled",       "PlaceHolder 1")]         = new() { ["lnSpcReduction"] = "9999" },
        [("title",              "PlaceHolder 3")]         = new() { ["fontScale"] = "55000", ["lnSpcReduction"] = "19999" },
        [("title_headshot",     "PlaceHolder 3")]         = new() { ["fontScale"] = "55000", ["lnSpcReduction"] = "19999" },
        [("title_wave",         "PlaceHolder 2")]         = new() { ["fontScale"] = "55000", ["lnSpcReduction"] = "19999" },
        [("title_customer_logo","PlaceHolder 1")]         = new() { ["fontScale"] = "85000", ["lnSpcReduction"] = "9999" },
        [("title_customer_logo","PlaceHolder 2")]         = new() { ["lnSpcReduction"] = "9999" },
        [("quote",              "PlaceHolder 1")]         = new() { ["fontScale"] = "85000", ["lnSpcReduction"] = "9999" },
        [("quote_simple",       "PlaceHolder 1")]         = new() { ["fontScale"] = "60000", ["lnSpcReduction"] = "9999" },
    };

    // Minimum shape height (EMU) below which autofit is not applied.
    private const int MinContentHeight = 200_000;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Main entry point. Injects <paramref name="content"/> into
    /// <paramref name="slidePart"/> according to the slide type and mapping.
    /// </summary>
    public virtual void InjectContent(
        SlidePart slidePart,
        Dictionary<string, object?> content,
        string slideType,
        SlideMapping mapping,
        float titleSubtitleSpacing = 1.5f)
    {
        var slideRoot = slidePart.Slide;

        // safe_harbor is a fixed legal slide — clone it exactly, no modifications.
        if (string.Equals(slideType, "safe_harbor", StringComparison.OrdinalIgnoreCase))
            return;

        // ── Build content map ─────────────────────────────────────────────────
        var contentMap = BuildContentMap(content, slideType, mapping);

        // ── Slide-type-specific positional replacements ───────────────────────
        // These run BEFORE regular pattern replacement and handle shapes that
        // share identical template text and must be distinguished by x-position.
        HandlePositionalReplacements(slidePart, slideRoot, content, slideType, mapping);

        // ── Perform text replacements ─────────────────────────────────────────
        ReplaceTextByPattern(slideRoot, contentMap);

        // ── Apply autofit ─────────────────────────────────────────────────────
        ApplyAutofitToAllShapes(slideRoot, slideType);

        // ── Content bullet font scaling (must run after normAutofit is set) ──
        if (string.Equals(slideType, "content", StringComparison.OrdinalIgnoreCase))
            ApplyContentBulletFontScale(slideRoot, content, mapping);

        ApplyFontScaleOverrides(slideRoot, slideType);
        EnforceRunSizes(slideRoot, slideType);

        // ── Delete unfilled placeholders ──────────────────────────────────────
        DeleteUnfilledPlaceholders(slidePart, slideRoot, content, mapping, slideType);

        // ── Speaker notes ──────────────────────────────────────────────────────
        var notesText = content.TryGetValue("notes", out var nObj) ? nObj?.ToString() : null;
        if (!string.IsNullOrWhiteSpace(notesText))
            WriteNotesSlide(slidePart, notesText!.Trim());
    }

    // ── Content map builder ──────────────────────────────────────────────────

    /// <summary>
    /// Build the pattern → replacement string dictionary from the raw content dict.
    /// Mirrors Python XMLSlideCloner._inject_content logic section-by-section.
    /// </summary>
    public Dictionary<string, string> BuildContentMap(
        Dictionary<string, object?> content,
        string slideType,
        SlideMapping mapping)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Title handling ────────────────────────────────────────────────────
        switch (slideType)
        {
            case "title":
                HandleTitleSlideMap(content, mapping, map);
                break;

            case "title_headshot":
                HandleTitleSlideMap(content, mapping, map);
                break;

            case "title_wave":
            case "title_particle":
                // Fragmented titles are handled directly in HandlePositionalReplacements.
                break;

            case "title_customer_logo":
                // Also handled positionally.
                break;

            case "thank_you":
                HandleThankYouMap(content, mapping, map);
                break;

            default:
                if (content.TryGetValue("title", out var titleObj) && titleObj is not null)
                {
                    var title = titleObj.ToString()!;
                    if (slideType is "section" or "section_dots" or "chapter_particle")
                        title = title.ToUpperInvariant();

                    var pattern = mapping.TextPatterns.GetValueOrDefault("title", "");
                    if (!string.IsNullOrEmpty(pattern))
                        map[pattern.ToLowerInvariant()] = title;
                }
                break;
        }

        // ── Subtitle handling ─────────────────────────────────────────────────
        var subtitleParts = new List<string>();
        bool subtitleExplicitlyProvided = content.ContainsKey("subtitle");

        if (subtitleExplicitlyProvided && content["subtitle"] is { } subObj
            && !string.IsNullOrEmpty(subObj.ToString()))
        {
            subtitleParts.Add(subObj.ToString()!);
        }

        if (slideType == "title" || slideType == "title_headshot")
        {
            // For the title slide, author + date go to the dedicated "date" shape.
            var datePattern = mapping.TextPatterns.GetValueOrDefault("date", "");
            if (!string.IsNullOrEmpty(datePattern))
            {
                var dateParts = new List<string>();
                if (GetString(content, "author") is { } a && !string.IsNullOrEmpty(a)) dateParts.Add(a);
                if (GetString(content, "date")   is { } d && !string.IsNullOrEmpty(d)) dateParts.Add(d);
                if (dateParts.Count > 0)
                    map[datePattern.ToLowerInvariant()] = string.Join(" | ", dateParts);
            }
        }
        else
        {
            var infoParts = new List<string>();
            if (GetString(content, "author") is { } a && !string.IsNullOrEmpty(a)) infoParts.Add(a);
            if (GetString(content, "date")   is { } d && !string.IsNullOrEmpty(d)) infoParts.Add(d);
            if (infoParts.Count > 0)
                subtitleParts.Add(string.Join(" | ", infoParts));
        }

        if (subtitleParts.Count > 0 && (subtitleExplicitlyProvided || slideType != "blank"))
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("subtitle", "");
            if (!string.IsNullOrEmpty(pattern))
            {
                var subtitleText = string.Join("\n", subtitleParts);
                if (slideType is "section" or "section_dots" or "chapter_particle")
                    subtitleText = subtitleText.ToUpperInvariant();
                map[pattern.ToLowerInvariant()] = subtitleText;
            }
        }

        // ── Body / bullets ────────────────────────────────────────────────────
        // Content slide types handle bullets positionally — only set body for others.
        var contentSlideTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "content", "content_long_title", "content_wave_1", "content_wave_2", "content_arrows"
        };

        if (content.ContainsKey("bullets") && !contentSlideTypes.Contains(slideType))
        {
            var bodyText = FormatBullets(content["bullets"]);
            var pattern = mapping.TextPatterns.GetValueOrDefault("body", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = bodyText;
        }
        else if (GetString(content, "body") is { } body && !string.IsNullOrEmpty(body))
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("body", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = body;
        }

        // ── Quote handling ────────────────────────────────────────────────────
        if (GetString(content, "quote") is { } quote && !string.IsNullOrEmpty(quote))
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("quote", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = quote;
        }

        // Attribution: synthesise from author + role for quote slide types.
        var attributionParts = new List<string>();
        if (slideType is "quote" or "quote_photo" or "quote_simple")
        {
            if (GetString(content, "author") is { } attrAuthor && !string.IsNullOrEmpty(attrAuthor))
                attributionParts.Add(attrAuthor.TrimStart('—').TrimStart(' '));
            if (GetString(content, "role") is { } role && !string.IsNullOrEmpty(role))
                attributionParts.Add(role);
        }

        if (attributionParts.Count > 0)
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("attribution", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = string.Join(" | ", attributionParts);
        }
        else if (GetString(content, "attribution") is { } attribution && !string.IsNullOrEmpty(attribution))
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("attribution", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = attribution;
        }

        // Company field
        if (GetString(content, "company") is { } company && !string.IsNullOrEmpty(company))
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("company", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = company;
        }

        // ── Two-column body ───────────────────────────────────────────────────
        // two_column uses positional replacement (x-sort) — skip pattern map here.
        if (slideType != "two_column")
        foreach (var side in new[] { "left", "right" })
        {
            foreach (var key in new[] { $"body_{side}", $"{side}_content" })
            {
                if (GetString(content, key) is { } sideText && !string.IsNullOrEmpty(sideText))
                {
                    var text    = FormatContent(content[key]);
                    var pattern = mapping.TextPatterns.GetValueOrDefault($"body_{side}", "");
                    if (!string.IsNullOrEmpty(pattern))
                        map[pattern.ToLowerInvariant()] = text;
                    break;
                }
            }
        }

        // ── Multi-column content ──────────────────────────────────────────────
        // two_column_titled and three_column_titled use positional replacement for
        // col*_title and col*_content — skip those here.
        var positionalColTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "two_column_titled", "three_column_titled"
        };

        for (int colNum = 1; colNum <= 4; colNum++)
        {
            var colPrefix = $"col{colNum}";
            foreach (var suffix in new[] { "_title", "_content", "_number" })
            {
                var colKey = $"{colPrefix}{suffix}";
                if (suffix is "_title" or "_content" && positionalColTypes.Contains(slideType))
                    continue;
                if (GetString(content, colKey) is { } colVal && !string.IsNullOrEmpty(colVal))
                {
                    var pattern = mapping.TextPatterns.GetValueOrDefault(colKey, "");
                    if (!string.IsNullOrEmpty(pattern))
                        map[pattern.ToLowerInvariant()] = colVal;
                }
            }

            // Simple col (col1, col2, col3) — only when col*_content is absent.
            var simpleKey = $"col{colNum}";
            if (GetString(content, simpleKey) is { } simpleVal
                && !string.IsNullOrEmpty(simpleVal)
                && !content.ContainsKey($"{simpleKey}_content"))
            {
                var pattern = mapping.TextPatterns.GetValueOrDefault(simpleKey, "");
                if (!string.IsNullOrEmpty(pattern))
                    map[pattern.ToLowerInvariant()] = simpleVal;
            }
        }

        // ── Split slide content ───────────────────────────────────────────────
        if (GetString(content, "content_title") is { } ctitle && !string.IsNullOrEmpty(ctitle))
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("content_title", "");
            if (!string.IsNullOrEmpty(pattern))
                map[pattern.ToLowerInvariant()] = ctitle;
        }
        if (content.TryGetValue("content", out var contentObj) && contentObj is not null)
        {
            var text = FormatContent(contentObj);
            if (!string.IsNullOrEmpty(text))
            {
                var pattern = mapping.TextPatterns.GetValueOrDefault("content", "");
                if (!string.IsNullOrEmpty(pattern))
                    map[pattern.ToLowerInvariant()] = text;
            }
        }

        return map;
    }

    // ── Title-specific map builders ──────────────────────────────────────────

    private static void HandleTitleSlideMap(
        Dictionary<string, object?> content,
        SlideMapping mapping,
        Dictionary<string, string> map)
    {
        if (GetString(content, "title_line1") is { } l1 && !string.IsNullOrEmpty(l1))
        {
            var p = mapping.TextPatterns.GetValueOrDefault("title_line1", "");
            if (!string.IsNullOrEmpty(p)) map[p.ToLowerInvariant()] = l1.ToUpperInvariant();
        }
        if (GetString(content, "title_line2") is { } l2 && !string.IsNullOrEmpty(l2))
        {
            var p = mapping.TextPatterns.GetValueOrDefault("title_line2", "");
            if (!string.IsNullOrEmpty(p)) map[p.ToLowerInvariant()] = l2.ToUpperInvariant();
        }

        // Legacy: single 'title' field (when title_line1 was not supplied).
        if (GetString(content, "title") is { } title && !string.IsNullOrEmpty(title)
            && !content.ContainsKey("title_line1"))
        {
            var upper = title.ToUpperInvariant();
            if (upper.Contains('\n'))
            {
                var parts = upper.Split('\n', 2);
                var p1 = mapping.TextPatterns.GetValueOrDefault("title_line1", "");
                var p2 = mapping.TextPatterns.GetValueOrDefault("title_line2", "");
                if (!string.IsNullOrEmpty(p1)) map[p1.ToLowerInvariant()] = parts[0];
                if (!string.IsNullOrEmpty(p2)) map[p2.ToLowerInvariant()] = parts[1];
            }
            else
            {
                var p = mapping.TextPatterns.GetValueOrDefault("title_line1", "");
                if (!string.IsNullOrEmpty(p)) map[p.ToLowerInvariant()] = upper;
            }
        }
    }

    private static void HandleThankYouMap(
        Dictionary<string, object?> content,
        SlideMapping mapping,
        Dictionary<string, string> map)
    {
        bool hasTitle = (!string.IsNullOrEmpty(GetString(content, "title")))
                     || (!string.IsNullOrEmpty(GetString(content, "title_line1")));
        if (!hasTitle) return;

        if (GetString(content, "title") is { } t && !string.IsNullOrEmpty(t))
        {
            var p = mapping.TextPatterns.GetValueOrDefault("title", "");
            if (!string.IsNullOrEmpty(p)) map[p.ToLowerInvariant()] = t.ToUpperInvariant();
        }
        if (GetString(content, "title_line2") is { } t2 && !string.IsNullOrEmpty(t2))
        {
            var p = mapping.TextPatterns.GetValueOrDefault("title_line2", "");
            if (!string.IsNullOrEmpty(p)) map[p.ToLowerInvariant()] = t2.ToUpperInvariant();
        }
    }

    // ── Text replacement engine ──────────────────────────────────────────────

    /// <summary>
    /// Replace placeholder text in slide shapes using the content map.
    ///
    /// Two-pass algorithm matching the Python implementation:
    ///   Pass 1 — match pattern against individual &lt;a:t&gt; run text.
    ///   Pass 2 — for unmatched patterns, match against concatenated shape text
    ///            (handles text fragmented across multiple runs).
    /// </summary>
    public void ReplaceTextByPattern(
        OpenXmlElement slideElement,
        Dictionary<string, string> contentMap)
    {
        if (contentMap.Count == 0) return;

        var usedPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pass 1: run-level matching
        foreach (var shape in GetTextShapes(slideElement))
        {
            ProcessShapeTextRunLevel(shape, contentMap, usedPatterns);
        }

        // Pass 2: shape-level matching (fragmented runs)
        foreach (var (pattern, replacement) in contentMap)
        {
            if (usedPatterns.Contains(pattern)) continue;
            if (string.IsNullOrEmpty(replacement)) continue;

            if (ReplaceShapeTextByPattern(slideElement, pattern, replacement))
                usedPatterns.Add(pattern);
        }
    }

    /// <summary>
    /// Pass 1: try to match each pattern against a single &lt;a:t&gt; element's text.
    /// On match, replace that element's text and mark the pattern used.
    /// </summary>
    private static void ProcessShapeTextRunLevel(
        OpenXmlElement shape,
        Dictionary<string, string> contentMap,
        HashSet<string> usedPatterns)
    {
        var txBody = FindTxBody(shape);
        if (txBody is null) return;

        foreach (var tElem in txBody.Descendants<A.Text>())
        {
            if (tElem.Text is null) continue;
            var runLower = tElem.Text.ToLowerInvariant();

            foreach (var (pattern, replacement) in contentMap)
            {
                if (usedPatterns.Contains(pattern)) continue;
                if (!runLower.Contains(pattern)) continue;

                if (string.IsNullOrEmpty(replacement))
                {
                    usedPatterns.Add(pattern);
                    continue;
                }

                tElem.Text = replacement;
                usedPatterns.Add(pattern);
                break; // one pattern per <a:t> element — move on
            }
        }
    }

    /// <summary>
    /// Pass 2: match pattern against the concatenated text of an entire shape.
    /// Puts the replacement text in the first &lt;a:t&gt; and blanks all others.
    /// Returns true if a match was found and replaced.
    /// </summary>
    private static bool ReplaceShapeTextByPattern(
        OpenXmlElement slideRoot,
        string pattern,
        string replacement)
    {
        var lowerPattern = pattern.ToLowerInvariant();

        foreach (var shape in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(shape);
            if (txBody is null) continue;

            var shapeText = GetAllRunText(txBody);
            if (!shapeText.ToLowerInvariant().Contains(lowerPattern)) continue;

            var tElems = txBody.Descendants<A.Text>().ToList();
            for (int i = 0; i < tElems.Count; i++)
                tElems[i].Text = i == 0 ? replacement : string.Empty;

            return true;
        }
        return false;
    }

    // ── Positional replacement dispatch ──────────────────────────────────────

    /// <summary>
    /// Override in a derived class (or SlideTypeHandler) to handle slide types
    /// that require positional shape matching (identical template text in
    /// multiple shapes distinguished only by x-coordinate).
    ///
    /// This is called BEFORE ReplaceTextByPattern so positional replacements
    /// take precedence.
    /// </summary>
    protected virtual void HandlePositionalReplacements(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        string slideType,
        SlideMapping mapping)
    {
        switch (slideType)
        {
            case "two_column":
                ReplaceTwoColumnBodyPatterns(slideRoot, content, mapping, slideType);
                break;

            case "two_column_titled":
            case "three_column_titled":
                ReplaceMultiColumnPatterns(slideRoot, content, slideType, mapping);
                break;

            case "three_column":
                ReplaceThreeColumnBodyPatterns(slideRoot, content, mapping);
                break;

            case "three_column_icons":
                ReplaceIconColumnPatterns(slideRoot, content, mapping);
                InjectColumnIcons(slidePart, slideRoot, content, 3);
                break;

            case "four_column_numbers":
                ReplaceFourColumnNumberPatterns(slidePart, slideRoot, content, mapping);
                // Positional label alignment: collect by Y position, exclude number shapes
                {
                    const long BodyYThreshold = 1_200_000L;
                    var labelShapes = slideRoot.Descendants<Shape>()
                        .Where(sp => {
                            var txBody = sp.TextBody;  // P.TextBody, not A.TextBody
                            if (txBody == null) return false;
                            var txt = string.Concat(txBody.Descendants<A.Text>().Select(t => t.Text));
                            var spPr = sp.Elements<ShapeProperties>().FirstOrDefault();
                            var y = spPr?.Transform2D?.Offset?.Y?.Value ?? 0L;
                            if (y < BodyYThreshold) return false;
                            var trimmed = txt.Trim();
                            return !(trimmed.Length < 10 && trimmed.Any(char.IsDigit));
                        })
                        .OrderBy(sp => sp.Elements<ShapeProperties>().FirstOrDefault()?.Transform2D?.Offset?.X?.Value ?? long.MaxValue)
                        .Take(4)
                        .ToList();
                    foreach (var sp in labelShapes)
                    {
                        var bodyPr = sp.Descendants<A.BodyProperties>().FirstOrDefault();
                        if (bodyPr != null) bodyPr.Anchor = A.TextAnchoringTypeValues.Top;
                    }
                    if (labelShapes.Count == 4)
                    {
                        // Normalize Y positions to the minimum Y found
                        long minY = labelShapes.Min(sp => sp.Elements<ShapeProperties>().FirstOrDefault()?.Transform2D?.Offset?.Y?.Value ?? long.MaxValue);
                        // Normalize heights to the minimum height found
                        long minH = labelShapes.Min(sp => sp.Elements<ShapeProperties>().FirstOrDefault()?.Transform2D?.Extents?.Cy?.Value ?? long.MaxValue);
                        foreach (var sp in labelShapes)
                        {
                            var xfrm = sp.Elements<ShapeProperties>().FirstOrDefault()?.Transform2D;
                            if (xfrm?.Offset != null)
                                xfrm.Offset.Y = minY;
                            if (xfrm?.Extents != null)
                                xfrm.Extents.Cy = minH;
                        }

                        // Strip extra leading empty paragraphs so all label shapes have the same paragraph structure.
                        // Template col1 shape may have an extra spacer paragraph, causing its text to render lower.
                        static int CountLeadingEmptyParas(OpenXmlElement txBody)
                        {
                            int count = 0;
                            foreach (var para in txBody.Elements<A.Paragraph>())
                            {
                                var allText = string.Concat(para.Descendants<A.Text>().Select(t => t.Text));
                                if (string.IsNullOrWhiteSpace(allText))
                                    count++;
                                else
                                    break;
                            }
                            return count;
                        }

                        int minLeading = labelShapes.Min(sp => CountLeadingEmptyParas(sp.TextBody!));
                        foreach (var sp in labelShapes)
                        {
                            var txBody = sp.TextBody;
                            if (txBody == null) continue;
                            int leading = CountLeadingEmptyParas(txBody);
                            int toRemove = leading - minLeading;
                            for (int i = 0; i < toRemove; i++)
                            {
                                var firstPara = txBody.Elements<A.Paragraph>().FirstOrDefault();
                                firstPara?.Remove();
                            }
                        }
                    }
                }
                break;

            case "four_column_icons":
                ReplaceFourColumnIconPatterns(slideRoot, content, mapping);
                InjectColumnIcons(slidePart, slideRoot, content, 4);
                break;

            case "speaker_headshots":
                ReplaceSpeakerHeadshotsPatterns(slidePart, slideRoot, content, mapping);
                break;

            case "title":
                CenterTitleText(slideRoot);
                break;

            case "title_wave":
            case "title_particle":
                HandleFragmentedTitle(slideRoot, content, mapping);
                break;

            case "title_customer_logo":
                HandleCustomerLogoTitle(slidePart, slideRoot, content, mapping);
                break;

            case "title_headshot":
                HandleTitleHeadshotSlide(slidePart, slideRoot, content, mapping);
                break;

            case "quote":
                CenterQuoteBodyAnchor(slideRoot, mapping);
                break;

            case "quote_photo":
                HandleQuotePhoto(slidePart, slideRoot, content);
                break;

            case "quote_simple":
                HandleQuoteSimpleAttributionPosition(slideRoot, content, mapping);
                break;

            case "split":
                HandleSplitImage(slidePart, slideRoot, content);
                HandleSplitContent(slideRoot, content, mapping);
                break;

            case "content":
                HandleContentBullets(slideRoot, content, mapping);
                break;

            case "agenda":
                HandleAgendaItems(slideRoot, content, mapping);
                break;

            case "table_styled":
            case "table_striped":
                HandleTableData(slidePart, slideRoot, content, slideType);
                break;
        }
    }

    // ── Slide-type-specific positional methods ───────────────────────────────
    // Full implementations are here. All methods mirror their Python counterparts.

    /// <summary>
    /// Replace patterns in two_column_titled / three_column_titled slides
    /// by sorting shapes by x-position and pairing them with col1..N content.
    /// </summary>
    protected virtual void ReplaceMultiColumnPatterns(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        string slideType,
        SlideMapping mapping)
    {
        int numCols = slideType == "two_column_titled" ? 2 : 3;
        var titlePattern   = mapping.TextPatterns.GetValueOrDefault("col1_title", "").ToLowerInvariant().Trim();
        var contentPattern = mapping.TextPatterns.GetValueOrDefault("col1_content", "").ToLowerInvariant().Trim();

        if (string.IsNullOrEmpty(titlePattern)) return;

        var titleShapes   = new List<(int X, OpenXmlElement TxBody)>();
        var contentShapes = new List<(int X, OpenXmlElement TxBody)>();

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant().Trim();

            if (text == titlePattern)
                titleShapes.Add((GetShapeXPos(sp), txBody));
            else if (!string.IsNullOrEmpty(contentPattern) && text.StartsWith(contentPattern))
                contentShapes.Add((GetShapeXPos(sp), txBody));
        }

        titleShapes   = titleShapes.OrderBy(x => x.X).ToList();
        contentShapes = contentShapes.OrderBy(x => x.X).ToList();

        for (int i = 0; i < Math.Min(numCols, titleShapes.Count); i++)
        {
            var key = $"col{i + 1}_title";
            if (GetString(content, key) is { } val && !string.IsNullOrEmpty(val))
            {
                var tElems = titleShapes[i].TxBody.Descendants<A.Text>().ToList();
                var match  = tElems.FirstOrDefault(
                    t => t.Text?.ToLowerInvariant().Trim() == titlePattern);
                if (match != null)
                    match.Text = val;
            }
        }

        for (int i = 0; i < Math.Min(numCols, contentShapes.Count); i++)
        {
            var key = $"col{i + 1}_content";
            if (GetString(content, key) is { } val && !string.IsNullOrEmpty(val))
            {
                var tElems = contentShapes[i].TxBody.Descendants<A.Text>().ToList();
                if (tElems.Count >= 2)
                {
                    tElems[0].Text = string.Empty;
                    tElems[1].Text = val;
                    for (int j = 2; j < tElems.Count; j++) tElems[j].Text = string.Empty;
                }
                else if (tElems.Count == 1)
                {
                    tElems[0].Text = val;
                }
            }
        }
    }

    /// <summary>
    /// Replace body patterns in two_column / comparison slides using
    /// x-position ordering (left shape first, right shape second).
    /// </summary>
    protected virtual void ReplaceTwoColumnBodyPatterns(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping,
        string slideType)
    {
        var bodyPattern = mapping.TextPatterns.GetValueOrDefault("body_left", "").ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(bodyPattern)) return;

        // Normalise content keys
        TryAliasKey(content, "left_content",  "body_left");
        TryAliasKey(content, "right_content", "body_right");
        TryAliasKey(content, "before",        "body_left");
        TryAliasKey(content, "after",         "body_right");

        var hasLeft  = GetString(content, "body_left")  is { Length: > 0 };
        var hasRight = GetString(content, "body_right") is { Length: > 0 };
        if (!hasLeft && !hasRight) return;

        var bodyShapes = new List<(int X, OpenXmlElement TxBody)>();
        var prefix = bodyPattern.Length >= 20 ? bodyPattern[..20] : bodyPattern;

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant().Trim();
            if (!text.Contains(prefix)) continue;
            bodyShapes.Add((GetShapeXPos(sp), txBody));
        }

        bodyShapes = bodyShapes.OrderBy(x => x.X).ToList();

        foreach (var (idx, sideKey) in new[] { (0, "body_left"), (1, "body_right") })
        {
            if (GetString(content, sideKey) is not { Length: > 0 } sideText) continue;
            if (idx >= bodyShapes.Count) continue;

            var items = sideText.Contains('\n')
                ? sideText.Split('\n').ToList()
                : new List<string> { sideText };

            ReplaceBodyWithParagraphs(bodyShapes[idx].TxBody, items);
        }

        // Column headers (left_header / right_header)
        TryAliasKey(content, "left_header",  "before_title");
        TryAliasKey(content, "right_header", "after_title");

        var hasLeftTitle  = GetString(content, "before_title") is { Length: > 0 };
        var hasRightTitle = GetString(content, "after_title")  is { Length: > 0 };
        if (!hasLeftTitle && !hasRightTitle) return;

        var titlePatternStr = (mapping.TextPatterns.GetValueOrDefault("left_header", "")
                            is { Length: > 0 } lh ? lh
                            : mapping.TextPatterns.GetValueOrDefault("col1_title", ""))
                            .ToLowerInvariant().Trim();

        if (string.IsNullOrEmpty(titlePatternStr)) return;

        var titleShapes = GetTextShapes(slideRoot)
            .Where(sp =>
            {
                var txBody = FindTxBody(sp);
                return txBody != null &&
                       GetAllRunText(txBody).ToLowerInvariant().Trim() == titlePatternStr;
            })
            .Select(sp => (X: GetShapeXPos(sp), Shape: sp))
            .OrderBy(t => t.X)
            .ToList();

        foreach (var (idx, key) in new[] { (0, "before_title"), (1, "after_title") })
        {
            if (GetString(content, key) is not { Length: > 0 } val) continue;
            if (idx >= titleShapes.Count) continue;
            var txBody = FindTxBody(titleShapes[idx].Shape)!;
            var match  = txBody.Descendants<A.Text>()
                               .FirstOrDefault(t => t.Text?.ToLowerInvariant().Trim() == titlePatternStr);
            if (match != null) match.Text = val;
        }
    }

    /// <summary>
    /// Replace body patterns in three_column slides by x-position ordering.
    /// </summary>
    protected virtual void ReplaceThreeColumnBodyPatterns(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        var bodyPattern = mapping.TextPatterns.GetValueOrDefault("col1", "").ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(bodyPattern)) return;

        const int BodyYThreshold = 900_000;
        var prefix = bodyPattern.Length >= 15 ? bodyPattern[..15] : bodyPattern;

        var bodyShapes = new List<(int X, OpenXmlElement TxBody)>();
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant();
            if (text.Contains("layout") || text.Contains("subtitle")) continue;
            if (!text.Contains(prefix)) continue;
            if (GetShapeYPos(sp) < BodyYThreshold) continue;
            bodyShapes.Add((GetShapeXPos(sp), txBody));
        }

        if (bodyShapes.Count < 3)
        {
            Console.Error.WriteLine($"[C2-WARN] three_column: expected 3 body shapes, found {bodyShapes.Count}. Check Y threshold and 'layout' filter.");
        }

        bodyShapes = bodyShapes.OrderBy(x => x.X).ToList();

        foreach (var (idx, key) in new[] { (0, "col1"), (1, "col2"), (2, "col3") })
        {
            if (GetString(content, key) is not { Length: > 0 } val) continue;
            if (idx >= bodyShapes.Count) continue;
            var tElems = bodyShapes[idx].TxBody.Descendants<A.Text>().ToList();
            if (tElems.Count > 0)
            {
                tElems[0].Text = val;
                for (int j = 1; j < tElems.Count; j++) tElems[j].Text = string.Empty;
            }
        }
    }

    /// <summary>
    /// Replace patterns in three_column_icons slides by x/y-position ordering.
    /// </summary>
    protected virtual void ReplaceIconColumnPatterns(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        var titlePattern   = mapping.TextPatterns.GetValueOrDefault("col1_title",   "").ToLowerInvariant().Trim();
        var contentPattern = mapping.TextPatterns.GetValueOrDefault("col1_content", "").ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(titlePattern)) return;

        var titleShapes   = new List<(int X, OpenXmlElement TxBody)>();
        var contentShapes = new List<(int X, OpenXmlElement TxBody)>();

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant().Trim();
            var y    = GetShapeYPos(sp);

            if (text == titlePattern && y > 2_000_000)
                titleShapes.Add((GetShapeXPos(sp), txBody));
            else if (!string.IsNullOrEmpty(contentPattern) && text.Contains(contentPattern))
                contentShapes.Add((GetShapeXPos(sp), txBody));
        }

        titleShapes   = titleShapes.OrderBy(x => x.X).ToList();
        contentShapes = contentShapes.OrderBy(x => x.X).ToList();

        for (int i = 0; i < Math.Min(3, titleShapes.Count); i++)
        {
            var key = $"col{i + 1}_title";
            if (GetString(content, key) is not { Length: > 0 } val) continue;
            var match = titleShapes[i].TxBody.Descendants<A.Text>()
                            .FirstOrDefault(t => t.Text?.ToLowerInvariant().Trim() == titlePattern);
            if (match != null) match.Text = val;
        }

        for (int i = 0; i < Math.Min(3, contentShapes.Count); i++)
        {
            var key = $"col{i + 1}_content";
            if (GetString(content, key) is not { Length: > 0 } val) continue;

            // Find all paragraphs that contain the content pattern; replace the first,
            // zero out runs in subsequent matching paragraphs to avoid concatenation artifacts.
            bool replaced = false;
            foreach (var para in contentShapes[i].TxBody.Descendants<A.Paragraph>())
            {
                var paraText = GetAllRunTextFromElement(para).ToLowerInvariant().Trim();
                if (!paraText.Contains(contentPattern)) continue;

                var tElems = para.Descendants<A.Text>().ToList();
                if (tElems.Count == 0) continue;

                if (!replaced)
                {
                    var matchRun = tElems.FirstOrDefault(t =>
                        t.Text?.ToLowerInvariant().Contains(contentPattern) == true)
                        ?? tElems[0];
                    matchRun.Text = val;
                    foreach (var t in tElems.Where(t => t != matchRun))
                        t.Text = string.Empty;
                    replaced = true;
                }
                else
                {
                    foreach (var t in tElems)
                        t.Text = string.Empty;
                }
            }
        }
    }

    /// <summary>
    /// Replace body patterns in four_column_numbers by x-position ordering.
    /// Text goes into the LAST &lt;a:t&gt; to preserve spacer paragraphs.
    /// </summary>
    protected virtual void ReplaceFourColumnNumberPatterns(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        var bodyPattern = mapping.TextPatterns.GetValueOrDefault("col1_content", "").ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(bodyPattern)) return;

        const int BodyYThreshold = 1_200_000;
        var prefix = bodyPattern.Length >= 15 ? bodyPattern[..15] : bodyPattern;

        var bodyShapes = new List<(int X, OpenXmlElement TxBody)>();
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody);
            var textLower = text.ToLowerInvariant().Trim();
            if (textLower.Contains("layout") || textLower.Contains("subtitle")) continue;
            // Skip big-number shapes (short strings with digits)
            if (text.Trim().Length < 10 && text.Any(char.IsDigit)) continue;
            if (!textLower.Contains(prefix)) continue;
            if (GetShapeYPos(sp) < BodyYThreshold) continue;
            bodyShapes.Add((GetShapeXPos(sp), txBody));
        }

        bodyShapes = bodyShapes.OrderBy(x => x.X).ToList();

        var colKeys = new[] { "col1_content", "col2_content", "col3_content", "col4_content" };
        for (int i = 0; i < colKeys.Length; i++)
        {
            if (GetString(content, colKeys[i]) is not { Length: > 0 } val) continue;
            if (i >= bodyShapes.Count) continue;
            var tElems = bodyShapes[i].TxBody.Descendants<A.Text>().ToList();
            if (tElems.Count > 0)
            {
                // Put replacement in the LAST <a:t> to preserve spacer paragraphs.
                for (int j = 0; j < tElems.Count - 1; j++) tElems[j].Text = string.Empty;
                tElems[^1].Text = val;
            }
        }

        // Apply uniform font size so no content description wraps to a second line.
        ApplyFourColumnNumbersContentFontSize(bodyShapes, content, colKeys);

        // Apply dynamic font size to big number shapes so long values fit on one line.
        // Uses the centralised NumberSizing thresholds for consistent sizing.
        NumberSizing.ApplyNumberSizing(slidePart, content);
    }

    /// <summary>
    /// Sets a uniform font size on all four col*_content shapes in a four_column_numbers slide
    /// so that the longest description fits on a single line. All four columns use the same
    /// size — the most constrained (longest) string drives the selection.
    ///
    /// Sizes are in hundredths of a point (OpenXml sz units: 1400 = 14pt, 900 = 9pt).
    /// </summary>
    private static void ApplyFourColumnNumbersContentFontSize(
        List<(int X, OpenXmlElement TxBody)> bodyShapes,
        Dictionary<string, object?> content,
        string[] colKeys)
    {
        // Lookup table: (maxLength inclusive, sz in 1/100pt).
        // Sliding scale: 16pt default, 14pt floor for long text.
        // Validator allows up to 90 chars (23 chars/line × 3 lines).
        (int MaxLen, int Sz)[] fontTable =
        [
            (55,  1600),  // <= 55 chars → 16pt (comfortable 3 lines at 23 chars/line)
            (69,  1500),  // 56-69 chars → 15pt (fills 3 lines at ~23 chars/line)
            (80,  1400),  // 70-80 chars → 14pt (approaching max, tighter fit)
            (int.MaxValue, 1400),  // > 80 chars → 14pt floor (never below)
        ];

        // Find the longest injected content string.
        int maxLen = 0;
        for (int i = 0; i < colKeys.Length; i++)
        {
            var val = GetString(content, colKeys[i]) ?? string.Empty;
            if (val.Length > maxLen) maxLen = val.Length;
        }

        // Look up font size.
        int sz = fontTable[^1].Sz;
        foreach (var (maxL, s) in fontTable)
        {
            if (maxLen <= maxL) { sz = s; break; }
        }

        // Apply sz to every A.RunProperties in each content shape.
        for (int i = 0; i < Math.Min(colKeys.Length, bodyShapes.Count); i++)
        {
            foreach (var rPr in bodyShapes[i].TxBody.Descendants<A.RunProperties>())
            {
                rPr.FontSize = sz;
            }
        }
    }

    /// <summary>
    /// Replace patterns in four_column_icons by x-position ordering.
    /// Content also goes into the LAST &lt;a:t&gt;.
    /// </summary>
    protected virtual void ReplaceFourColumnIconPatterns(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        var contentPattern = mapping.TextPatterns.GetValueOrDefault("col1_content", "").ToLowerInvariant().Trim();
        var titlePatterns  = Enumerable.Range(1, 4)
            .Select(i => mapping.TextPatterns.GetValueOrDefault($"col{i}_title", "").ToLowerInvariant().Trim())
            .ToArray();

        var titleShapes   = new List<(int X, OpenXmlElement TxBody)>();
        var contentShapes = new List<(int X, OpenXmlElement TxBody)>();

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant().Trim();
            if (text.Contains("layout") || text.Contains("subtitle")) continue;

            bool isTitle   = titlePatterns.Any(p => !string.IsNullOrEmpty(p) && text.StartsWith(p));
            bool isContent = !string.IsNullOrEmpty(contentPattern) && text.Contains(contentPattern);

            if (isTitle)        titleShapes.Add((GetShapeXPos(sp), txBody));
            else if (isContent) contentShapes.Add((GetShapeXPos(sp), txBody));
        }

        titleShapes   = titleShapes.OrderBy(x => x.X).ToList();
        contentShapes = contentShapes.OrderBy(x => x.X).ToList();

        for (int i = 1; i <= 4; i++)
        {
            var newTitle = GetString(content, $"col{i}_title");
            if (newTitle is { Length: > 0 } && i - 1 < titleShapes.Count)
            {
                var tElems = titleShapes[i - 1].TxBody.Descendants<A.Text>().ToList();
                if (tElems.Count > 0)
                {
                    tElems[0].Text = newTitle;
                    for (int j = 1; j < tElems.Count; j++) tElems[j].Text = string.Empty;
                }
            }

            var newContent = GetString(content, $"col{i}_content");
            if (newContent is { Length: > 0 } && i - 1 < contentShapes.Count)
            {
                var tElems = contentShapes[i - 1].TxBody.Descendants<A.Text>().ToList();
                if (tElems.Count > 0)
                {
                    for (int j = 0; j < tElems.Count - 1; j++) tElems[j].Text = string.Empty;
                    tElems[^1].Text = newContent;
                }
            }
        }
    }

    /// <summary>
    /// Inject SVG/image icons into the column icon placeholder shapes.
    ///
    /// Icon placeholder shapes are identified as p:sp elements with:
    ///   - A custGeom element (custom vector path from the template), AND
    ///   - No text content
    ///
    /// For each placeholder (sorted by x-position), we embed the corresponding
    /// col{N}_icon image file and replace the shape's spPr with a blipFill so
    /// the image is displayed within the same bounding box.
    /// </summary>
    private void InjectColumnIcons(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        int columnCount)
    {
        const string rNs2 = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Collect candidate icon placeholder shapes: p:sp with custGeom and no text.
        var candidates = new List<(int X, OpenXmlElement Shape)>();

        foreach (var sp in slideRoot.Descendants()
                     .Where(e => e.LocalName == "sp"))
        {
            bool hasCustGeom = sp.Descendants().Any(e => e.LocalName == "custGeom");
            if (!hasCustGeom) continue;

            var allText = string.Concat(sp.Descendants()
                .Where(e => e.LocalName == "t")
                .Select(e => e.InnerText ?? "")).Trim();
            if (allText.Length > 0) continue;

            candidates.Add((GetShapeXPos(sp), sp));
        }

        if (candidates.Count == 0) return;
        candidates = candidates.OrderBy(c => c.X).ToList();

        for (int i = 0; i < Math.Min(columnCount, candidates.Count); i++)
        {
            var iconKey = $"col{i + 1}_icon";
            if (GetString(content, iconKey) is not { Length: > 0 } iconRef) continue;

            var resolved = ResolveImagePath(iconRef);
            if (resolved is null) continue;

            var sp = candidates[i].Shape;

            // Read bounding box from the existing xfrm.
            long offX = 0, offY = 0, cx = 0, cy = 0;
            var xfrm = sp.Descendants().FirstOrDefault(e => e.LocalName == "xfrm");
            if (xfrm != null)
            {
                var off = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "off");
                var ext = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "ext");
                if (off != null)
                {
                    try { offX = long.Parse(off.GetAttribute("x", string.Empty).Value); } catch { }
                    try { offY = long.Parse(off.GetAttribute("y", string.Empty).Value); } catch { }
                }
                if (ext != null)
                {
                    try { cx = long.Parse(ext.GetAttribute("cx", string.Empty).Value); } catch { }
                    try { cy = long.Parse(ext.GetAttribute("cy", string.Empty).Value); } catch { }
                }
            }

            if (cx == 0 || cy == 0) continue;

            // Compute aspect-ratio preserving fit within the bounding box.
            var (imgW, imgH) = GetImageDimensions(resolved);
            double scale = Math.Min((double)cx / imgW, (double)cy / imgH);
            long fitW = (long)(imgW * scale);
            long fitH = (long)(imgH * scale);
            long fitOffX = offX + (cx - fitW) / 2;
            long fitOffY = offY + (cy - fitH) / 2;

            try
            {
                // Add image part and get relationship ID.
                var bytes       = File.ReadAllBytes(resolved);
                var contentType = GuessContentType(resolved);
                var imgPart     = slidePart.AddNewPart<ImagePart>(contentType);
                using (var stream = imgPart.GetStream(FileMode.Create, FileAccess.Write))
                    stream.Write(bytes, 0, bytes.Length);
                var rId = slidePart.GetIdOfPart(imgPart);

                // Find spPr and replace its geometry+fill.
                OpenXmlElement? spPr = sp.ChildElements.FirstOrDefault(e => e.LocalName == "spPr");
                if (spPr == null) continue;

                // Remove geometry and fill children.
                var toRemove = spPr.ChildElements
                    .Where(e => e.LocalName is "custGeom" or "prstGeom"
                                           or "solidFill" or "gradFill" or "blipFill"
                                           or "pattFill" or "noFill" or "ln")
                    .ToList();
                foreach (var rem in toRemove) rem.Remove();

                // Append: prstGeom (rect), blipFill (stretch), outline (no stroke).
                var prstGeom = new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle };
                prstGeom.Append(new A.AdjustValueList());
                spPr.AppendChild(prstGeom);

                var blip = new A.Blip();
                blip.SetAttribute(new OpenXmlAttribute("embed", rNs2, rId));
                var blipFill = new A.BlipFill();
                blipFill.Append(blip);
                blipFill.Append(new A.Stretch(new A.FillRectangle()));
                spPr.AppendChild(blipFill);

                var ln = new A.Outline { Width = 0 };
                ln.Append(new A.NoFill());
                spPr.AppendChild(ln);

                // Update the shape's transform to the fitted dimensions.
                if (xfrm != null)
                {
                    var off = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "off");
                    var ext = xfrm.ChildElements.FirstOrDefault(e => e.LocalName == "ext");
                    off?.SetAttribute(new OpenXmlAttribute("x", string.Empty, fitOffX.ToString()));
                    off?.SetAttribute(new OpenXmlAttribute("y", string.Empty, fitOffY.ToString()));
                    ext?.SetAttribute(new OpenXmlAttribute("cx", string.Empty, fitW.ToString()));
                    ext?.SetAttribute(new OpenXmlAttribute("cy", string.Empty, fitH.ToString()));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContentInjector] icon inject col{i + 1}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Replace speaker information on speaker_headshots slides.
    /// Injects n speakers, removes unfilled slots (circles + name/title shapes),
    /// and repositions remaining n shapes with even horizontal spacing.
    /// </summary>
    protected virtual void ReplaceSpeakerHeadshotsPatterns(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        if (!content.TryGetValue("speakers", out var speakersObj)
            || speakersObj is not List<object?> rawSpeakers
            || rawSpeakers.Count == 0)
            return;

        var speakers = rawSpeakers;
        int n = Math.Clamp(speakers.Count, 1, 4);

        const string rNs             = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const long   HeadshotSlideWidth = 9_144_000L; // Snowflake template — NOT the 12_192_000 constant
        const long   MarginX         = 457_200L;
        const double AspectMin       = 0.8;
        const double AspectMax       = 1.2;
        const long   NameRowY        = 3_200_000L;
        const long   NameRowMaxCY    = 400_000L;
        const long   TcRowY          = 3_600_000L;
        const long   TcRowMaxCY      = 600_000L;

        // ── Phase 1: Collect circle shapes ───────────────────────────────────────
        // Each entry: (SpElem for removal, SpPrElem for xfrm mutation, BlipElem for photo, X, CX)
        var circleShapes = new List<(OpenXmlElement SpElem, OpenXmlElement SpPrElem,
                                      OpenXmlElement BlipElem, long X, long CX)>();

        foreach (var elem in slideRoot.Descendants())
        {
            // Is this a blip element with r:embed?
            string? embedVal = null;
            try { embedVal = elem.GetAttribute("embed", rNs).Value; } catch { }
            if (string.IsNullOrEmpty(embedVal)) continue;

            // Walk up to find p:spPr (contains a:xfrm) and p:sp (the shape element)
            OpenXmlElement? spPrElem = null;
            OpenXmlElement? spElem   = null;
            long cx = 0, cy = 0, x = 0;

            var cursor = elem.Parent;
            while (cursor != null)
            {
                if (spPrElem is null && cursor.LocalName == "spPr")
                {
                    spPrElem = cursor;
                    // Read geometry from a:xfrm inside this spPr
                    foreach (var child in cursor.Elements())
                    {
                        if (child.LocalName != "xfrm") continue;
                        foreach (var sub in child.Elements())
                        {
                            if (sub.LocalName == "off")
                                try { x = long.Parse(sub.GetAttribute("x", string.Empty).Value ?? "0"); } catch { }
                            if (sub.LocalName == "ext")
                            {
                                try { cx = long.Parse(sub.GetAttribute("cx", string.Empty).Value ?? "0"); } catch { }
                                try { cy = long.Parse(sub.GetAttribute("cy", string.Empty).Value ?? "0"); } catch { }
                            }
                        }
                        break;
                    }
                }
                if (spElem is null && cursor.LocalName == "sp")
                    spElem = cursor;

                if (spPrElem != null && spElem != null) break;
                cursor = cursor.Parent;
            }

            if (spPrElem is null || spElem is null || cy == 0) continue;
            var aspect = (double)cx / cy;
            if (aspect < AspectMin || aspect > AspectMax) continue;

            // Guard: sp must be a direct child of spTree
            if (spElem.Parent?.LocalName != "spTree")
            {
                Console.Error.WriteLine($"[ContentInjector] speaker circle shape is not in spTree — skipping");
                continue;
            }

            circleShapes.Add((spElem, spPrElem, elem, x, cx));
        }

        circleShapes = circleShapes.OrderBy(t => t.X).ToList();

        if (circleShapes.Count == 0)
        {
            Console.Error.WriteLine("[ContentInjector] speaker_headshots: no circle shapes found in template");
            return;
        }
        if (circleShapes.Count < n)
        {
            Console.Error.WriteLine($"[ContentInjector] speaker_headshots: only {circleShapes.Count} circle shapes for {n} speakers");
            n = circleShapes.Count;
        }

        // ── Collect name and title/company shapes ────────────────────────────────
        var nameShapes = GetTextShapes(slideRoot)
            .Where(sp =>
            {
                var y  = (long)GetShapeYPos(sp);
                var cy = (long)GetShapeHeight(sp);
                return y >= NameRowY && cy <= NameRowMaxCY;
            })
            .Select(sp => (X: GetShapeXPos(sp), Shape: sp, TxBody: FindTxBody(sp)!))
            .Where(t => t.TxBody != null)
            .OrderBy(t => t.X)
            .ToList();

        var tcShapes = GetTextShapes(slideRoot)
            .Where(sp =>
            {
                var y  = (long)GetShapeYPos(sp);
                var cy = (long)GetShapeHeight(sp);
                return y >= TcRowY && cy <= TcRowMaxCY;
            })
            .Select(sp => (X: GetShapeXPos(sp), Shape: sp, TxBody: FindTxBody(sp)!))
            .Where(t => t.TxBody != null)
            .OrderBy(t => t.X)
            .ToList();

        // ── Phase 2: Inject filled speakers ──────────────────────────────────────
        for (int i = 0; i < n; i++)
        {
            if (speakers[i] is not Dictionary<string, object?> speaker) continue;

            // Photo
            var photoRef = GetString(speaker, "photo");
            if (!string.IsNullOrEmpty(photoRef))
            {
                var resolved = ResolveImagePath(photoRef!);
                if (resolved != null)
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(resolved);
                        var imgPart = slidePart.AddNewPart<ImagePart>(GuessContentType(resolved));
                        using (var stream = imgPart.GetStream(FileMode.Create, FileAccess.Write))
                            stream.Write(bytes, 0, bytes.Length);
                        var newRId = slidePart.GetIdOfPart(imgPart);
                        circleShapes[i].BlipElem.SetAttribute(new OpenXmlAttribute("embed", rNs, newRId));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ContentInjector] speaker photo {i}: {ex.Message}");
                    }
                }
            }

            // Name — replace only first paragraph's runs (intentional: preserve trailing empty para)
            if (i < nameShapes.Count)
            {
                var nameTxBody = nameShapes[i].TxBody;
                var namePara   = nameTxBody.Descendants<A.Paragraph>().FirstOrDefault();
                if (namePara != null)
                {
                    var nameVal  = GetString(speaker, "name") ?? string.Empty;
                    var tElems   = namePara.Descendants<A.Text>().ToList();
                    if (tElems.Count > 0)
                    {
                        tElems[0].Text = nameVal;
                        for (int j = 1; j < tElems.Count; j++) tElems[j].Text = string.Empty;
                    }
                }
            }

            // Title + company
            if (i < tcShapes.Count)
            {
                var tcTxBody   = tcShapes[i].TxBody;
                var paragraphs = tcTxBody.Descendants<A.Paragraph>().ToList();
                var titleVal   = GetString(speaker, "speaker_title") ?? GetString(speaker, "title") ?? string.Empty;
                var companyVal = GetString(speaker, "company") ?? string.Empty;

                void SetPara(int pIdx, string val)
                {
                    if (pIdx >= paragraphs.Count) return;
                    var tElems = paragraphs[pIdx].Descendants<A.Text>().ToList();
                    if (tElems.Count > 0) { tElems[0].Text = val; for (int j = 1; j < tElems.Count; j++) tElems[j].Text = string.Empty; }
                }
                SetPara(0, titleVal);
                SetPara(1, companyVal);
            }
        }

        // ── Phase 3: Remove unfilled slots ────────────────────────────────────────
        for (int i = n; i < circleShapes.Count; i++)
            circleShapes[i].SpElem.Remove();

        for (int i = n; i < nameShapes.Count; i++)
            if (nameShapes[i].Shape.Parent != null) nameShapes[i].Shape.Remove();

        for (int i = n; i < tcShapes.Count; i++)
            if (tcShapes[i].Shape.Parent != null) tcShapes[i].Shape.Remove();

        // ── Phase 4: Reposition remaining n slots ─────────────────────────────────
        long circleWidth = circleShapes[0].CX;
        long usableWidth = HeadshotSlideWidth - 2 * MarginX;
        long gap         = (usableWidth - n * circleWidth) / (n + 1);

        for (int i = 0; i < n; i++)
        {
            long newX    = MarginX + gap + i * (circleWidth + gap);
            long deltaX  = newX - circleShapes[i].X;
            if (deltaX == 0) continue;

            // Update circle xfrm via raw attribute on p:spPr > a:xfrm > a:off
            var spPr = circleShapes[i].SpPrElem;
            foreach (var child in spPr.Elements())
            {
                if (child.LocalName != "xfrm") continue;
                foreach (var sub in child.Elements())
                {
                    if (sub.LocalName != "off") continue;
                    try
                    {
                        long oldX = long.Parse(sub.GetAttribute("x", string.Empty).Value ?? "0");
                        sub.SetAttribute(new OpenXmlAttribute("x", string.Empty, (oldX + deltaX).ToString()));
                    }
                    catch { }
                    break;
                }
                break;
            }

            // Update name shape x
            if (i < nameShapes.Count)
            {
                var off = FindShapeOffset(nameShapes[i].Shape);
                if (off?.X != null) off.X = off.X.Value + deltaX;
            }

            // Update tc shape x
            if (i < tcShapes.Count)
            {
                var off = FindShapeOffset(tcShapes[i].Shape);
                if (off?.X != null) off.X = off.X.Value + deltaX;
            }
        }
    }

    /// <summary>
    /// Handle title_wave / title_particle fragmented titles by replacing
    /// subtitle and attribution before title to avoid substring collisions.
    /// </summary>
    protected virtual void HandleFragmentedTitle(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        foreach (var field in new[] { "subtitle", "attribution", "title" })
        {
            if (GetString(content, field) is not { Length: > 0 } text) continue;
            if (field == "title") text = text.ToUpperInvariant();
            var pattern = mapping.TextPatterns.GetValueOrDefault(field, "").ToLowerInvariant();
            if (!string.IsNullOrEmpty(pattern))
                ReplaceShapeTextByPattern(slideRoot, pattern, text);
        }
    }

    /// <summary>
    /// Handle title_customer_logo slide with optional split-run title.
    /// </summary>
    protected virtual void HandleCustomerLogoTitle(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        string? line1 = null;
        string? line2 = null;

        if (content.ContainsKey("title_line1") || content.ContainsKey("title_line2"))
        {
            line1 = GetString(content, "title_line1")?.ToUpperInvariant() ?? string.Empty;
            line2 = GetString(content, "title_line2")?.ToUpperInvariant();
        }
        else if (GetString(content, "title") is { Length: > 0 } rawTitle)
        {
            var upper = rawTitle.ToUpperInvariant();
            var parts = upper.Split('\n', 2);
            line1 = parts[0];
            line2 = parts.Length > 1 ? parts[1] : null;
        }

        if (line1 != null)
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("title", "").ToLowerInvariant();
            if (!string.IsNullOrEmpty(pattern))
            {
                if (!string.IsNullOrEmpty(line2))
                    ReplaceSplitRunTitle(slideRoot, pattern, line1, line2);
                else
                    ReplaceShapeTextByPattern(slideRoot, pattern, line1);
            }
        }

        foreach (var field in new[] { "subtitle", "attribution" })
        {
            if (GetString(content, field) is not { Length: > 0 } val) continue;
            var pattern = mapping.TextPatterns.GetValueOrDefault(field, "").ToLowerInvariant();
            if (!string.IsNullOrEmpty(pattern))
                ReplaceShapeTextByPattern(slideRoot, pattern, val);
        }

        // Inject customer logo image.
        var logoRef = GetString(content, "customer_logo") ?? GetString(content, "image");
        if (!string.IsNullOrEmpty(logoRef))
        {
            InjectCustomerLogo(slidePart, slideRoot, logoRef!);
            CenterCustomerLogoVertically(slideRoot);
        }
    }

    /// <summary>
    /// After InjectCustomerLogo(), vertically center the logo blip container inside
    /// the right white panel.  All geometry is read/written via raw XML attributes
    /// because the template uses OpenXmlUnknownElement nodes that the typed SDK misses.
    /// </summary>
    private static void CenterCustomerLogoVertically(OpenXmlElement slideRoot)
    {
        const string rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        const long HalfSlideWidth = 6_096_000L; // 12_192_000 / 2

        // ── 1. Locate the blip element in the right panel (same criterion as InjectCustomerLogo) ──
        OpenXmlElement? targetBlip = null;
        int targetX = 0;

        foreach (var elem in slideRoot.Descendants())
        {
            string? embedVal = null;
            try { embedVal = elem.GetAttribute("embed", rNs).Value; } catch { }
            if (string.IsNullOrEmpty(embedVal)) continue;

            // Walk up to find x
            OpenXmlElement? cursor = elem.Parent;
            int x = 0;
            while (cursor != null)
            {
                foreach (var child in cursor.Elements())
                {
                    if (child.LocalName == "xfrm")
                    {
                        foreach (var off in child.Elements())
                        {
                            if (off.LocalName == "off")
                            {
                                try { x = int.Parse(off.GetAttribute("x", string.Empty).Value); } catch { }
                                goto blipFoundX;
                            }
                        }
                    }
                }
                cursor = cursor.Parent;
            }
            blipFoundX:
            if (x > 4_500_000)
            {
                targetBlip = elem;
                targetX = x;
                break;
            }
        }

        if (targetBlip == null) return;

        // ── 2. Walk up from blip to find the xfrm that owns it ──
        OpenXmlElement? logoXfrm = null;
        {
            OpenXmlElement? cursor = targetBlip.Parent;
            while (cursor != null)
            {
                foreach (var child in cursor.Elements())
                {
                    if (child.LocalName == "xfrm")
                    {
                        logoXfrm = child;
                        goto foundLogoXfrm;
                    }
                }
                cursor = cursor.Parent;
            }
            foundLogoXfrm:;
        }

        if (logoXfrm == null) return;

        // Get logo's current CY (height)
        long logoCY = 0;
        foreach (var child in logoXfrm.Elements())
        {
            if (child.LocalName == "ext")
            {
                try { logoCY = long.Parse(child.GetAttribute("cy", string.Empty).Value); } catch { }
                break;
            }
        }

        if (logoCY <= 0) return;

        // ── 3. Find the right white panel shape ──
        // The panel is the shape (not the logo's own shape) that has:
        //   x > HalfSlideWidth, cy > 3_000_000, and is a background/fill shape (not the blip container)
        OpenXmlElement? panelXfrm = null;

        foreach (var elem in slideRoot.Descendants())
        {
            if (elem.LocalName != "xfrm") continue;

            long x = 0, cy = 0, y = 0;
            foreach (var child in elem.Elements())
            {
                if (child.LocalName == "off")
                {
                    try { x = long.Parse(child.GetAttribute("x", string.Empty).Value); } catch { }
                    try { y = long.Parse(child.GetAttribute("y", string.Empty).Value); } catch { }
                }
                else if (child.LocalName == "ext")
                {
                    try { cy = long.Parse(child.GetAttribute("cy", string.Empty).Value); } catch { }
                }
            }

            // Right-side shape with substantial height — candidate for the white panel
            if (x > HalfSlideWidth && cy > 3_000_000L && elem != logoXfrm)
            {
                // Prefer the tallest shape (the panel spans most of slide height)
                if (panelXfrm == null)
                {
                    panelXfrm = elem;
                }
                else
                {
                    long existingCY = 0;
                    foreach (var child in panelXfrm.Elements())
                    {
                        if (child.LocalName == "ext")
                        {
                            try { existingCY = long.Parse(child.GetAttribute("cy", string.Empty).Value); } catch { }
                            break;
                        }
                    }
                    if (cy > existingCY) panelXfrm = elem;
                }
            }
        }

        if (panelXfrm == null) return;

        long panelY = 0, panelCY = 0;
        foreach (var child in panelXfrm.Elements())
        {
            if (child.LocalName == "off")
                try { panelY = long.Parse(child.GetAttribute("y", string.Empty).Value); } catch { }
            else if (child.LocalName == "ext")
                try { panelCY = long.Parse(child.GetAttribute("cy", string.Empty).Value); } catch { }
        }

        if (panelCY <= 0) return;

        long centeredY = panelY + (panelCY - logoCY) / 2;

        // ── 4. Update the logo's Y offset ──
        foreach (var child in logoXfrm.Elements())
        {
            if (child.LocalName == "off")
            {
                child.SetAttribute(new OpenXmlAttribute("y", string.Empty, centeredY.ToString()));
                break;
            }
        }
    }

    /// <summary>
    /// Handle title_headshot slide: title lines, subtitle, date, name/title/company text,
    /// and circular headshot photo injection.
    /// </summary>
    protected virtual void HandleTitleHeadshotSlide(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        // Name field — must run BEFORE subtitle so the name pattern matches the
        // original name shape, not the subtitle shape (which may contain the name
        // after replacement).
        if (GetString(content, "name") is { Length: > 0 } name)
        {
            var pattern = mapping.TextPatterns.GetValueOrDefault("name", "").ToLowerInvariant();
            if (!string.IsNullOrEmpty(pattern))
                ReplaceShapeTextByPattern(slideRoot, pattern, name);
        }

        // Title lines, subtitle, date — reuse fragmented title logic.
        // HandleTitleSlideMap already added title_line1/title_line2 to the content map,
        // and date is handled there too; here we handle subtitle explicitly.
        if (GetString(content, "subtitle") is { Length: > 0 } sub)
        {
            var subPattern = mapping.TextPatterns.GetValueOrDefault("subtitle", "").ToLowerInvariant();
            if (!string.IsNullOrEmpty(subPattern))
                ReplaceShapeTextByPattern(slideRoot, subPattern, sub);
        }

        // speaker_title and company are in the same shape (two paragraphs).
        // The shape contains "Title\nCompany" — replace each paragraph separately.
        var speakerTitle = GetString(content, "speaker_title");
        var company      = GetString(content, "company");
        if (!string.IsNullOrEmpty(speakerTitle) || !string.IsNullOrEmpty(company))
        {
            var titlePattern = mapping.TextPatterns.GetValueOrDefault("speaker_title", "title").ToLowerInvariant();
            ReplaceTitleCompanyShape(slideRoot, titlePattern, speakerTitle, company);
        }

        // Headshot photo — inject into the near-square ellipse blip
        var photoRef = GetString(content, "photo");
        if (!string.IsNullOrEmpty(photoRef))
        {
            var resolved = ResolveImagePath(photoRef!);
            if (resolved != null)
                ReplaceFirstSquareBlip(slidePart, slideRoot, resolved);
        }
    }

    /// <summary>
    /// For the title_headshot slide, the speaker_title and company text live in
    /// the same shape as two separate paragraphs. Find the shape by the title
    /// placeholder pattern and set each paragraph's first run independently.
    /// </summary>
    private static void ReplaceTitleCompanyShape(
        OpenXmlElement slideRoot,
        string titlePattern,
        string? speakerTitle,
        string? company)
    {
        var lowerPat = titlePattern.ToLowerInvariant();
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var shapeText = GetAllRunText(txBody).ToLowerInvariant();
            if (!shapeText.Contains(lowerPat)) continue;

            var paragraphs = txBody.Elements<A.Paragraph>().ToList();
            if (paragraphs.Count >= 1 && !string.IsNullOrEmpty(speakerTitle))
            {
                var firstRun = paragraphs[0].Descendants<A.Text>().FirstOrDefault();
                if (firstRun != null) firstRun.Text = speakerTitle!;
            }
            if (paragraphs.Count >= 2 && !string.IsNullOrEmpty(company))
            {
                var firstRun = paragraphs[1].Descendants<A.Text>().FirstOrDefault();
                if (firstRun != null) firstRun.Text = company!;
            }
            return;
        }
    }

    /// <summary>
    /// Replace the image in the first near-square blip found in the slide
    /// (aspect ratio 0.8–1.2). Used for the title_headshot circular photo.
    /// </summary>
    private void ReplaceFirstSquareBlip(SlidePart slidePart, OpenXmlElement slideRoot, string imagePath)
    {
        const string rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        foreach (var elem in slideRoot.Descendants())
        {
            string? embedVal = null;
            try { embedVal = elem.GetAttribute("embed", rNs).Value; } catch { }
            if (string.IsNullOrEmpty(embedVal)) continue;

            // Check aspect ratio
            long cx = 0; long cy = 0;
            OpenXmlElement? cursor = elem.Parent;
            while (cursor != null)
            {
                foreach (var child in cursor.Elements())
                {
                    if (child.LocalName != "xfrm") continue;
                    foreach (var sub in child.Elements())
                    {
                        if (sub.LocalName == "ext")
                        {
                            try { cx = long.Parse(sub.GetAttribute("cx", string.Empty).Value); } catch { }
                            try { cy = long.Parse(sub.GetAttribute("cy", string.Empty).Value); } catch { }
                        }
                    }
                    goto found;
                }
                cursor = cursor.Parent;
            }
            found:
            if (cy == 0) continue;
            var aspect = (double)cx / cy;
            if (aspect < 0.8 || aspect > 1.2) continue;

            try
            {
                var bytes = File.ReadAllBytes(imagePath);
                var imgPart = slidePart.AddNewPart<ImagePart>(GuessContentType(imagePath));
                using (var stream = imgPart.GetStream(FileMode.Create, FileAccess.Write))
                    stream.Write(bytes, 0, bytes.Length);
                var newRId = slidePart.GetIdOfPart(imgPart);
                elem.SetAttribute(new OpenXmlAttribute("embed", rNs, newRId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContentInjector] headshot photo: {ex.Message}");
            }
            return; // only replace first square blip
        }
    }

    /// <summary>
    /// Place <paramref name="line1"/> in the first run and <paramref name="line2"/>
    /// in the second run of the shape that matches <paramref name="pattern"/>,
    /// preserving per-run formatting.
    /// </summary>
    private static void ReplaceSplitRunTitle(
        OpenXmlElement slideRoot,
        string pattern,
        string line1,
        string line2)
    {
        var lowerPat = pattern.ToLowerInvariant();
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var shapeText = GetAllRunText(txBody);
            if (!shapeText.ToLowerInvariant().Contains(lowerPat)) continue;

            var tElems = txBody.Descendants<A.Text>().ToList();
            if (tElems.Count >= 2)
            {
                tElems[0].Text = line1;
                tElems[1].Text = line2;
                for (int i = 2; i < tElems.Count; i++) tElems[i].Text = string.Empty;
            }
            else if (tElems.Count == 1)
            {
                tElems[0].Text = $"{line1}\n{line2}";
            }
            return;
        }
    }

    // ── Paragraph-level body replacement ────────────────────────────────────

    /// <summary>
    /// Replace a txBody's paragraphs with one paragraph per item in
    /// <paramref name="items"/>, cloning the first template paragraph's
    /// formatting for overflow items.
    /// </summary>
    protected static void ReplaceBodyWithParagraphs(
        OpenXmlElement txBody,
        List<string> items)
    {
        var paragraphs = txBody.Elements<A.Paragraph>().ToList();

        if (paragraphs.Count == 0)
        {
            var para = new A.Paragraph();
            var run  = new A.Run(new A.Text(items.Count > 0 ? items[0] : string.Empty));
            para.Append(run);
            txBody.Append(para);
            paragraphs = new List<A.Paragraph> { para };
            items = items.Skip(1).ToList();
        }

        for (int i = 0; i < items.Count; i++)
        {
            A.Paragraph para;
            if (i < paragraphs.Count)
            {
                para = paragraphs[i];
            }
            else
            {
                // Clone formatting from the first paragraph.
                para = (A.Paragraph)paragraphs[0].CloneNode(deep: true);
                var clonedPPr = para.Elements<A.ParagraphProperties>().FirstOrDefault();
                if (clonedPPr != null) clonedPPr.Level = 0;
                txBody.Append(para);
            }

            var tElems = para.Descendants<A.Text>().ToList();
            if (tElems.Count > 0)
            {
                tElems[0].Text = items[i];
                for (int j = 1; j < tElems.Count; j++) tElems[j].Text = string.Empty;
            }
            else
            {
                var run = new A.Run(new A.Text(items[i]));
                para.Append(run);
            }
        }

        // Remove excess template paragraphs.
        var all = txBody.Elements<A.Paragraph>().ToList();
        for (int i = items.Count; i < all.Count; i++)
            all[i].Remove();
    }

    /// <summary>
    /// Replaces the content of an empty <paramref name="txBody"/> with one cloned paragraph
    /// per item in <paramref name="items"/>. Level-0 items clone from <paramref name="level0Source"/>;
    /// level-1 items clone from <paramref name="level1Source"/> (falls back to level-0 if null).
    /// <paramref name="txBody"/> must be empty when this is called — no paragraphs are removed here.
    /// </summary>
    protected static void ReplaceBodyWithLeveledParagraphs(
        OpenXmlElement txBody,
        List<(string Text, int Level)> items,
        A.Paragraph level0Source,
        A.Paragraph? level1Source)
    {
        foreach (var (text, level) in items)
        {
            var source = (level == 1 && level1Source != null) ? level1Source : level0Source;
            var clone = (A.Paragraph)source.CloneNode(deep: true);

            var tElems = clone.Descendants<A.Text>().ToList();
            if (tElems.Count > 0)
            {
                tElems[0].Text = text;
                for (int j = 1; j < tElems.Count; j++) tElems[j].Text = string.Empty;
            }
            else
            {
                clone.Append(new A.Run(new A.Text(text)));
            }

            txBody.Append(clone);
        }
    }

    // ── Autofit / normAutofit ────────────────────────────────────────────────

    /// <summary>
    /// Apply normAutofit to all large text shapes on the slide, skipping shapes
    /// in SpAutofitPreserve / NoAutofitPreserve and shapes smaller than
    /// MinContentHeight EMU.
    /// </summary>
    public void ApplyAutofitToAllShapes(OpenXmlElement slideRoot, string slideType)
    {
        // Types where short content looks better with noAutofit
        var shortBulletTypes = new HashSet<string>
            { "content", "two_column", "three_column", "three_column_titled" };

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var shapeName = GetShapeName(sp);

            if (SpAutofitPreserve.Contains((slideType, shapeName))) continue;
            if (NoAutofitPreserve.Contains((slideType, shapeName))) continue;

            // Skip shapes that are too small to be content containers.
            var cy = GetShapeHeight(sp);
            if (cy > 0 && cy < MinContentHeight) continue;

            if (shortBulletTypes.Contains(slideType))
            {
                var txBody = FindTxBody(sp);
                if (txBody != null)
                {
                    int paraCount = txBody.Elements<A.Paragraph>()
                        .Count(p => p.Descendants<A.Text>().Any(t => !string.IsNullOrWhiteSpace(t.Text)));
                    if (paraCount <= 3)
                    {
                        var bodyPr = txBody.Elements<A.BodyProperties>().FirstOrDefault();
                        if (bodyPr != null)
                        {
                            bodyPr.Elements<A.NormalAutoFit>().ToList().ForEach(e => e.Remove());
                            bodyPr.Elements<A.ShapeAutoFit>().ToList().ForEach(e => e.Remove());
                            if (!bodyPr.Elements<A.NoAutoFit>().Any())
                                bodyPr.Append(new A.NoAutoFit());
                        }
                        continue;
                    }
                }
            }

            ApplyNormAutofitToShape(sp);
        }
    }

    /// <summary>
    /// Convert spAutoFit / noAutofit → normAutofit on a single shape.
    /// If normAutofit is already present it is left untouched (preserving
    /// any existing fontScale / lnSpcReduction attributes).
    /// </summary>
    protected static void ApplyNormAutofitToShape(OpenXmlElement sp)
    {
        var txBody = FindTxBody(sp);
        if (txBody is null) return;

        var bodyPr = txBody.Elements<A.BodyProperties>().FirstOrDefault();
        if (bodyPr is null) return;

        if (bodyPr.Elements<A.NormalAutoFit>().Any()) return; // already set

        bodyPr.Elements<A.ShapeAutoFit>().ToList().ForEach(e => e.Remove());
        bodyPr.Elements<A.NoAutoFit>().ToList().ForEach(e => e.Remove());
        bodyPr.Append(new A.NormalAutoFit());
    }

    /// <summary>
    /// Apply fontScale / lnSpcReduction overrides to normAutofit elements
    /// for shapes that need them (called after ApplyAutofitToAllShapes).
    /// </summary>
    public void ApplyFontScaleOverrides(OpenXmlElement slideRoot, string slideType)
    {
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var shapeName = GetShapeName(sp);
            if (!FontScaleOverrides.TryGetValue((slideType, shapeName), out var attrs)) continue;

            var txBody = FindTxBody(sp);
            if (txBody is null) continue;

            var bodyPr = txBody.Elements<A.BodyProperties>().FirstOrDefault();
            if (bodyPr is null) continue;

            var na = bodyPr.Elements<A.NormalAutoFit>().FirstOrDefault();
            if (na is null) continue;

            foreach (var (attr, val) in attrs)
            {
                if (attr == "fontScale" && int.TryParse(val, out var fs))
                    na.FontScale = fs;
                else if (attr == "lnSpcReduction" && int.TryParse(val, out var lsr))
                    na.LineSpaceReduction = lsr;
            }
        }
    }

    /// <summary>
    /// Bump run sz values that are below the minimum defined in RunSizeOverrides.
    /// </summary>
    public void EnforceRunSizes(OpenXmlElement slideRoot, string slideType)
    {
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var shapeName = GetShapeName(sp);
            if (!RunSizeOverrides.TryGetValue((slideType, shapeName), out var minSz)) continue;

            var txBody = FindTxBody(sp);
            if (txBody is null) continue;

            foreach (var rPr in txBody.Descendants<A.RunProperties>())
            {
                var szStr = rPr.GetAttribute("sz", string.Empty).Value;
                if (int.TryParse(szStr, out var sz) && sz < minSz)
                    rPr.SetAttribute(new OpenXmlAttribute("sz", string.Empty, minSz.ToString()));
            }
        }
    }

    // ── Delete unfilled placeholders ─────────────────────────────────────────

    /// <summary>
    /// Remove shapes that still contain un-replaced template placeholder text.
    /// Mirrors Python XMLSlideCloner._delete_unfilled_placeholders.
    /// </summary>
    public void DeleteUnfilledPlaceholders(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping,
        string slideType)
    {
        // thank_you slide always preserves its shapes.
        if (slideType == "thank_you") return;

        // Build set of patterns for keys that were NOT supplied.
        var patternsToCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, pattern) in mapping.TextPatterns)
        {
            if (key == "body" || key.EndsWith("_content")) continue;

            var actualKey = key;
            if (key == "left_header"  && content.ContainsKey("before_title")) actualKey = "before_title";
            else if (key == "right_header" && content.ContainsKey("after_title")) actualKey = "after_title";
            else if (key == "attribution"
                     && (content.ContainsKey("author") || content.ContainsKey("role")))
                continue; // attribution is synthesised from author + role on quote slides

            var val = GetString(content, actualKey);
            if (string.IsNullOrEmpty(val))
                patternsToCheck.Add(pattern.ToLowerInvariant());
        }

        var placeholderPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "click to add" };

        if (string.IsNullOrEmpty(GetString(content, "subtitle")))
            placeholderPatterns.Add("subtitle if needed");

        var preservedTextPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "lorem ipsum" };

        // Preserved shape name fragments (case-insensitive).
        var preservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "logo", "customer", "snowflake", "copyright", "footer" };
        if (mapping.PreserveShapes is not null)
            foreach (var s in mapping.PreserveShapes) preservedNames.Add(s.ToLowerInvariant());

        var allPatterns = patternsToCheck
            .Concat(placeholderPatterns)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Collect the spTree so we can remove children from it.
        var spTree = slideRoot.Descendants<ShapeTree>().FirstOrDefault();
        if (spTree is null) return;

        var shapesToRemove = new List<OpenXmlElement>();

        foreach (var sp in spTree.Elements<Shape>())
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;

            var shapeTextLower = GetAllRunText(txBody).ToLowerInvariant().Trim();

            // Preserve lorem-ipsum shapes (body placeholders in two_column_titled etc.)
            if (slideType == "two_column_titled" && shapeTextLower.Contains("lorem ipsum")) continue;
            if (preservedTextPatterns.Any(p => shapeTextLower.Contains(p))) continue;

            bool shouldRemove = patternsToCheck.Any(p => shapeTextLower.Contains(p))
                             || placeholderPatterns.Any(p => shapeTextLower.Contains(p));
            if (!shouldRemove) continue;

            // Check preserved shape names.
            var shapeName = GetShapeName(sp).ToLowerInvariant();
            if (preservedNames.Any(p => shapeName.Contains(p)))
                shouldRemove = false;

            if (!shouldRemove) continue;

            // If the shape has some non-pattern text, clear only the matching runs
            // instead of deleting the entire shape.
            // IMPORTANT: check against the CONCATENATED shape text, not individual runs,
            // because a single logical placeholder can be split across multiple a:t elements.
            bool hasNonPatternText = !allPatterns.Any(p => shapeTextLower.Contains(p))
                || txBody.Descendants<A.Text>()
                    .Where(t => (t.Text ?? string.Empty).Trim().Length > 0)
                    .Any(t =>
                    {
                        var text = (t.Text ?? string.Empty).Trim().ToLowerInvariant();
                        return !allPatterns.Any(p => text.Contains(p)) && !allPatterns.Any(p => p.Contains(text));
                    });

            // If the entire concatenated text matches a placeholder pattern, always delete.
            if (allPatterns.Any(p => shapeTextLower == p || shapeTextLower.Trim() == p))
                hasNonPatternText = false;

            if (hasNonPatternText)
            {
                // Clear only the runs that match unfilled patterns.
                foreach (var t in txBody.Descendants<A.Text>())
                {
                    var text = (t.Text ?? string.Empty).Trim();
                    if (text.Length > 0 && allPatterns.Any(p => text.ToLowerInvariant().Contains(p)))
                        t.Text = string.Empty;
                }
            }
            else
            {
                shapesToRemove.Add(sp);
            }
        }

        foreach (var sp in shapesToRemove)
            sp.Remove();
    }

    // ── Content formatting helpers ────────────────────────────────────────────

    /// <summary>Format a bullet list as a newline-separated string.</summary>
    public static string FormatBullets(object? bulletsObj)
    {
        if (bulletsObj is null) return string.Empty;

        var lines = new List<string>();
        var items = bulletsObj as System.Collections.IEnumerable;
        if (items is null) return bulletsObj.ToString() ?? string.Empty;

        foreach (var item in items)
        {
            if (item is string s)
            {
                lines.Add(s);
            }
            else if (item is Dictionary<string, object?> d)
            {
                var text  = d.TryGetValue("text",  out var t) ? t?.ToString() ?? string.Empty : string.Empty;
                var level = d.TryGetValue("level", out var l) && int.TryParse(l?.ToString(), out var lv) ? lv : 0;
                lines.Add(new string(' ', level * 2) + text);
            }
            else
            {
                lines.Add(item?.ToString() ?? string.Empty);
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>Format string or list content as a display string.</summary>
    public static string FormatContent(object? contentObj)
    {
        return contentObj switch
        {
            null                     => string.Empty,
            string s                 => s,
            System.Collections.IEnumerable e => FormatBullets(e),
            _                        => contentObj.ToString() ?? string.Empty
        };
    }

    // ── Shape geometry helpers ────────────────────────────────────────────────

    /// <summary>Extract the x-position from a shape's transform (EMU).</summary>
    public static int GetShapeXPos(OpenXmlElement shape)
    {
        var off = FindShapeOffset(shape);
        if (off?.X is null) return 0;
        return (int)off.X.Value;
    }

    /// <summary>Extract the y-position from a shape's transform (EMU).</summary>
    public static int GetShapeYPos(OpenXmlElement shape)
    {
        var off = FindShapeOffset(shape);
        if (off?.Y is null) return 0;
        return (int)off.Y.Value;
    }

    /// <summary>Extract the height from a shape's transform (EMU). Returns 0 if absent.</summary>
    private static int GetShapeHeight(OpenXmlElement shape)
    {
        var spPr = shape.Elements<ShapeProperties>().FirstOrDefault();
        if (spPr is null) return 0;
        var ext = spPr.Transform2D?.Extents;
        if (ext is null) return 0;
        return ext.Cy != null ? (int)(long)ext.Cy : 0;
    }

    private static A.Offset? FindShapeOffset(OpenXmlElement shape)
    {
        var spPr = shape.Elements<ShapeProperties>().FirstOrDefault();
        return spPr?.Transform2D?.Offset;
    }

    // ── Text extraction helpers ───────────────────────────────────────────────

    /// <summary>Get all shapes that carry a txBody (p:sp elements).</summary>
    public static IEnumerable<OpenXmlElement> GetTextShapes(OpenXmlElement root)
        => root.Descendants<Shape>();

    /// <summary>Find the txBody inside a shape (may be p:txBody or a:txBody).</summary>
    public static OpenXmlElement? FindTxBody(OpenXmlElement shape)
    {
        // For p:sp shapes, the txBody is a direct child TextBody.
        if (shape is Shape sp)
            return sp.TextBody;
        // Generic fallback for other element types.
        return shape.Descendants<TextBody>().FirstOrDefault()
            ?? (OpenXmlElement?)shape.Descendants<A.ListStyle>().FirstOrDefault()?.Parent;
    }

    /// <summary>Concatenate all &lt;a:t&gt; text nodes within a txBody.</summary>
    public static string GetAllRunText(OpenXmlElement txBody)
        => string.Concat(txBody.Descendants<A.Text>().Select(t => t.Text ?? string.Empty));

    /// <summary>Concatenate all &lt;a:t&gt; text nodes within any element.</summary>
    private static string GetAllRunTextFromElement(OpenXmlElement elem)
        => string.Concat(elem.Descendants<A.Text>().Select(t => t.Text ?? string.Empty));

    /// <summary>Get the shape's cNvPr name attribute (or empty string).</summary>
    public static string GetShapeName(OpenXmlElement shape)
    {
        if (shape is Shape sp)
            return sp.NonVisualShapeProperties?
                      .NonVisualDrawingProperties?
                      .Name?.Value ?? string.Empty;
        return string.Empty;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    /// <summary>Safe string accessor for a mixed-type content dictionary.</summary>
    private static string? GetString(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var val)) return null;
        return val?.ToString();
    }

    /// <summary>
    /// If <paramref name="srcKey"/> is present and <paramref name="dstKey"/> is not,
    /// copy the value so both keys exist (content normalisation alias).
    /// </summary>
    private static void TryAliasKey(
        Dictionary<string, object?> content,
        string srcKey,
        string dstKey)
    {
        if (content.ContainsKey(srcKey) && !content.ContainsKey(dstKey))
            content[dstKey] = content[srcKey];
    }

    // ── Image injection ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolve an image reference to an absolute path.
    /// 1. Absolute path that exists → return as-is.
    /// 2. Relative path from CWD → check Directory.GetCurrentDirectory().
    /// 3. Delegate to AssetCatalog.ResolveImagePath (checks asset dirs + keyword search).
    /// </summary>
    private string? ResolveImagePath(string imageRef)
    {
        if (string.IsNullOrWhiteSpace(imageRef)) return null;

        if (Path.IsPathRooted(imageRef) && File.Exists(imageRef))
            return imageRef;

        var fromCwd = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), imageRef));
        if (File.Exists(fromCwd)) return fromCwd;

        return _catalog.ResolveImagePath(imageRef);
    }

    /// <summary>
    /// Guess a MIME content type from file extension.
    /// </summary>
    private static string GuessContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            ".svg"            => "image/svg+xml",
            _                 => "image/png",
        };

    /// <summary>
    /// Inject bullet items into the content slide body shape.
    /// Finds the shape containing the lorem-ipsum body pattern and replaces its
    /// paragraphs with one paragraph per bullet item.
    /// </summary>
    protected virtual void HandleContentBullets(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        if (!content.ContainsKey("bullets")) return;

        var bodyPattern = mapping.TextPatterns.GetValueOrDefault("body", "").ToLowerInvariant();
        if (string.IsNullOrEmpty(bodyPattern)) return;

        var prefix = bodyPattern.Length >= 20 ? bodyPattern[..20] : bodyPattern;

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant();
            if (!text.Contains(prefix)) continue;

            var items = new List<string>();
            var bulletsObj = content["bullets"] as System.Collections.IEnumerable;
            if (bulletsObj != null)
            {
                foreach (var item in bulletsObj)
                {
                    if (item is string s) items.Add(s);
                    else if (item is Dictionary<string, object?> d)
                        items.Add(d.TryGetValue("text", out var t) ? t?.ToString() ?? string.Empty : string.Empty);
                    else items.Add(item?.ToString() ?? string.Empty);
                }
            }
            else
            {
                items.Add(content["bullets"]?.ToString() ?? string.Empty);
            }

            ReplaceBodyWithParagraphs(txBody, items);
            return;
        }
    }

    /// <summary>
    /// Apply pre-emptive fontScale to the content body shape based on total
    /// bullet text volume. Called after ApplyAutofitToAllShapes has set
    /// normAutofit on the shape.
    ///
    /// Lookup table (total character count across all bullets):
    ///   &lt;= 300  → no override (template default, normAutofit handles it)
    ///   301-450 → fontScale 85000, lnSpcReduction 9999
    ///   451-600 → fontScale 75000, lnSpcReduction 14999
    ///   &gt; 600   → fontScale 65000, lnSpcReduction 19999
    /// </summary>
    protected virtual void ApplyContentBulletFontScale(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping _mapping)
    {
        if (!content.ContainsKey("bullets")) return;

        // Sum character count across all bullets (mirrors HandleContentBullets parsing).
        int totalChars = 0;
        var bulletsObj = content["bullets"] as System.Collections.IEnumerable;
        if (bulletsObj != null)
        {
            foreach (var item in bulletsObj)
            {
                if (item is string s)
                    totalChars += s.Length;
                else if (item is Dictionary<string, object?> d)
                    totalChars += (d.TryGetValue("text", out var t) ? t?.ToString() ?? string.Empty : string.Empty).Length;
                else
                    totalChars += (item?.ToString() ?? string.Empty).Length;
            }
        }
        else
        {
            totalChars = (content["bullets"]?.ToString() ?? string.Empty).Length;
        }

        if (totalChars <= 300) return;

        // Lookup fontScale and lnSpcReduction.
        (int MaxChars, int FontScale, int LnSpcReduction)[] table =
        [
            (450, 85000, 9999),
            (600, 75000, 14999),
            (int.MaxValue, 65000, 19999),
        ];

        int fontScale = table[^1].FontScale;
        int lnSpcReduction = table[^1].LnSpcReduction;
        foreach (var (maxC, fs, lsr) in table)
        {
            if (totalChars <= maxC) { fontScale = fs; lnSpcReduction = lsr; break; }
        }

        // Find the body shape by its normAutofit element and paragraph count.
        // By this point HandleContentBullets has already replaced the body text,
        // so we can't match on the original pattern. The body shape is the one
        // with normAutofit and multiple text paragraphs (one per bullet).
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;

            int paraCount = txBody.Elements<A.Paragraph>()
                .Count(p => p.Descendants<A.Text>().Any(t => !string.IsNullOrWhiteSpace(t.Text)));
            if (paraCount < 2) continue;

            var bodyPr = txBody.Elements<A.BodyProperties>().FirstOrDefault();
            if (bodyPr is null) continue;

            var na = bodyPr.Elements<A.NormalAutoFit>().FirstOrDefault();
            if (na is null) continue;

            na.FontScale = fontScale;
            na.LineSpaceReduction = lnSpcReduction;
            return;
        }
    }

    /// <summary>
    /// Inject agenda items into the agenda slide right panel.
    /// Finds the shape containing the "Template Instructions" body pattern and
    /// replaces its paragraphs with spec items. Also removes the left-panel
    /// boilerplate shape ("WELCOME TO YOUR 2026 TEMPLATE").
    /// </summary>
    protected virtual void HandleAgendaItems(
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        SlideMapping mapping)
    {
        if (!content.ContainsKey("items")) return;

        var bodyPattern = mapping.TextPatterns.GetValueOrDefault("body", "").ToLowerInvariant();

        // Parse items into leveled tuples: (text, 0) for top-level, (text, 1) for sub-bullets.
        // YAML option A: a dict item has ONE key (the title text) whose value is a dict
        // containing "subitems". NormalizeValue ensures all dicts use string keys (OrdinalIgnoreCase).
        var leveledItems = new List<(string Text, int Level)>();
        var rawItems = content["items"] as System.Collections.IEnumerable;
        if (rawItems != null)
        {
            foreach (var item in rawItems)
            {
                if (item is string s)
                {
                    leveledItems.Add((s, 0));
                }
                else if (item is Dictionary<string, object?> d)
                {
                    // Support two formats:
                    // Format A (preferred): single-key dict {"Title": {subitems: [...]}}
                    // Format B (fallback):  multi-key dict  {title: "...", subitems: [...]}
                    string parentTitle;
                    Dictionary<string, object?>? innerDict = null;

                    if (d.Count == 1)
                    {
                        parentTitle = d.Keys.First();
                        if (d[parentTitle] is Dictionary<string, object?> inner)
                            innerDict = inner;
                    }
                    else if (d.TryGetValue("title", out var titleVal) && titleVal is string titleStr)
                    {
                        parentTitle = titleStr;
                        if (d.TryGetValue("subitems", out var subVal) && subVal is Dictionary<string, object?> inner)
                            innerDict = inner;
                    }
                    else
                    {
                        continue; // truly malformed
                    }

                    leveledItems.Add((parentTitle, 0));

                    if (innerDict is not null &&
                        innerDict.TryGetValue("subitems", out var subObj) &&
                        subObj is List<object?> subList)
                    {
                        foreach (var si in subList)
                            leveledItems.Add((si?.ToString() ?? string.Empty, 1));
                    }
                    else if (d.Count > 1 &&
                             d.TryGetValue("subitems", out var directSubObj) &&
                             directSubObj is List<object?> directSubList)
                    {
                        // Format B: subitems at same level as title key
                        foreach (var si in directSubList)
                            leveledItems.Add((si?.ToString() ?? string.Empty, 1));
                    }
                }
            }
        }

        if (leveledItems.Count == 0) return;

        // Find and replace the body/items shape (right panel — highest x)
        var bodyShapes = new List<(int X, OpenXmlElement TxBody)>();
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant();
            if (!string.IsNullOrEmpty(bodyPattern) && text.Contains(bodyPattern))
                bodyShapes.Add((GetShapeXPos(sp), txBody));
        }

        if (bodyShapes.Count > 0)
        {
            var rightBody = bodyShapes.OrderByDescending(b => b.X).First().TxBody;

            // Collect one deep-cloned source paragraph per distinct marL value.
            // Minimum marL → level-0 source; next marL → level-1 source.
            static int GetParaMarL(A.Paragraph p) {
                var pPr = p.Elements<A.ParagraphProperties>().FirstOrDefault();
                if (pPr == null) return 0;
                var attr = pPr.GetAttributes().FirstOrDefault(a => a.LocalName == "marL");
                return attr.Value is string v && int.TryParse(v, out int n) ? n : 0;
            }

            var allParas = rightBody.Elements<A.Paragraph>().ToList();
            var byMarL = allParas
                .GroupBy(GetParaMarL)
                .OrderBy(g => g.Key)
                .ToList();

            A.Paragraph level0Source;
            A.Paragraph? level1Source = null;

            if (byMarL.Count == 0)
            {
                // Fallback: create a bare paragraph as source
                level0Source = new A.Paragraph();
            }
            else
            {
                level0Source = (A.Paragraph)byMarL[0].First().CloneNode(deep: true);
                if (byMarL.Count >= 2)
                    level1Source = (A.Paragraph)byMarL[1].First().CloneNode(deep: true);
            }

            // Remove all original template paragraphs — txBody is now empty
            foreach (var para in allParas)
                para.Remove();

            ReplaceBodyWithLeveledParagraphs(rightBody, leveledItems, level0Source, level1Source);
        }

        // Delete the left-panel boilerplate shape
        var spTree = slideRoot.Descendants<ShapeTree>().FirstOrDefault();
        if (spTree is null) return;

        foreach (var sp in spTree.Elements<Shape>().ToList())
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var text = GetAllRunText(txBody).ToLowerInvariant();
            if (text.Contains("welcome to your") || text.Contains("welcome to the"))
                sp.Remove();
        }
    }

    /// <summary>
    /// Inject table data into table_styled / table_striped slides.
    /// Finds the a:tbl element within a graphicFrame and replaces cell text
    /// with the spec's headers and rows.
    /// </summary>
    protected virtual void HandleTableData(
        SlidePart slidePart,
        OpenXmlElement slideRoot,
        Dictionary<string, object?> content,
        string slideType = "table_striped")
    {
        if (!content.TryGetValue("table", out var tableObj) || tableObj is null) return;

        // Extract headers and rows from the spec
        List<string>? headers = null;
        List<List<string>>? rows = null;

        if (tableObj is Dictionary<string, object?> tblDict)
        {
            if (tblDict.TryGetValue("headers", out var hObj) && hObj is System.Collections.IEnumerable hList)
                headers = hList.Cast<object>().Select(x => x?.ToString() ?? string.Empty).ToList();

            if (tblDict.TryGetValue("rows", out var rObj) && rObj is System.Collections.IEnumerable rList)
            {
                rows = new List<List<string>>();
                foreach (var row in rList.Cast<object>())
                {
                    if (row is System.Collections.IEnumerable cellList)
                        rows.Add(cellList.Cast<object>().Select(x => x?.ToString() ?? string.Empty).ToList());
                }
            }
        }

        if (headers is null && rows is null) return;

        // Find the a:tbl element
        var tbl = slideRoot.Descendants<A.Table>().FirstOrDefault();
        if (tbl is null) return;

        var tblRows = tbl.Elements<A.TableRow>().ToList();

        int rowIdx = 0;

        // Row 0 = header row
        if (headers != null && tblRows.Count > 0)
        {
            SetTableRowCells(tblRows[0], headers);
            rowIdx = 1;
        }

        // Remaining rows = data rows
        if (rows != null)
        {
            for (int i = 0; i < rows.Count && rowIdx < tblRows.Count; i++, rowIdx++)
                SetTableRowCells(tblRows[rowIdx], rows[i]);
        }

        // Remove any excess template rows that weren't filled
        for (int r = tblRows.Count - 1; r >= rowIdx; r--)
            tblRows[r].Remove();

        // Remove empty columns and re-center table
        RemoveEmptyTableColumns(tbl);

        // Resize table shape to fill content area
        // Read actual slide width from the presentation rather than hardcoding
        long tableSlideWidth = 9_144_000L; // standard 4:3 fallback (720pt)
        try
        {
            var presPackage = slidePart.OpenXmlPackage;
            var presPart = presPackage.GetPartsOfType<PresentationPart>().FirstOrDefault();
            var slideSize = presPart?.Presentation?.SlideSize;
            if (slideSize?.Cx?.HasValue == true)
                tableSlideWidth = slideSize.Cx.Value;
        }
        catch { /* use fallback */ }

        // Use the existing frame X as the left margin; apply same margin on right
        var tableGraphicFrame = tbl.Ancestors<GraphicFrame>().FirstOrDefault();
        long tableMargin = tableGraphicFrame?.Transform?.Offset?.X?.Value ?? 457_200L;
        long tableContentWidth = tableSlideWidth - 2 * tableMargin;

        if (tableGraphicFrame?.Transform?.Extents != null)
        {
            tableGraphicFrame.Transform.Extents.Cx = tableContentWidth;
        }

        // Distribute column widths
        var gridCols = tbl.TableGrid?.Elements<A.GridColumn>().ToList();
        if (gridCols != null && gridCols.Count > 0)
        {
            bool isTableStyled = string.Equals(slideType, "table_styled", StringComparison.OrdinalIgnoreCase);

            if (isTableStyled && gridCols.Count > 1)
            {
                // Col 0 = 30%, remaining cols split 70% equally
                long col0Width = (long)(tableContentWidth * 0.30);
                long remainingWidth = tableContentWidth - col0Width;
                long otherColWidth = remainingWidth / (gridCols.Count - 1);
                // Add remainder to last col to avoid rounding gap
                long lastColWidth = remainingWidth - otherColWidth * (gridCols.Count - 2);
                gridCols[0].Width = col0Width;
                for (int i = 1; i < gridCols.Count - 1; i++)
                    gridCols[i].Width = otherColWidth;
                gridCols[gridCols.Count - 1].Width = lastColWidth;
            }
            else
            {
                // Equal widths for table_striped (or table_styled with 1 col)
                long colWidth = tableContentWidth / gridCols.Count;
                long lastColWidth = tableContentWidth - colWidth * (gridCols.Count - 1);
                for (int i = 0; i < gridCols.Count - 1; i++)
                    gridCols[i].Width = colWidth;
                gridCols[gridCols.Count - 1].Width = lastColWidth;
            }

            // Remove colId extLst entries — PowerPoint may use stale cached widths from these IDs
            foreach (var col in gridCols)
            {
                var extLst = col.GetFirstChild<A.ExtensionList>();
                extLst?.Remove();
            }
        }

    }

    /// <summary>
    /// Removes columns where all cells (header + data) are empty.
    /// Re-centers table by redistributing column widths evenly.
    /// </summary>
    private static void RemoveEmptyTableColumns(A.Table tbl)
    {
        var rows = tbl.Elements<A.TableRow>().ToList();
        if (rows.Count == 0) return;

        var numCols = rows[0].Elements<A.TableCell>().Count();
        if (numCols == 0) return;

        var emptyColIndices = new List<int>();
        for (int col = 0; col < numCols; col++)
        {
            bool allEmpty = true;
            foreach (var row in rows)
            {
                var cells = row.Elements<A.TableCell>().ToList();
                if (col < cells.Count)
                {
                    var text = string.Join("", cells[col].Descendants<A.Text>().Select(t => t.Text ?? "")).Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        allEmpty = false;
                        break;
                    }
                }
            }
            if (allEmpty) emptyColIndices.Add(col);
        }

        if (emptyColIndices.Count == 0) return;

        foreach (var row in rows)
        {
            var cells = row.Elements<A.TableCell>().ToList();
            for (int i = emptyColIndices.Count - 1; i >= 0; i--)
            {
                var idx = emptyColIndices[i];
                if (idx < cells.Count)
                    cells[idx].Remove();
            }
        }

        var grid = tbl.Elements<A.TableGrid>().FirstOrDefault();
        if (grid != null)
        {
            var gridCols = grid.Elements<A.GridColumn>().ToList();
            for (int i = emptyColIndices.Count - 1; i >= 0; i--)
            {
                var idx = emptyColIndices[i];
                if (idx < gridCols.Count)
                    gridCols[idx].Remove();
            }

            var remainingCols = grid.Elements<A.GridColumn>().ToList();
            if (remainingCols.Count > 0)
            {
                long totalWidth = remainingCols.Sum(c => c.Width?.Value ?? 0);
                long evenWidth = totalWidth / remainingCols.Count;
                foreach (var col in remainingCols)
                    col.Width = evenWidth;
            }
        }
    }

    /// <summary>
    /// Replace the text in all cells of a table row (a:tr) with the given values.
    /// Extra cells beyond values.Count are blanked out.
    /// </summary>
    private static void SetTableRowCells(OpenXmlElement trElem, List<string> values)
    {
        var cells = trElem.Elements<A.TableCell>().ToList();
        for (int i = 0; i < cells.Count; i++)
        {
            var tElems = cells[i].Descendants<A.Text>().ToList();
            if (tElems.Count == 0) continue;
            var text = i < values.Count ? values[i] : string.Empty;
            tElems[0].Text = text;
            for (int j = 1; j < tElems.Count; j++)
                tElems[j].Text = string.Empty;
        }
    }

    /// <summary>
    /// Inject customer logo into the title_customer_logo slide.
    /// Finds p:pic shapes with x > 4 500 000 EMU (right half), sorted by x,
    /// and replaces the first match.  Falls back to the last pic if none qualify.
    /// </summary>
    /// <summary>
    /// Inject the headshot photo for quote_photo slides.
    /// Finds near-square blip shapes (aspect 0.8–1.2), picks the rightmost one.
    /// </summary>
    private void HandleQuotePhoto(SlidePart slidePart, OpenXmlElement slideRoot,
        Dictionary<string, object?> content)
    {
        var photoRef = GetString(content, "photo");
        if (string.IsNullOrEmpty(photoRef)) return;

        var resolved = ResolveImagePath(photoRef!);
        if (resolved is null) return;

        const string rNs2 = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var candidates = new List<(int X, long Area, OpenXmlElement BlipElem)>();

        foreach (var elem in slideRoot.Descendants())
        {
            string? embedVal = null;
            try { embedVal = elem.GetAttribute("embed", rNs2).Value; } catch { }
            if (string.IsNullOrEmpty(embedVal)) continue;

            int x = 0; long cx = 0; long cy = 0;
            OpenXmlElement? cursor = elem.Parent;
            while (cursor != null)
            {
                foreach (var child in cursor.Elements())
                {
                    if (child.LocalName != "xfrm") continue;
                    foreach (var sub in child.Elements())
                    {
                        if (sub.LocalName == "off")
                            try { x = int.Parse(sub.GetAttribute("x", string.Empty).Value); } catch { }
                        if (sub.LocalName == "ext")
                        {
                            try { cx = long.Parse(sub.GetAttribute("cx", string.Empty).Value); } catch { }
                            try { cy = long.Parse(sub.GetAttribute("cy", string.Empty).Value); } catch { }
                        }
                    }
                    goto foundQP;
                }
                cursor = cursor.Parent;
            }
            foundQP:
            if (cy == 0) continue;
            var aspect = (double)cx / cy;
            if (aspect < 0.8 || aspect > 1.2) continue;
            candidates.Add((x, cx * cy, elem));
        }

        if (candidates.Count == 0) return;

        // Pick rightmost (highest x)
        var target = candidates.OrderByDescending(t => t.X).First().BlipElem;

        try
        {
            var bytes = File.ReadAllBytes(resolved);
            var imgPart = slidePart.AddNewPart<ImagePart>(GuessContentType(resolved));
            using (var stream = imgPart.GetStream(FileMode.Create, FileAccess.Write))
                stream.Write(bytes, 0, bytes.Length);
            var newRId = slidePart.GetIdOfPart(imgPart);
            target.SetAttribute(new OpenXmlAttribute("embed", rNs2, newRId));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentInjector] quote_photo: {ex.Message}");
        }
    }

    /// <summary>
    /// Reposition the attribution shape on quote_simple slides to sit just
    /// below the estimated rendered text height of the quote, eliminating
    /// dead space when short quotes leave the quote box mostly empty.
    /// Also strips the &lt;p:ph type="subTitle"/&gt; from the attribution shape so
    /// PowerPoint uses the explicit SpPr transform rather than inheriting a
    /// default placeholder position from the slide master.
    /// Runs inside HandlePositionalReplacements (before text replacement) so
    /// shapes are found by their template pattern text.
    /// </summary>
    private void HandleQuoteSimpleAttributionPosition(OpenXmlElement slideRoot, Dictionary<string, object?> content, SlideMapping mapping)
    {
        var quotePattern  = mapping.TextPatterns.GetValueOrDefault("quote", "").ToLowerInvariant();
        var attrPattern   = mapping.TextPatterns.GetValueOrDefault("attribution", "").ToLowerInvariant();
        if (string.IsNullOrEmpty(quotePattern) || string.IsNullOrEmpty(attrPattern)) return;

        OpenXmlElement? quoteBodyShape = null;
        OpenXmlElement? attrBodyShape  = null;

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            var txt = GetAllRunText(txBody).ToLowerInvariant();
            if (txt.Contains(quotePattern))
                quoteBodyShape = sp;
            else if (txt.Contains(attrPattern))
                attrBodyShape = sp;
        }

        if (quoteBodyShape == null || attrBodyShape == null) return;

        // Strip <p:ph> from attribution shape so PowerPoint uses only the explicit
        // SpPr transform rather than inheriting a default placeholder position.
        if (attrBodyShape is Shape attrSp)
        {
            var nvPr = attrSp.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties;
            var ph   = nvPr?.GetFirstChild<PlaceholderShape>();
            ph?.Remove();
        }

        var quoteXfrm = quoteBodyShape.Descendants<A.Transform2D>().FirstOrDefault()
                        ?? (quoteBodyShape as Shape)?.ShapeProperties?.Transform2D;
        var attrXfrm  = attrBodyShape.Descendants<A.Transform2D>().FirstOrDefault()
                        ?? (attrBodyShape as Shape)?.ShapeProperties?.Transform2D;

        if (quoteXfrm == null || attrXfrm == null) return;

        long quoteY  = quoteXfrm.Offset?.Y?.Value ?? 0L;
        long quoteCY = quoteXfrm.Extents?.Cy?.Value ?? 0L;
        if (quoteCY == 0L) return;

        // Estimate visual line count from the user's actual quote text.
        // The template box is ~7.98" wide at 40pt bold Arial; roughly 30 chars/line.
        var userQuoteText = content.TryGetValue("quote", out var qObj) ? qObj?.ToString() ?? "" : "";
        const int CharsPerLine   = 30;
        int       visualLines    = (int)Math.Ceiling((double)Math.Max(1, userQuoteText.Length) / CharsPerLine);

        // At 40pt with 100% line spacing, one visual line is ~660,000 EMU (~0.72").
        // tIns on the box is 45,720 EMU, so add a small top-of-box offset.
        const long LineHeightEmu     = 660_000L;  // ~0.72" per visual line at 40pt
        const long BoxTopInset       =  91_440L;  // matches tIns in template (0.1")
        const long AttributionGap    = 457_200L;  // 0.5" gap between quote text and attribution
        const long SlideBottomSafety = 6_200_000L; // stay well above footer (~6.8" slide)

        long estimatedTextBottom = quoteY + BoxTopInset + (visualLines * LineHeightEmu);
        long newAttrY = Math.Min(estimatedTextBottom + AttributionGap, SlideBottomSafety);

        if (attrXfrm.Offset != null)
            attrXfrm.Offset.Y = newAttrY;
    }

    /// <summary>
    /// Vertically centre the quote text body on quote slides by setting anchor="ctr".
    /// The template has anchor=top by default, leaving a large gap when text is short.
    /// </summary>
    private void CenterQuoteBodyAnchor(OpenXmlElement slideRoot, SlideMapping mapping)
    {
        var quotePattern = mapping.TextPatterns.GetValueOrDefault("quote", "").ToLowerInvariant();
        if (string.IsNullOrEmpty(quotePattern)) return;

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            if (!GetAllRunText(txBody).ToLowerInvariant().Contains(quotePattern)) continue;

            var bodyPr = txBody.Descendants<A.BodyProperties>().FirstOrDefault();
            if (bodyPr != null)
                bodyPr.Anchor = A.TextAnchoringTypeValues.Center;
            break;
        }
    }

    /// <summary>
    /// Set the quote text body anchor to Top on quote_photo slides so that
    /// text flows from the top of the shape rather than being vertically centred.
    /// Runs inside HandlePositionalReplacements (before text replacement) so
    /// the shape is found by its template pattern text.
    /// </summary>
    private void HandleQuotePhotoBodyAnchor(OpenXmlElement slideRoot, SlideMapping mapping)
    {
        var quotePattern = mapping.TextPatterns.GetValueOrDefault("quote", "").ToLowerInvariant();
        if (string.IsNullOrEmpty(quotePattern)) return;

        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody is null) continue;
            if (!GetAllRunText(txBody).ToLowerInvariant().Contains(quotePattern)) continue;

            var bodyPr = txBody.Descendants<A.BodyProperties>().FirstOrDefault();
            if (bodyPr != null)
                bodyPr.Anchor = A.TextAnchoringTypeValues.Top;
            break;
        }
    }

    /// <summary>
    /// Inject the main image for split slides.
    /// Finds all blip shapes by area, picks the largest one.
    /// </summary>
    private void HandleSplitImage(SlidePart slidePart, OpenXmlElement slideRoot,
        Dictionary<string, object?> content)
    {
        var imageRef = GetString(content, "image");
        if (string.IsNullOrEmpty(imageRef)) return;

        var resolved = ResolveImagePath(imageRef!);
        if (resolved is null) return;

        const string rNs2 = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var candidates = new List<(long Area, OpenXmlElement BlipElem)>();

        foreach (var elem in slideRoot.Descendants())
        {
            string? embedVal = null;
            try { embedVal = elem.GetAttribute("embed", rNs2).Value; } catch { }
            if (string.IsNullOrEmpty(embedVal)) continue;

            long cx = 0; long cy = 0;
            OpenXmlElement? cursor = elem.Parent;
            while (cursor != null)
            {
                foreach (var child in cursor.Elements())
                {
                    if (child.LocalName != "xfrm") continue;
                    foreach (var sub in child.Elements())
                    {
                        if (sub.LocalName == "ext")
                        {
                            try { cx = long.Parse(sub.GetAttribute("cx", string.Empty).Value); } catch { }
                            try { cy = long.Parse(sub.GetAttribute("cy", string.Empty).Value); } catch { }
                        }
                    }
                    goto foundSplit;
                }
                cursor = cursor.Parent;
            }
            foundSplit:
            candidates.Add((cx * cy, elem));
        }

        if (candidates.Count == 0) return;

        var target = candidates.OrderByDescending(t => t.Area).First().BlipElem;

        try
        {
            var bytes = File.ReadAllBytes(resolved);
            var imgPart = slidePart.AddNewPart<ImagePart>(GuessContentType(resolved));
            using (var stream = imgPart.GetStream(FileMode.Create, FileAccess.Write))
                stream.Write(bytes, 0, bytes.Length);
            var newRId = slidePart.GetIdOfPart(imgPart);
            target.SetAttribute(new OpenXmlAttribute("embed", rNs2, newRId));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentInjector] split image: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles split slide content_title and content fields by replacing text at the paragraph level.
    /// The template has both fields in the same shape (PlaceHolder 4) as separate paragraphs:
    /// - Paragraph 0: content_title
    /// - Paragraph 1-2: content body
    /// </summary>
    private void HandleSplitContent(OpenXmlElement slideRoot, Dictionary<string, object?> content, SlideMapping mapping)
    {
        var contentTitle = GetString(content, "content_title");
        var contentBody = GetString(content, "content");
        
        // If neither field is provided, nothing to do
        if (string.IsNullOrEmpty(contentTitle) && string.IsNullOrEmpty(contentBody))
            return;
        
        // Find the shape containing the content_title pattern
        var contentTitlePattern = mapping.TextPatterns.GetValueOrDefault("content_title");
        var contentPattern = mapping.TextPatterns.GetValueOrDefault("content");
        if (string.IsNullOrEmpty(contentTitlePattern))
            return;
        
        var lowerPattern = contentTitlePattern.ToLowerInvariant();
        
        // Look for the shape containing the content_title pattern text (same approach as ReplaceShapeTextByPattern)
        foreach (var sp in GetTextShapes(slideRoot))
        {
            var txBody = FindTxBody(sp);
            if (txBody == null) continue;
            
            var shapeText = GetAllRunText(txBody);
            if (!shapeText.ToLowerInvariant().Contains(lowerPattern)) continue;
            
            // Found the shape - now replace by paragraph
            var paragraphs = txBody.Descendants<A.Paragraph>().ToList();
            
            int textParaIndex = 0;
            foreach (var para in paragraphs)
            {
                var textElements = para.Descendants<A.Text>().ToList();
                if (textElements.Count == 0)
                    continue;
                
                // First paragraph with text: content_title
                if (textParaIndex == 0 && !string.IsNullOrEmpty(contentTitle))
                {
                    for (int i = 0; i < textElements.Count; i++)
                        textElements[i].Text = i == 0 ? contentTitle : string.Empty;
                }
                // Second paragraph with text: content body
                else if (textParaIndex == 1 && !string.IsNullOrEmpty(contentBody))
                {
                    for (int i = 0; i < textElements.Count; i++)
                        textElements[i].Text = i == 0 ? contentBody : string.Empty;
                }
                // Third+ paragraphs: clear (they were template duplicates)
                else if (textParaIndex >= 2)
                {
                    for (int i = 0; i < textElements.Count; i++)
                        textElements[i].Text = string.Empty;
                }
                textParaIndex++;
            }
            
            // Mark these patterns as handled so ReplaceTextByPattern skips them
            // Note: We need to remove from the content map, not mapping.TextPatterns
            // Actually, since BuildContentMap doesn't add these for split slides,
            // ReplaceTextByPattern won't touch them anyway. But for safety, return here.
            return;
        }
    }
    
    /// <summary>
    /// Normalizes whitespace by collapsing multiple spaces into single spaces.
    /// </summary>
    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return System.Text.RegularExpressions.Regex.Replace(input.Trim(), @"\s+", " ");
    }

    private void InjectCustomerLogo(SlidePart slidePart, OpenXmlElement slideRoot, string imageRef)
    {
        var resolved = ResolveImagePath(imageRef);
        if (resolved is null) return;

        // SDK Descendants<A.Blip>() misses blips inside p:pic (stored as OpenXmlUnknownElement).
        // Find them via raw attribute search across all descendants.
        const string rNs2 = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Collect blip elements and their x-position by walking the full element tree.
        var pics = new List<(int X, OpenXmlElement BlipElem)>();

        foreach (var elem in slideRoot.Descendants())
        {
            // Check if this element has r:embed (i.e. is a blip)
            string? embedVal = null;
            try { embedVal = elem.GetAttribute("embed", rNs2).Value; } catch { }
            if (string.IsNullOrEmpty(embedVal)) continue;

            // Walk up to find x offset
            OpenXmlElement? cursor = elem.Parent;
            int x = 0;
            while (cursor != null)
            {
                // Check for a:xfrm > a:off
                foreach (var child in cursor.Elements())
                {
                    if (child.LocalName == "xfrm")
                    {
                        foreach (var off in child.Elements())
                        {
                            if (off.LocalName == "off")
                            {
                                try { x = int.Parse(off.GetAttribute("x", string.Empty).Value); } catch { }
                                goto foundX;
                            }
                        }
                    }
                }
                cursor = cursor.Parent;
            }
            foundX:
            pics.Add((x, elem));
        }

        if (pics.Count == 0) return;

        var candidates = pics.Where(p => p.X > 4_500_000).OrderBy(p => p.X).ToList();
        var target = candidates.Count > 0 ? candidates[0] : pics[^1];

        try
        {
            var bytes = File.ReadAllBytes(resolved);
            var imgPart = slidePart.AddNewPart<ImagePart>(GuessContentType(resolved));
            using (var stream = imgPart.GetStream(FileMode.Create, FileAccess.Write))
                stream.Write(bytes, 0, bytes.Length);

            var newRId = slidePart.GetIdOfPart(imgPart);
            target.BlipElem.SetAttribute(new OpenXmlAttribute("embed", rNs2, newRId));

            // Preserve image aspect ratio within the template placeholder bounds
            AdjustShapeToImageAspectRatio(target.BlipElem, bytes, resolved);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContentInjector] customer logo: {ex.Message}");
        }
    }

    /// <summary>
    /// After replacing the image data on a blip element, scale the containing shape's
    /// cx/cy so the new image is displayed with its original aspect ratio, fitting
    /// within the original placeholder bounds.  Also re-centers the shape horizontally
    /// within the original bounding box.
    /// </summary>
    private static void AdjustShapeToImageAspectRatio(OpenXmlElement blipElem, byte[] imageBytes, string filePath)
    {
        var imgSize = GetImagePixelSize(imageBytes, filePath);
        if (imgSize is null) return;

        // Walk up from blip to find containing xfrm — check both direct children
        // and grandchildren (xfrm is typically nested inside spPr)
        OpenXmlElement? xfrm = null;
        OpenXmlElement? cursor = blipElem.Parent;
        while (cursor != null)
        {
            foreach (var ch in cursor.Elements())
            {
                if (ch.LocalName == "xfrm") { xfrm = ch; goto foundXfrm; }
                foreach (var gc in ch.Elements())
                {
                    if (gc.LocalName == "xfrm") { xfrm = gc; goto foundXfrm; }
                }
            }
            cursor = cursor.Parent;
        }
        foundXfrm:
        if (xfrm is null) return;

        // Read current cx/cy from ext, and x from off
        long origCX = 0, origCY = 0, origX = 0;
        OpenXmlElement? extElem = null, offElem = null;
        foreach (var ch in xfrm.Elements())
        {
            if (ch.LocalName == "ext")
            {
                extElem = ch;
                try { origCX = long.Parse(ch.GetAttribute("cx", string.Empty).Value); } catch { }
                try { origCY = long.Parse(ch.GetAttribute("cy", string.Empty).Value); } catch { }
            }
            else if (ch.LocalName == "off")
            {
                offElem = ch;
                try { origX = long.Parse(ch.GetAttribute("x", string.Empty).Value); } catch { }
            }
        }

        if (origCX <= 0 || origCY <= 0 || extElem is null) return;

        double imgW = imgSize.Value.Width;
        double imgH = imgSize.Value.Height;
        double scale = Math.Min((double)origCX / imgW, (double)origCY / imgH);
        long newCX = (long)(imgW * scale);
        long newCY = (long)(imgH * scale);

        extElem.SetAttribute(new OpenXmlAttribute("cx", string.Empty, newCX.ToString()));
        extElem.SetAttribute(new OpenXmlAttribute("cy", string.Empty, newCY.ToString()));

        // Re-center horizontally within original bounding box
        if (offElem is not null)
        {
            long newX = origX + (origCX - newCX) / 2;
            offElem.SetAttribute(new OpenXmlAttribute("x", string.Empty, newX.ToString()));
        }
    }

    /// <summary>
    /// Extract pixel dimensions from image bytes without external dependencies.
    /// Supports PNG, JPEG, GIF headers and SVG viewBox/width+height attributes.
    /// Returns null if the format is unrecognized or dimensions cannot be parsed.
    /// </summary>
    private static (int Width, int Height)? GetImagePixelSize(byte[] data, string filePath)
    {
        // PNG: 8-byte signature, IHDR chunk with width/height at offsets 16-23
        if (data.Length > 24 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            int w = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
            int h = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
            if (w > 0 && h > 0) return (w, h);
        }

        // JPEG: find SOF0 (0xFFC0) or SOF2 (0xFFC2) marker
        if (data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            int i = 2;
            while (i < data.Length - 9)
            {
                if (data[i] != 0xFF) { i++; continue; }
                byte marker = data[i + 1];
                if (marker == 0xC0 || marker == 0xC2)
                {
                    int h = (data[i + 5] << 8) | data[i + 6];
                    int w = (data[i + 7] << 8) | data[i + 8];
                    if (w > 0 && h > 0) return (w, h);
                }
                if (i + 3 >= data.Length) break;
                int len = (data[i + 2] << 8) | data[i + 3];
                if (len < 2) break;
                i += 2 + len;
            }
        }

        // GIF: width/height at bytes 6-9 (little-endian)
        if (data.Length > 10 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
        {
            int w = data[6] | (data[7] << 8);
            int h = data[8] | (data[9] << 8);
            if (w > 0 && h > 0) return (w, h);
        }

        // SVG: parse viewBox or width/height attributes
        if (filePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(data);
                var vbMatch = System.Text.RegularExpressions.Regex.Match(
                    text, @"viewBox\s*=\s*""[\d.]+\s+[\d.]+\s+([\d.]+)\s+([\d.]+)""");
                if (vbMatch.Success)
                {
                    var w = (int)Math.Round(double.Parse(vbMatch.Groups[1].Value,
                        System.Globalization.CultureInfo.InvariantCulture));
                    var h = (int)Math.Round(double.Parse(vbMatch.Groups[2].Value,
                        System.Globalization.CultureInfo.InvariantCulture));
                    if (w > 0 && h > 0) return (w, h);
                }
                var wMatch = System.Text.RegularExpressions.Regex.Match(text, @"<svg[^>]*\swidth\s*=\s*""([\d.]+)");
                var hMatch = System.Text.RegularExpressions.Regex.Match(text, @"<svg[^>]*\sheight\s*=\s*""([\d.]+)");
                if (wMatch.Success && hMatch.Success)
                {
                    var w = (int)Math.Round(double.Parse(wMatch.Groups[1].Value,
                        System.Globalization.CultureInfo.InvariantCulture));
                    var h = (int)Math.Round(double.Parse(hMatch.Groups[1].Value,
                        System.Globalization.CultureInfo.InvariantCulture));
                    if (w > 0 && h > 0) return (w, h);
                }
            }
            catch { /* SVG parsing best-effort */ }
        }

        return null;
    }

    /// <summary>
    /// Creates or replaces the NotesSlidePart for <paramref name="slidePart"/>
    /// with the given <paramref name="notesText"/>.
    /// </summary>
    private static void WriteNotesSlide(SlidePart slidePart, string notesText)
    {
        const string pNs = "http://schemas.openxmlformats.org/presentationml/2006/main";
        const string aNs = "http://schemas.openxmlformats.org/drawingml/2006/main";
        const string rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Create or get the notes part *before* building XML so we can add hyperlink relationships.
        var notesPart = slidePart.NotesSlidePart
                       ?? slidePart.AddNewPart<NotesSlidePart>();

        // Regex to detect URLs inside the text.
        var urlRegex = new System.Text.RegularExpressions.Regex(@"https?://[^\s<>""]+");

        // XML-escape a plain-text string.
        static string XmlEscape(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
             .Replace("\"", "&quot;").Replace("'", "&apos;");

        // Build <a:p> elements for each line of notesText.
        var paragraphs = new System.Text.StringBuilder();
        var lines = notesText.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (string.IsNullOrEmpty(line))
            {
                // Blank line → empty paragraph for visual spacing.
                paragraphs.Append($"<a:p xmlns:a=\"{aNs}\"><a:endParaRPr/></a:p>");
                continue;
            }

            paragraphs.Append($"<a:p xmlns:a=\"{aNs}\">");

            int pos = 0;
            foreach (System.Text.RegularExpressions.Match m in urlRegex.Matches(line))
            {
                // Emit plain text before this URL.
                if (m.Index > pos)
                    paragraphs.Append($"<a:r><a:t>{XmlEscape(line.Substring(pos, m.Index - pos))}</a:t></a:r>");

                // Add hyperlink relationship and emit hlinkClick run.
                if (Uri.TryCreate(m.Value, UriKind.Absolute, out var uri))
                {
                    var relId = $"rHlnk{System.Guid.NewGuid():N}".Substring(0, 16);
                    notesPart.AddHyperlinkRelationship(uri, isExternal: true, relId);
                    paragraphs.Append(
                        $"<a:r>" +
                        $"<a:rPr><a:hlinkClick xmlns:r=\"{rNs}\" r:id=\"{XmlEscape(relId)}\"/></a:rPr>" +
                        $"<a:t>{XmlEscape(m.Value)}</a:t>" +
                        $"</a:r>");
                }
                else
                {
                    // Malformed URL — emit as plain text.
                    paragraphs.Append($"<a:r><a:t>{XmlEscape(m.Value)}</a:t></a:r>");
                }

                pos = m.Index + m.Length;
            }

            // Remaining plain text after the last URL (or the whole line if no URLs).
            if (pos < line.Length)
                paragraphs.Append($"<a:r><a:t>{XmlEscape(line.Substring(pos))}</a:t></a:r>");

            paragraphs.Append("</a:p>");
        }

        var notesXml = $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <p:notes xmlns:p="{pNs}" xmlns:a="{aNs}" xmlns:r="{rNs}">
              <p:cSld>
                <p:spTree>
                  <p:nvGrpSpPr>
                    <p:cNvPr id="1" name=""/>
                    <p:cNvGrpSpPr/>
                    <p:nvPr/>
                  </p:nvGrpSpPr>
                  <p:grpSpPr>
                    <a:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/>
                      <a:chOff x="0" y="0"/><a:chExt cx="0" cy="0"/></a:xfrm>
                  </p:grpSpPr>
                  <p:sp>
                    <p:nvSpPr>
                      <p:cNvPr id="2" name="Slide Image Placeholder 1"/>
                      <p:cNvSpPr><a:spLocks noGrp="1" noRot="1" noChangeAspect="1"/></p:cNvSpPr>
                      <p:nvPr><p:ph type="sldImg"/></p:nvPr>
                    </p:nvSpPr>
                    <p:spPr/>
                  </p:sp>
                  <p:sp>
                    <p:nvSpPr>
                      <p:cNvPr id="3" name="Notes Placeholder 2"/>
                      <p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr>
                      <p:nvPr><p:ph type="body" idx="1"/></p:nvPr>
                    </p:nvSpPr>
                    <p:spPr/>
                    <p:txBody>
                      <a:bodyPr/>
                      <a:lstStyle/>
                      {paragraphs}
                    </p:txBody>
                  </p:sp>
                </p:spTree>
              </p:cSld>
            </p:notes>
            """;

        using var stream = notesPart.GetStream(FileMode.Create, FileAccess.Write);
        var bytes = System.Text.Encoding.UTF8.GetBytes(notesXml);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Get the pixel dimensions of an image file (SVG, PNG, or JPG).
    /// Returns (1, 1) on failure so callers default to square aspect ratio.
    /// </summary>
    private static (double Width, double Height) GetImageDimensions(string filePath)
    {
        try
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".svg")
            {
                var doc = XDocument.Load(filePath);
                var root = doc.Root!;

                // Try width/height attributes first
                var wAttr = root.Attribute("width")?.Value;
                var hAttr = root.Attribute("height")?.Value;
                if (wAttr != null && hAttr != null)
                {
                    // Strip units like "px", "pt" etc.
                    if (double.TryParse(wAttr.TrimEnd("abcdefghijklmnopqrstuvwxyz%".ToCharArray()),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var w)
                        && double.TryParse(hAttr.TrimEnd("abcdefghijklmnopqrstuvwxyz%".ToCharArray()),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var h)
                        && w > 0 && h > 0)
                        return (w, h);
                }

                // Fallback: viewBox
                var vb = root.Attribute("viewBox")?.Value;
                if (vb != null)
                {
                    var parts = vb.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4
                        && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var vw)
                        && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var vh)
                        && vw > 0 && vh > 0)
                        return (vw, vh);
                }
            }
            else if (ext == ".png")
            {
                var bytes = File.ReadAllBytes(filePath);
                if (bytes.Length >= 24)
                {
                    int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                    int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                    if (w > 0 && h > 0) return (w, h);
                }
            }
            else if (ext is ".jpg" or ".jpeg")
            {
                using var fs = File.OpenRead(filePath);
                // Skip to SOF0 marker
                while (fs.Position < fs.Length - 1)
                {
                    if (fs.ReadByte() != 0xFF) continue;
                    int marker = fs.ReadByte();
                    if (marker is >= 0xC0 and <= 0xC3)
                    {
                        var buf = new byte[5];
                        fs.Read(buf, 0, 5);
                        int h = (buf[1] << 8) | buf[2];
                        int w = (buf[3] << 8) | buf[4];
                        if (w > 0 && h > 0) return (w, h);
                    }
                    else if (marker != 0x00 && marker != 0xFF)
                    {
                        // Skip segment
                        int hi = fs.ReadByte(), lo = fs.ReadByte();
                        if (hi < 0 || lo < 0) break;
                        int len = (hi << 8) | lo;
                        fs.Seek(len - 2, SeekOrigin.Current);
                    }
                }
            }
        }
        catch { /* fall through */ }

        return (1, 1); // fallback: square
    }

    /// <summary>
    /// Center the title text vertically on title slides by setting anchor="ctr"
    /// on PlaceHolder 1 (the main title shape).
    /// </summary>
    private static void CenterTitleText(OpenXmlElement slideRoot)
    {
        foreach (var sp in slideRoot.Descendants<Shape>())
        {
            var cNvPr = sp.Descendants<NonVisualDrawingProperties>().FirstOrDefault();
            if (cNvPr?.Name?.Value != "PlaceHolder 1") continue;

            var bodyPr = sp.Descendants<A.BodyProperties>().FirstOrDefault();
            if (bodyPr != null)
                bodyPr.Anchor = A.TextAnchoringTypeValues.Center;
            break;
        }
    }

    /// <summary>
    /// Top-align number label text in four_column_numbers slides (PlaceHolder 2,3,4,5).
    /// This ensures labels with different amounts of text align at the top.
    /// </summary>
    private static void TopAlignNumberLabels(OpenXmlElement slideRoot)
    {
        var targetNames = new HashSet<string> { "PlaceHolder 2", "PlaceHolder 3", "PlaceHolder 4", "PlaceHolder 5" };
        foreach (var sp in slideRoot.Descendants<Shape>())
        {
            var cNvPr = sp.Descendants<NonVisualDrawingProperties>().FirstOrDefault();
            if (cNvPr?.Name?.Value == null || !targetNames.Contains(cNvPr.Name.Value)) continue;

            var bodyPr = sp.Descendants<A.BodyProperties>().FirstOrDefault();
            if (bodyPr != null)
                bodyPr.Anchor = A.TextAnchoringTypeValues.Top;
        }
    }
}
