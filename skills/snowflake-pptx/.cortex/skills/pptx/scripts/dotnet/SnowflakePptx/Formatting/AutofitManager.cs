using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DPres = DocumentFormat.OpenXml.Presentation;

namespace SnowflakePptx.Formatting;

/// <summary>
/// Ports the autofit / fontScale / run-size logic from xml_slide_cloner.py.
///
/// Three-pass strategy (matching Python):
///   1. ApplyAutofitToAllShapes  — convert spAutoFit/noAutofit → normAutofit
///      (skipping preserve-listed shapes and shapes smaller than 200 000 EMU).
///   2. ApplyFontscaleOverrides  — stamp fontScale/lnSpcReduction onto specific
///      normAutofit elements after pass 1.
///   3. EnforceRunSizes          — bump any run whose sz attribute is below a
///      per-(slideType, shapeName) minimum.
/// </summary>
public static class AutofitManager
{
    private const long MinContentHeightEmu = 200_000L;

    // ── Preserve sets ─────────────────────────────────────────────────────────

    /// <summary>
    /// Shapes that must keep spAutoFit (auto-grow box) rather than being
    /// converted to normAutofit.  Matches _SPAUTOFIT_PRESERVE in Python.
    /// </summary>
    private static readonly HashSet<(string SlideType, string ShapeName)> SpAutofitPreserve = new()
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

    /// <summary>
    /// Shapes that must keep noAutofit (decorative labels — don't auto-shrink).
    /// Matches _NOAUTOFIT_PRESERVE in Python.
    /// </summary>
    private static readonly HashSet<(string SlideType, string ShapeName)> NoAutofitPreserve = new()
    {
        ("content",           "PlaceHolder 4"),
        ("two_column",        "PlaceHolder 5"),
        ("two_column_titled", "PlaceHolder 5"),
    };

    // ── fontScale / lnSpcReduction overrides ──────────────────────────────────

    /// <summary>
    /// Attributes to set on &lt;a:normAutofit&gt; for specific shapes after pass 1.
    /// Matches _FONTSCALE_OVERRIDES in Python.
    /// </summary>
    private static readonly Dictionary<(string SlideType, string ShapeName), Dictionary<string, string>>
        FontscaleOverrides = new()
        {
            // three_column_titled: column title shapes
            { ("three_column_titled", "Google Shape;705;p67"), new() { ["fontScale"] = "92500", ["lnSpcReduction"] = "9999" } },
            { ("three_column_titled", "Google Shape;706;p67"), new() { ["fontScale"] = "92500", ["lnSpcReduction"] = "9999" } },
            { ("three_column_titled", "Google Shape;707;p67"), new() { ["fontScale"] = "92500", ["lnSpcReduction"] = "9999" } },
            // split
            { ("split",               "PlaceHolder 1"),        new() { ["lnSpcReduction"] = "9999" } },
            // quote_photo
            { ("quote_photo",         "PlaceHolder 1"),        new() { ["fontScale"] = "85000", ["lnSpcReduction"] = "9999" } },
            { ("quote_photo",         "PlaceHolder 3"),        new() { ["lnSpcReduction"] = "9999" } },
            // table_styled
            { ("table_styled",        "PlaceHolder 1"),        new() { ["lnSpcReduction"] = "9999" } },
            // title: date box (small box with noAutofit in template)
            { ("title",               "PlaceHolder 3"),        new() { ["fontScale"] = "55000", ["lnSpcReduction"] = "19999" } },
            // title_wave: attribution box
            { ("title_wave",          "PlaceHolder 2"),        new() { ["fontScale"] = "55000", ["lnSpcReduction"] = "19999" } },
            // title_customer_logo
            { ("title_customer_logo", "PlaceHolder 1"),        new() { ["fontScale"] = "85000", ["lnSpcReduction"] = "9999" } },
            { ("title_customer_logo", "PlaceHolder 2"),        new() { ["lnSpcReduction"] = "9999" } },
            // quote: quote body
            { ("quote",               "PlaceHolder 1"),        new() { ["fontScale"] = "85000", ["lnSpcReduction"] = "9999" } },
            // quote_simple: quote body (larger bounding box + longer text needs more reduction)
            { ("quote_simple",        "PlaceHolder 1"),        new() { ["fontScale"] = "60000", ["lnSpcReduction"] = "9999" } },
        };

    // ── Minimum run sz overrides ──────────────────────────────────────────────

