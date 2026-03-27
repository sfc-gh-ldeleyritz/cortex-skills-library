using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace SnowflakePptx.Core;

/// <summary>
/// Deep-clones a slide from a read-only template <see cref="PresentationDocument"/>
/// into a target document, copying all relationships (images, layouts, externals).
///
/// C# port of <c>XMLSlideCloner._deep_clone_slide</c> and its helpers.
/// </summary>
public class SlideCloner
{
    // ── Constants ────────────────────────────────────────────────────────────

    /// <summary>Number of slides in the Snowflake template (0-based max index = 22).</summary>
    public const int TemplateSlideCount = 23;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Clone the slide at <paramref name="templateIndex"/> from
    /// <paramref name="templateDoc"/> into <paramref name="targetDoc"/>.
    /// </summary>
    /// <param name="templateDoc">Source template (opened read-only or editable).</param>
    /// <param name="targetDoc">Target document that receives the new slide.</param>
    /// <param name="templateIndex">0-based index of the source slide.</param>
    /// <returns>The newly added <see cref="SlidePart"/> in the target document.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   Thrown when <paramref name="templateIndex"/> is outside [0, TemplateSlideCount).
    /// </exception>
    public SlidePart CloneSlide(
        PresentationDocument templateDoc,
        PresentationDocument targetDoc,
        int templateIndex)
    {
        if (templateIndex < 0 || templateIndex >= TemplateSlideCount)
            throw new ArgumentOutOfRangeException(
                nameof(templateIndex),
                $"Template index {templateIndex} is outside [0, {TemplateSlideCount}).");

        var templatePresentationPart = templateDoc.PresentationPart
            ?? throw new InvalidOperationException("Template document has no PresentationPart.");
        var targetPresentationPart = targetDoc.PresentationPart
            ?? throw new InvalidOperationException("Target document has no PresentationPart.");

        // ── Locate source slide ───────────────────────────────────────────────
        var sourceSlideId = templatePresentationPart.Presentation.SlideIdList!
            .Elements<SlideId>()
            .ElementAt(templateIndex);

        var sourceRId = sourceSlideId.RelationshipId!.Value!;
        var sourceSlidePart = (SlidePart)templatePresentationPart
            .GetPartById(sourceRId);

        // ── Add new slide part to target ─────────────────────────────────────
        var newSlidePart = targetPresentationPart.AddNewPart<SlidePart>();

        // Deep-copy the raw slide XML bytes so all shapes, fills, and formatting
        // are preserved exactly as they appear in the template.
        using (var srcStream = sourceSlidePart.GetStream(FileMode.Open, FileAccess.Read))
        using (var dstStream = newSlidePart.GetStream(FileMode.Create, FileAccess.Write))
        {
            srcStream.CopyTo(dstStream);
        }

        // Force the SDK to re-parse the copied XML.
        newSlidePart.Slide.Reload();

        // ── Copy all relationships ───────────────────────────────────────────
        var rIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
        CopySlideRelationships(sourceSlidePart, newSlidePart, targetDoc, rIdMap);
        UpdateRelationshipIds(newSlidePart, rIdMap);

        // ── Register in SlideIdList ──────────────────────────────────────────
        var slideIdList = targetPresentationPart.Presentation.SlideIdList
            ?? throw new InvalidOperationException("Target SlideIdList is null.");

        uint maxId = slideIdList.Elements<SlideId>()
            .Select(s => s.Id?.Value ?? 0u)
            .DefaultIfEmpty(255u)
            .Max();

        var newSlideId = new SlideId
        {
            Id = maxId + 1,
            RelationshipId = targetPresentationPart.GetIdOfPart(newSlidePart)
        };
        slideIdList.Append(newSlideId);
        targetPresentationPart.Presentation.Save();

        return newSlidePart;
    }

    // ── Relationship copying ─────────────────────────────────────────────────

