using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DPres = DocumentFormat.OpenXml.Presentation;

namespace SnowflakePptx.Formatting;

/// <summary>
/// Pre-emptive font sizing engine — ports text_fitting.py and the
/// _apply_number_sizing method from xml_slide_cloner.py.
///
/// Font sizes in PowerPoint XML are stored in hundredths of a point
/// (e.g. 5200 = 52 pt). All public methods that return sizes use
/// the “hundreth” convention to match the Python source.
/// </summary>
public static class NumberSizing
{
    private const int MinFontSizePt      = 12;
    private const int MinFontSizeContent = 14;

    // ── Threshold tables ─────────────────────────────────────────────────────
    // Each row: (maxChars, fontSizePt).
    // The first row whose maxChars >= textLength wins.
    // Ordered from smallest to largest character count.

    /// <summary>Four-column big-number sizing (digit count → pt).</summary>
    public static readonly (int MaxChars, int SizePt)[] NumberThresholds =
    {
        (4, 36), (5, 28), (6, 24), (8, 18), (999, 14),
    };

    /// <summary>Split-slide title sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] SplitTitleThresholds =
    {
        (25, 26), (40, 24), (55, 22), (70, 20), (999, 18),
    };

    /// <summary>Split-slide subtitle sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] SplitSubtitleThresholds =
    {
        (40, 18), (55, 16), (70, 14), (999, 14),
    };

    /// <summary>Split-slide body sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] SplitBodyThresholds =
    {
        (200, 18), (350, 16), (999, 14),
    };

    /// <summary>Quote body sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] QuoteThresholds =
    {
        (100, 44), (150, 40), (200, 36), (280, 32), (350, 28), (999, 24),
    };

    /// <summary>Content-slide bullet sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] ContentBulletThresholds =
    {
        (60, 18), (120, 16), (200, 16), (350, 14), (500, 14), (9999, 14),
    };

    /// <summary>Multi-column title sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] ColumnTitleThresholds =
    {
        (15, 18), (25, 16), (35, 14), (999, 14),
    };

    /// <summary>Multi-column body sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] ColumnContentThresholds =
    {
        (150, 18), (300, 16), (999, 14),
    };

    /// <summary>Comparison-slide title sizing.</summary>
    public static readonly (int MaxChars, int SizePt)[] ComparisonTitleThresholds =
    {
        (20, 18), (30, 16), (40, 14), (999, 14),
    };

    // ── Element type registry ─────────────────────────────────────────────────

    private static readonly Dictionary<string, ((int MaxChars, int SizePt)[] Thresholds, int MinFontPt)>
        ElementTypeConfig = new(StringComparer.OrdinalIgnoreCase)
        {
            ["split_title"]      = (SplitTitleThresholds,      MinFontSizePt),
            ["split_subtitle"]   = (SplitSubtitleThresholds,   MinFontSizePt),
            ["split_body"]       = (SplitBodyThresholds,       MinFontSizePt),
            ["number"]           = (NumberThresholds,          MinFontSizePt),
            ["quote"]            = (QuoteThresholds,           MinFontSizePt),
            ["content_bullet"]   = (ContentBulletThresholds,   MinFontSizeContent),
            ["column_title"]     = (ColumnTitleThresholds,     MinFontSizePt),
            ["column_content"]   = (ColumnContentThresholds,   MinFontSizeContent),
            ["comparison_title"] = (ComparisonTitleThresholds, MinFontSizePt),
        };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the appropriate font size for <paramref name="text"/> given
    /// <paramref name="elementType"/> and an optional <paramref name="baseSizeHundredths"/>.
    ///
    /// Returns the size in hundredths of a point (e.g. 2200 = 22 pt).
    /// Falls back to 1800 (18 pt) for unknown element types.
    /// </summary>
    public static int CalculateFontSizeHundredths(
        string text,
        string elementType,
        int?   baseSizeHundredths = null)
    {
        var basePt = baseSizeHundredths.HasValue ? baseSizeHundredths.Value / 100 : (int?)null;
        return CalculateFontSizePt(text, elementType, basePt) * 100;
    }

