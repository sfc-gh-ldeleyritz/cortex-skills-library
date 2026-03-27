using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using SnowflakePptx.Schema;

namespace SnowflakePptx.Core;

// ── BuildResult ────────────────────────────────────────────────────────────────

/// <summary>Result returned by <see cref="PresentationBuilder.Build"/>.</summary>
public sealed class BuildResult
{
    /// <summary>Raw bytes of the built PPTX file, or null if the build failed.</summary>
    public byte[]? PptxBytes { get; set; }

    /// <summary>Number of slides successfully added to the output.</summary>
    public int SlideCount { get; set; }

    /// <summary>Fatal errors that prevented the build from completing.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>Non-fatal warnings (e.g. individual slides that were skipped).</summary>
    public List<string> Warnings { get; } = new();

    /// <summary>0-based indices of slides that were skipped due to errors.</summary>
    public List<int> SkippedSlides { get; } = new();

    /// <summary>Wall-clock build time in milliseconds.</summary>
    public double BuildTimeMs { get; set; }

    /// <summary>True when the build completed with no fatal errors.</summary>
    public bool Success => PptxBytes != null && Errors.Count == 0;

    internal void AddError(string msg)   { Errors.Add(msg);   Debug.WriteLine($"[Builder ERROR] {msg}"); }
    internal void AddWarning(string msg) { Warnings.Add(msg); Debug.WriteLine($"[Builder WARN]  {msg}"); }
}

// ── PresentationBuilder ────────────────────────────────────────────────────────

/// <summary>
/// Main orchestrator: validates spec → loads template → strips slides →
/// clones per spec → injects content → appends thank_you → sets metadata
/// → returns serialised bytes.
///
/// C# port of Python PPTXBuilder.
/// </summary>
public class PresentationBuilder
{
    // ── Dependencies ───────────────────────────────────────────────────────────

    private readonly string? _templatePath;
    private readonly SlideCloner _cloner;
    private readonly ContentInjector _injector;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Create a builder that resolves the template automatically.
    /// </summary>
    public PresentationBuilder()
        : this(null, new SlideCloner(), new ContentInjector()) { }

    /// <summary>
    /// Create a builder with an explicit template file path.
    /// </summary>
    public PresentationBuilder(string? templatePath)
        : this(templatePath, new SlideCloner(), new ContentInjector()) { }