    private static void CopySlideRelationships(
        SlidePart source,
        SlidePart target,
        PresentationDocument targetDoc,
        Dictionary<string, string> rIdMap)
    {
        // Parse the source slide's .rels XML directly via IPackage so we get
        // every relationship regardless of SDK enumeration limitations.
        var allRels = ReadSlideRelsXml(source);

        const string imageType    = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
        const string layoutType   = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout";

        foreach (var (relId, relType, relTarget, isExternal) in allRels)
        {
            if (isExternal)
            {
                // ── External relationship (hyperlink, etc.) ───────────────────
                try
                {
                    var uri = new Uri(relTarget, UriKind.RelativeOrAbsolute);
                    AddExternalRel(target, relType, uri, relId, rIdMap);
                }
                catch { /* ignore — broken hyperlink is acceptable */ }
            }
            else if (relType == layoutType)
            {
                // ── Slide layout ──────────────────────────────────────────────
                var layoutPart = source.SlideLayoutPart;
                if (layoutPart != null)
                {
                    var layoutName   = layoutPart.SlideLayout?.CommonSlideData?.Name?.Value;
                    var targetLayout = FindMatchingLayout(targetDoc, layoutName);
                    var partToAdd    = targetLayout ?? (OpenXmlPart)layoutPart;
                    try
                    {
                        target.AddPart(partToAdd, relId);
                    }
                    catch
                    {
                        var newPart = target.AddPart(partToAdd);
                        var newId   = target.GetIdOfPart(newPart);
                        if (newId != relId) rIdMap[relId] = newId;
                    }
                }
            }
            else if (relType == imageType)
            {
                // ── Image (internal) ─────────────────────────────────────────
                // Find the matching ImagePart in the source by rId.
                try
                {
                    var srcImagePart = (ImagePart)source.GetPartById(relId);
                    ImagePart newImagePart;
                    try
                    {
                        newImagePart = target.AddNewPart<ImagePart>(srcImagePart.ContentType, relId);
                    }
                    catch
                    {
                        newImagePart = target.AddNewPart<ImagePart>(srcImagePart.ContentType);
                    }
                    using var srcStream = srcImagePart.GetStream(FileMode.Open, FileAccess.Read);
                    using var dstStream = newImagePart.GetStream(FileMode.Create, FileAccess.Write);
                    srcStream.CopyTo(dstStream);

                    var assignedRId = target.GetIdOfPart(newImagePart);
                    if (assignedRId != relId) rIdMap[relId] = assignedRId;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[SlideCloner] Image copy failed {relId}: {ex.Message}");
                }
            }
            // Other internal rel types are ignored (video/audio not used in Snowflake template).
        }
    }

    /// <summary>
    /// Walk every XML element in the newly cloned slide and rewrite any
    /// relationship-ID attribute whose value was remapped due to an ID
    /// collision during copying.
    /// </summary>
    private static void UpdateRelationshipIds(
        SlidePart slidePart,
        Dictionary<string, string> rIdMap)
    {
        if (rIdMap.Count == 0) return;

        // Attributes that carry embedded relationship IDs inside slide XML:
        //   r:embed  → blip fills and pictures
        //   r:link   → hyperlinks on images
        //   r:id     → media references (video/audio)
        const string rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        foreach (var elem in slidePart.Slide.Descendants())
        {
            foreach (var localName in new[] { "embed", "link", "id" })
            {
                try
                {
                    var attr = elem.GetAttribute(localName, rNs);
                    var attrValue = attr.Value;
                    if (!string.IsNullOrEmpty(attrValue) && rIdMap.TryGetValue(attrValue, out var newId))
                        elem.SetAttribute(new OpenXmlAttribute(localName, rNs, newId));
                }
                catch
                {
                    // Some strongly-typed OpenXml elements reject unknown attributes — skip them.
                }
            }
        }
    }

    // ── Layout lookup ────────────────────────────────────────────────────────

    /// <summary>
    /// Search all slide masters in <paramref name="doc"/> for a layout whose
    /// <c>CommonSlideData.Name</c> matches <paramref name="layoutName"/>.
    /// Returns <c>null</c> when no match is found.
    /// </summary>
    private static SlideLayoutPart? FindMatchingLayout(
        PresentationDocument doc,
        string? layoutName)
    {
        if (layoutName is null) return null;

        var presPart = doc.PresentationPart;
        if (presPart is null) return null;

        foreach (var masterPart in presPart.SlideMasterParts)
        {
            foreach (var layoutPart in masterPart.SlideLayoutParts)
            {
                var name = layoutPart.SlideLayout?.CommonSlideData?.Name?.Value;
                if (string.Equals(name, layoutName, StringComparison.OrdinalIgnoreCase))
                    return layoutPart;
            }
        }

        return null;
    }

    // ── External / hyperlink relationship helper ─────────────────────────────

