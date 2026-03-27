using DocumentFormat.OpenXml.Packaging;
using SnowflakePptx.Schema;
using Xunit;

using PShape    = DocumentFormat.OpenXml.Presentation.Shape;
using SlideId   = DocumentFormat.OpenXml.Presentation.SlideId;
using DParagraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using DRun       = DocumentFormat.OpenXml.Drawing.Run;

namespace SnowflakePptx.Tests;

/// <summary>
/// Verifies that every slide type's SlideIndex in TemplateMappings
/// actually points to a template slide whose text matches the expected TextPatterns.
/// Catches mapping drift when slides are reordered in the template PPTX.
/// </summary>
public class TemplateMappingTests
{
    /// <summary>
    /// Resolve the template PPTX path from environment variable or conventional location.
    /// </summary>
    private static string GetTemplatePath()
    {
        var envPath = Environment.GetEnvironmentVariable("SNOWFLAKE_TEMPLATE_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // Walk up from test assembly to find the scripts directory
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "scripts", "snowflake", "templates",
                "SNOWFLAKE TEMPLATE JANUARY 2026.pptx");
            if (File.Exists(candidate)) return candidate;

            // Also try relative to .cortex layout
            candidate = Path.Combine(dir, ".cortex", "skills", "pptx", "scripts",
                "snowflake", "templates", "SNOWFLAKE TEMPLATE JANUARY 2026.pptx");
            if (File.Exists(candidate)) return candidate;

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        // Hardcoded fallback for typical repo layout
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "scripts", "snowflake", "templates",
            "SNOWFLAKE TEMPLATE JANUARY 2026.pptx");
    }

    /// <summary>
    /// Extract all text from a slide part, concatenated.
    /// </summary>
    private static string ExtractSlideText(SlidePart slidePart)
    {
        var texts = new List<string>();
        foreach (var shape in slidePart.Slide.Descendants<PShape>())
        {
            var textBody = shape.TextBody;
            if (textBody == null) continue;

            foreach (var para in textBody.Descendants<DParagraph>())
            {
                var paraText = string.Join("",
                    para.Descendants<DRun>().Select(r => r.Text?.Text ?? ""));
                if (!string.IsNullOrWhiteSpace(paraText))
                    texts.Add(paraText);
            }
        }
        return string.Join(" ", texts);
    }

    [Fact]
    public void AllMappings_SlideIndex_MatchesTemplateContent()
    {
        var templatePath = GetTemplatePath();
        if (!File.Exists(templatePath))
        {
            // Skip test if template not available (e.g., CI without template)
            return;
        }

        using var doc = PresentationDocument.Open(templatePath, isEditable: false);
        var presPart = doc.PresentationPart!;
        var slideIds = presPart.Presentation.SlideIdList!
            .Elements<SlideId>().ToList();

        var failures = new List<string>();

        foreach (var (typeName, mapping) in TemplateMappings.Mappings)
        {
            if (mapping.SlideIndex < 0 || mapping.SlideIndex >= slideIds.Count)
            {
                failures.Add(
                    $"'{typeName}': SlideIndex {mapping.SlideIndex} is out of range [0, {slideIds.Count})");
                continue;
            }

            var rId = slideIds[mapping.SlideIndex].RelationshipId!.Value!;
            var slidePart = (SlidePart)presPart.GetPartById(rId);
            var slideText = ExtractSlideText(slidePart);

            // Check that at least one TextPattern value appears in the slide text
            var anyMatch = mapping.TextPatterns.Values
                .Where(v => !string.IsNullOrEmpty(v))
                .Any(pattern => slideText.Contains(pattern, StringComparison.OrdinalIgnoreCase));

            if (!anyMatch)
            {
                var samplePatterns = string.Join(", ",
                    mapping.TextPatterns.Values
                        .Where(v => !string.IsNullOrEmpty(v))
                        .Take(3)
                        .Select(v => $"\"{(v.Length > 40 ? v[..40] + "..." : v)}\""));
                var slidePreview = slideText.Length > 100 ? slideText[..100] + "..." : slideText;

                failures.Add(
                    $"'{typeName}': SlideIndex {mapping.SlideIndex} — " +
                    $"none of [{samplePatterns}] found in slide text: \"{slidePreview}\"");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail(
                $"Template mapping mismatches ({failures.Count}):\n" +
                string.Join("\n", failures.Select(f => $"  - {f}")));
        }
    }

    [Fact]
    public void AllMappings_SlideIndex_WithinTemplateRange()
    {
        var templatePath = GetTemplatePath();
        if (!File.Exists(templatePath))
            return;

        using var doc = PresentationDocument.Open(templatePath, isEditable: false);
        var slideCount = doc.PresentationPart!.Presentation.SlideIdList!
            .Elements<SlideId>().Count();

        foreach (var (typeName, mapping) in TemplateMappings.Mappings)
        {
            Assert.True(
                mapping.SlideIndex >= 0 && mapping.SlideIndex < slideCount,
                $"'{typeName}' has SlideIndex {mapping.SlideIndex} but template has only {slideCount} slides (0-{slideCount - 1})");
        }
    }
}