    /// <summary>
    /// Minimum run sz (hundredths of a point) per (slideType, shapeName).
    /// Any run whose sz is below this value is bumped up.
    /// Matches _RUN_SIZE_OVERRIDES in Python.
    /// </summary>
    private static readonly Dictionary<(string SlideType, string ShapeName), int> RunSizeOverrides = new()
    {
        // Quote attributions: template bakes 1600 (16 pt)
        { ("quote",               "PlaceHolder 3"), 1800 },
        { ("quote_photo",         "PlaceHolder 3"), 1800 },
        { ("quote_simple",        "PlaceHolder 2"), 1800 },
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pass 1: converts spAutoFit / noAutofit → normAutofit on every content
    /// text shape on the slide, subject to the preserve sets and the minimum
    /// height threshold.
    /// </summary>
    public static void ApplyAutofitToAllShapes(SlidePart slidePart, string slideType)
    {
        foreach (var shape in slidePart.Slide.Descendants<DPres.Shape>())
        {
            var shapeName = GetShapeName(shape);

            if (SpAutofitPreserve.Contains((slideType, shapeName)))
                continue;
            if (NoAutofitPreserve.Contains((slideType, shapeName)))
                continue;

            if (GetShapeHeight(shape) < MinContentHeightEmu)
                continue;

            ApplyNormAutofitToShape(shape);
        }
    }

    /// <summary>
    /// Pass 2: stamps fontScale / lnSpcReduction attributes onto the
    /// &lt;a:normAutofit&gt; element for shapes listed in FontscaleOverrides.
    /// Must be called after <see cref="ApplyAutofitToAllShapes"/>.
    /// </summary>
    public static void ApplyFontscaleOverrides(SlidePart slidePart, string slideType)
    {
        foreach (var shape in slidePart.Slide.Descendants<DPres.Shape>())
        {
            var shapeName = GetShapeName(shape);

            if (!FontscaleOverrides.TryGetValue((slideType, shapeName), out var attrs))
                continue;

            var txBody = shape.Descendants<TextBody>().FirstOrDefault();
            if (txBody is null)
                continue;

            var bodyPr = txBody.GetFirstChild<BodyProperties>();
            if (bodyPr is null)
                continue;

            var normAutofit = bodyPr.GetFirstChild<NormalAutoFit>();
            if (normAutofit is null)
                continue;

            foreach (var (attrName, attrValue) in attrs)
            {
                if (attrName == "fontScale" && int.TryParse(attrValue, out var fs))
                    normAutofit.FontScale = fs;
                else if (attrName == "lnSpcReduction" && int.TryParse(attrValue, out var lsr))
                    normAutofit.LineSpaceReduction = lsr;
            }
        }
    }

    /// <summary>
    /// Pass 3: bumps any run whose sz attribute is below the minimum defined
    /// in <see cref="RunSizeOverrides"/> for the given (slideType, shapeName).
    /// </summary>
    public static void EnforceRunSizes(SlidePart slidePart, string slideType)
    {
        foreach (var shape in slidePart.Slide.Descendants<DPres.Shape>())
        {
            var shapeName = GetShapeName(shape);

            if (!RunSizeOverrides.TryGetValue((slideType, shapeName), out var minSz))
                continue;

            var txBody = shape.Descendants<TextBody>().FirstOrDefault();
            if (txBody is null)
                continue;

            foreach (var rPr in txBody.Descendants<RunProperties>())
            {
                if (rPr.FontSize.HasValue && rPr.FontSize.Value < minSz)
                    rPr.FontSize = minSz;
            }

            foreach (var endRPr in txBody.Descendants<EndParagraphRunProperties>())
            {
                if (endRPr.FontSize.HasValue && endRPr.FontSize.Value < minSz)
                    endRPr.FontSize = minSz;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the value of cNvPr/@name for the given shape,
    /// or an empty string if not present.
    /// </summary>
    public static string GetShapeName(OpenXmlElement shape)
    {
        var nvSpPr = shape.GetFirstChild<DPres.NonVisualShapeProperties>();
        var cNvPr  = nvSpPr?.GetFirstChild<DPres.NonVisualDrawingProperties>();
        return cNvPr?.Name?.Value ?? string.Empty;
    }

    /// <summary>
    /// Returns the height of the shape in EMU from spPr/xfrm/ext/@cy,
    /// or 0 if the attribute is not present.
    /// </summary>
    public static long GetShapeHeight(OpenXmlElement shape)
    {
        var spPr = shape.GetFirstChild<DPres.ShapeProperties>();
        var xfrm = spPr?.GetFirstChild<Transform2D>();
        var ext  = xfrm?.GetFirstChild<Extents>();
        return ext?.Cy?.Value ?? 0L;
    }

    // ── Internal: single-shape normAutofit conversion ─────────────────────────

    /// <summary>
    /// Converts spAutoFit or noAutofit to normAutofit on a single shape.
    /// If normAutofit already exists (possibly with fontScale set by a prior
    /// override pass) it is left untouched so its attributes are preserved.
    /// </summary>
    private static void ApplyNormAutofitToShape(DPres.Shape shape)
    {
        var txBody = shape.Descendants<TextBody>().FirstOrDefault();
        if (txBody is null)
            return;

        var bodyPr = txBody.GetFirstChild<BodyProperties>();
        if (bodyPr is null)
            return;

        // If normAutofit already present, preserve it (may already carry fontScale).
        if (bodyPr.GetFirstChild<NormalAutoFit>() is not null)
            return;

        // Remove competing autofit elements.
        bodyPr.GetFirstChild<NoAutoFit>()?.Remove();
        bodyPr.GetFirstChild<ShapeAutoFit>()?.Remove();

        bodyPr.AppendChild(new NormalAutoFit());
    }
}