    private const string HyperlinkRelType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";

    /// <summary>
    /// Adds an external relationship to <paramref name="target"/>, using
    /// <c>AddHyperlinkRelationship</c> for hyperlinks (required by SDK 3.x).
    /// Handles rId collisions by assigning a new ID.
    /// </summary>
    private static void AddExternalRel(
        OpenXmlPart target, string relType, Uri uri, string relId,
        Dictionary<string, string> rIdMap)
    {
        void AddWithId(string id)
        {
            if (relType == HyperlinkRelType)
                target.AddHyperlinkRelationship(uri, isExternal: true, id);
            else
                target.AddExternalRelationship(relType, uri, id);
        }

        try
        {
            AddWithId(relId);
        }
        catch
        {
            // rId collision — assign a new one.
            var allIds = target.Parts.Select(p => p.RelationshipId)
                .Concat(target.HyperlinkRelationships.Select(r => r.Id))
                .Concat(target.ExternalRelationships.Select(r => r.Id))
                .Where(id => id.StartsWith("rId"))
                .Select(id => int.TryParse(id.Substring(3), out var n) ? n : 0)
                .ToList();
            var nextNum = (allIds.Count > 0 ? allIds.Max() : 0) + 1;
            var newId   = $"rId{nextNum}";
            try
            {
                AddWithId(newId);
                rIdMap[relId] = newId;
            }
            catch { /* ignore — broken hyperlink is acceptable */ }
        }
    }
    /// (relId, relType, relTarget, isExternal) tuples.
    /// Uses the SDK 3.x IPackage interface (obtained via reflection) to access
    /// the raw OPC .rels stream directly — more reliable than SDK enumeration.
    /// </summary>
    private static List<(string Id, string Type, string Target, bool IsExternal)>
        ReadSlideRelsXml(SlidePart source)
    {
        const string relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        var result = new List<(string, string, string, bool)>();

        try
        {
            var sourceUri     = source.Uri.ToString(); // e.g. /ppt/slides/slide2.xml
            var relsUriString = sourceUri
                .Replace("/ppt/slides/", "/ppt/slides/_rels/") + ".rels";

            // SDK 3.x: OpenXmlPackage.Package returns IPackage (not System.IO.Packaging.Package).
            var packageProp = typeof(OpenXmlPackage).GetProperty(
                "Package",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public   |
                System.Reflection.BindingFlags.NonPublic);

            var pkg = packageProp?.GetValue(source.OpenXmlPackage);
            if (pkg is null) return result;

            var pkgType           = pkg.GetType();
            var partExistsMethod  = pkgType.GetMethod("PartExists",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                null, new[] { typeof(Uri) }, null);
            var getPartMethod     = pkgType.GetMethod("GetPart",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                null, new[] { typeof(Uri) }, null);

            if (partExistsMethod is null || getPartMethod is null) return result;

            var relsUri = new Uri(relsUriString, UriKind.Relative);
            var exists  = (bool)partExistsMethod.Invoke(pkg, new object[] { relsUri })!;
            if (!exists) return result;

            var part           = getPartMethod.Invoke(pkg, new object[] { relsUri })!;
            var getStreamMethod = part.GetType().GetMethod("GetStream",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
                null, new[] { typeof(FileMode), typeof(FileAccess) }, null);

            if (getStreamMethod is null) return result;

            XDocument relsDoc;
            using (var relsStream = (Stream)getStreamMethod.Invoke(
                       part, new object[] { FileMode.Open, FileAccess.Read })!)
                relsDoc = XDocument.Load(relsStream);

            var ns = XNamespace.Get(relNs);
            foreach (var rel in relsDoc.Root?.Elements(ns + "Relationship")
                                 ?? Enumerable.Empty<XElement>())
            {
                var relId     = rel.Attribute("Id")?.Value;
                var relType   = rel.Attribute("Type")?.Value;
                var relTarget = rel.Attribute("Target")?.Value;
                if (relId is null || relType is null || relTarget is null) continue;

                var isExternal = string.Equals(
                    rel.Attribute("TargetMode")?.Value, "External",
                    StringComparison.OrdinalIgnoreCase);

                result.Add((relId, relType, relTarget, isExternal));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SlideCloner] ReadSlideRelsXml failed: {ex.GetType().Name}: {ex.Message}");
        }

        return result;
    }
}