    /// <summary>
    /// Full constructor — allows injecting custom cloner and injector (for testing).
    /// </summary>
    public PresentationBuilder(
        string? templatePath,
        SlideCloner cloner,
        ContentInjector injector)
    {
        _templatePath = templatePath ?? ResolveDefaultTemplatePath();
        _cloner       = cloner;
        _injector     = injector;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Build a presentation from the given spec.
    ///
    /// Per-slide error recovery: if one slide fails it is skipped and the
    /// build continues. The failed slide index is recorded in
    /// <see cref="BuildResult.SkippedSlides"/>.
    /// </summary>
    /// <summary>
    /// Convenience overload: build and immediately save to <paramref name="outputPath"/>.
    /// Matches the two-argument call in Program.cs.
    /// </summary>
    public BuildResult Build(PresentationSpec spec, string outputPath)
    {
        var result = Build(spec);
        if (result.Success)
            SaveToFile(result, outputPath);
        return result;
    }

    public BuildResult Build(PresentationSpec spec)
    {
        var sw     = Stopwatch.StartNew();
        var result = new BuildResult();

        try
        {
            // ── 1. Resolve and load template ───────────────────────────────────
            var templatePath = _templatePath ?? ResolveDefaultTemplatePath();
            if (templatePath is null || !File.Exists(templatePath))
            {
                result.AddError(
                    "Template file not found. Set SNOWFLAKE_TEMPLATE_PATH or " +
                    "place a .pptx in a templates/ folder next to the assembly.");
                return result;
            }

            var templateBytes = File.ReadAllBytes(templatePath);

            // ── 2. Open template as clone source ─────────────────────────────
            using var templateStream = new MemoryStream(templateBytes, writable: false);
            using var templateDoc    = PresentationDocument.Open(
                templateStream, isEditable: false);

            // ── 3. Target document: fresh copy of template, then strip all slides ──
            var targetStream = new MemoryStream();
            targetStream.Write(templateBytes, 0, templateBytes.Length);
            targetStream.Position = 0;

            using var targetDoc = PresentationDocument.Open(targetStream, isEditable: true);
            RemoveAllSlides(targetDoc);

            // ── 4. Build each slide ──────────────────────────────────────────
            float spacing = spec.TitleSubtitleSpacing;

            foreach (var (slideSpec, index) in spec.Slides.Select((s, i) => (s, i)))
            {
                var slideType = slideSpec.Type;

                // thank_you is auto-appended at the end; skip any mid-deck occurrences.
                if (string.Equals(slideType, "thank_you", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var mapping     = TemplateMappings.Get(slideType)
                                   ?? TemplateMappings.Get("content")!;
                    var templateIdx = mapping.SlideIndex;

                    var newSlidePart = _cloner.CloneSlide(
                        templateDoc, targetDoc, templateIdx);

                    var contentDict = BuildContentDict(slideSpec);
                    _injector.InjectContent(
                        newSlidePart, contentDict, slideType, mapping, spacing);

                    result.SlideCount++;
                }
                catch (Exception ex)
                {
                    result.AddWarning(
                        $"Skipped slide {index + 1} (type={slideType}): {ex.Message}");
                    result.SkippedSlides.Add(index);
                }
            }

            // ── 5. Append thank_you slide ────────────────────────────────────
            try
            {
                var tyMapping   = TemplateMappings.Get("thank_you")!;
                var tySlidePart = _cloner.CloneSlide(
                    templateDoc, targetDoc, tyMapping.SlideIndex);
                // Empty content dict preserves the template text on thank_you.
                _injector.InjectContent(
                    tySlidePart,
                    new Dictionary<string, object?>(),
                    "thank_you",
                    tyMapping,
                    spacing);
                result.SlideCount++;
            }
            catch (Exception ex)
            {
                result.AddWarning($"Failed to add thank_you slide: {ex.Message}");
            }

            // ── 6. Core properties ─────────────────────────────────────────────
            SetCoreProperties(targetDoc, spec);

            // ── 8. Save to bytes ─────────────────────────────────────────────
            targetDoc.Save();
            result.PptxBytes = targetStream.ToArray();
        }
        catch (Exception ex)
        {
            result.AddError($"Build failed: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            result.BuildTimeMs = sw.Elapsed.TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// Save the bytes from a successful build result to a file on disk.
    /// Creates parent directories as needed.
    /// </summary>
    public static void SaveToFile(BuildResult result, string outputPath)
    {
        if (!result.Success || result.PptxBytes is null)
            throw new InvalidOperationException("Cannot save: build was not successful.");

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(outputPath, result.PptxBytes);
    }

    // ── Slide stripping ──────────────────────────────────────────────────────────

    /// <summary>
    /// Remove all existing slides from the target document while preserving
    /// slide masters, layouts, and the theme.
    /// </summary>
    private static void RemoveAllSlides(PresentationDocument doc)
    {
        var presPart = doc.PresentationPart
            ?? throw new InvalidOperationException(
                "No PresentationPart in target document.");

        var slideIdList = presPart.Presentation.SlideIdList;
        if (slideIdList is null) return;

        // Snapshot all (sid, rId) pairs before mutating the collection.
        var pairs = slideIdList
            .Elements<SlideId>()
            .Select(sid => (sid, rId: sid.RelationshipId?.Value))
            .ToList();

        foreach (var (sid, rId) in pairs)
        {
            sid.Remove();
            if (rId != null)
            {
                try { presPart.DeletePart(rId); }
                catch { /* part may already be absent */ }
            }
        }

        presPart.Presentation.Save();
    }

    // ── Core properties ──────────────────────────────────────────────────────────

    private static void SetCoreProperties(PresentationDocument doc, PresentationSpec spec)
    {
        var core = doc.CoreFilePropertiesPart
                ?? doc.AddCoreFilePropertiesPart();

        using var writer = new System.Xml.XmlTextWriter(
            core.GetStream(FileMode.Create, FileAccess.Write),
            System.Text.Encoding.UTF8);

        var now    = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var title  = spec.Presentation.Title  ?? string.Empty;
        var author = spec.Presentation.Author ?? string.Empty;

        const string cpNs      = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
        const string dcNs      = "http://purl.org/dc/elements/1.1/";
        const string dctermsNs = "http://purl.org/dc/terms/";
        const string xsiNs     = "http://www.w3.org/2001/XMLSchema-instance";

        writer.WriteStartDocument(standalone: true);
        writer.WriteStartElement("cp", "coreProperties", cpNs);
        writer.WriteAttributeString("xmlns", "dc",      null, dcNs);
        writer.WriteAttributeString("xmlns", "dcterms", null, dctermsNs);
        writer.WriteAttributeString("xmlns", "xsi",     null, xsiNs);

        if (!string.IsNullOrEmpty(title))
        {
            writer.WriteStartElement("dc", "title", dcNs);
            writer.WriteString(title);
            writer.WriteEndElement();
        }
        if (!string.IsNullOrEmpty(author))
        {
            writer.WriteStartElement("dc", "creator", dcNs);
            writer.WriteString(author);
            writer.WriteEndElement();
        }

        writer.WriteStartElement("dcterms", "created", dctermsNs);
        writer.WriteAttributeString("xsi", "type", xsiNs, "dcterms:W3CDTF");
        writer.WriteString(now);
        writer.WriteEndElement();

        writer.WriteStartElement("dcterms", "modified", dctermsNs);
        writer.WriteAttributeString("xsi", "type", xsiNs, "dcterms:W3CDTF");
        writer.WriteString(now);
        writer.WriteEndElement();

        writer.WriteEndElement(); // coreProperties
        writer.WriteEndDocument();
    }

    // ── Content dict helper ─────────────────────────────────────────────────────

    /// <summary>
    /// Convert a SlideSpec to a mutable Dictionary the injector expects.
    /// A mutable copy is required because positional handlers add alias keys.
    /// </summary>
    private static Dictionary<string, object?> BuildContentDict(SlideSpec spec) =>
        new(spec.Content, StringComparer.OrdinalIgnoreCase);

    // ── Template path resolution ─────────────────────────────────────────────────

    /// <summary>
    /// Resolve the template path through multiple strategies:
    ///   1. SNOWFLAKE_TEMPLATE_PATH environment variable (exact path).
    ///   2. templates/*.pptx relative to the executing assembly directory.
    ///   3. ../../templates/*.pptx (development / source-tree layout).
    ///   4. templates/*.pptx relative to the current working directory.
    /// Returns null if no file is found.
    /// </summary>
    public static string? ResolveDefaultTemplatePath()
    {
        // 1. Environment variable override.
        var envPath = Environment.GetEnvironmentVariable("SNOWFLAKE_TEMPLATE_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        // 2. Assembly directory / templates/
        var assemblyDir = Path.GetDirectoryName(
                              Assembly.GetExecutingAssembly().Location)
                          ?? Directory.GetCurrentDirectory();

        var candidate = FindPptxInDir(Path.Combine(assemblyDir, "templates"));
        if (candidate != null) return candidate;

        // 3. Two levels up (development layout: bin/Debug/net9.0 → project root)
        var devRoot = Path.GetFullPath(
            Path.Combine(assemblyDir, "..", "..", "templates"));
        candidate = FindPptxInDir(devRoot);
        if (candidate != null) return candidate;

        // 4. CWD / templates/
        return FindPptxInDir(
            Path.Combine(Directory.GetCurrentDirectory(), "templates"));
    }

    private static string? FindPptxInDir(string dir)
    {
        if (!Directory.Exists(dir)) return null;
        return Directory
            .EnumerateFiles(dir, "*.pptx", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
    }
}