    /// <summary>
    /// Calculates minimum font size across multiple texts for the same element type,
    /// ensuring visual symmetry across columns. Returns the size in hundredths of a point.
    /// </summary>
    public static int CalculateSymmetricFontSizeHundredths(
        IEnumerable<string> texts,
        string              elementType,
        int?                baseSizeHundredths = null)
    {
        var textList = texts.Where(t => !string.IsNullOrEmpty(t)).ToList();
        if (textList.Count == 0)
            return (baseSizeHundredths.HasValue ? baseSizeHundredths.Value / 100 : 18) * 100;

        var basePt = baseSizeHundredths.HasValue ? baseSizeHundredths.Value / 100 : (int?)null;
        var minPt  = textList.Select(t => CalculateFontSizePt(t, elementType, basePt)).Min();
        return minPt * 100;
    }

    /// <summary>
    /// Applies symmetric number font sizing to a four_column_numbers slide.
    ///
    /// Reads col1_number – col4_number from <paramref name="content"/>, determines
    /// the minimum size across all columns, then stamps that size onto every
    /// run/defRPr/endParaRPr inside each number shape by text matching.
    /// </summary>
    public static void ApplyNumberSizing(
        SlidePart                   slidePart,
        Dictionary<string, object?> content)
    {
        var numberTexts = new List<string>();
        for (var i = 1; i <= 4; i++)
        {
            if (content.TryGetValue($"col{i}_number", out var val) && val is not null)
            {
                var s = val.ToString();
                if (!string.IsNullOrEmpty(s))
                    numberTexts.Add(s);
            }
        }

        if (numberTexts.Count == 0)
            return;

        // Symmetric: use minimum size across all columns so they all match visually.
        var minSizeHundredths = numberTexts
            .Select(t => CalculateFontSizeHundredths(t, "number"))
            .Min();

        foreach (var numText in numberTexts)
            UpdateShapeFontSize(slidePart, numText, minSizeHundredths);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static int CalculateFontSizePt(string? text, string elementType, int? baseSizePt)
    {
        var textLen = text?.Length ?? 0;

        if (!ElementTypeConfig.TryGetValue(elementType, out var config))
            return baseSizePt ?? 18;

        var (thresholds, minFont) = config;

        foreach (var (maxChars, sizePt) in thresholds)
        {
            if (textLen <= maxChars)
            {
                if (baseSizePt.HasValue && baseSizePt.Value < sizePt)
                    return Math.Max(minFont, baseSizePt.Value);
                return Math.Max(minFont, sizePt);
            }
        }

        // Fallback: last row's size (text longer than all thresholds).
        return Math.Max(minFont, thresholds[^1].SizePt);
    }

    private static void UpdateShapeFontSize(
        SlidePart slidePart,
        string    matchFragment,
        int       newSizeHundredths)
    {
        var needle = (matchFragment.Length > 50 ? matchFragment[..50] : matchFragment)
                     .ToLowerInvariant();
        if (string.IsNullOrEmpty(needle))
            return;

        foreach (var shape in slidePart.Slide.Descendants<DPres.Shape>())
        {
            var shapeText = GetShapeText(shape).ToLowerInvariant();
            if (!shapeText.Contains(needle))
                continue;

            var txBody = shape.Descendants<TextBody>().FirstOrDefault();
            if (txBody is null)
                continue;

            foreach (var rPr in txBody.Descendants<RunProperties>())
                rPr.FontSize = newSizeHundredths;

            foreach (var defRPr in txBody.Descendants<DefaultRunProperties>())
                defRPr.FontSize = newSizeHundredths;

            foreach (var endRPr in txBody.Descendants<EndParagraphRunProperties>())
                endRPr.FontSize = newSizeHundredths;

            break; // only the first matching shape
        }
    }

    private static string GetShapeText(DPres.Shape shape)
    {
        var txBody = shape.Descendants<TextBody>().FirstOrDefault();
        if (txBody is null)
            return string.Empty;

        return string.Concat(txBody.Descendants<Text>().Select(t => t.Text ?? string.Empty));
    }
}
