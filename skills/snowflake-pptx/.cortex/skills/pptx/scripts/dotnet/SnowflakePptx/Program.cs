using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using SnowflakePptx.Core;
using SnowflakePptx.Schema;

using PShape    = DocumentFormat.OpenXml.Presentation.Shape;
using SlideId   = DocumentFormat.OpenXml.Presentation.SlideId;
using SlidePart = DocumentFormat.OpenXml.Packaging.SlidePart;
using DParagraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using DRun       = DocumentFormat.OpenXml.Drawing.Run;

var rootCommand = new RootCommand("Snowflake PPTX Generator");

// ── build subcommand ──────────────────────────────────────────────────────────

var specOption     = new Option<FileInfo>("--spec",     "YAML spec file")             { IsRequired = true };
var outOption      = new Option<FileInfo>("--out",      "Output PPTX path")           { IsRequired = true };
var templateOption = new Option<FileInfo?>("--template", "Template PPTX path (optional)");

var buildCommand = new Command("build", "Build a PPTX presentation from a YAML spec file");
buildCommand.AddOption(specOption);
buildCommand.AddOption(outOption);
buildCommand.AddOption(templateOption);

buildCommand.SetHandler(async (FileInfo spec, FileInfo output, FileInfo? template) =>
{
    var sw       = Stopwatch.StartNew();
    var warnings = new List<string>();
    var errors   = new List<string>();

    try
    {
        if (!spec.Exists)
        {
            errors.Add($"Spec file not found: {spec.FullName}");
            PrintBuildResult(success: false, slideCount: 0, warnings, errors, sw.Elapsed.TotalMilliseconds);
            Environment.Exit(1);
            return;
        }

        var yamlText = await File.ReadAllTextAsync(spec.FullName);

        PresentationSpec presentationSpec;
        try
        {
            presentationSpec = SlideSpecDeserializer.Deserialize(yamlText);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse YAML spec: {ex.Message}");
            PrintBuildResult(success: false, slideCount: 0, warnings, errors, sw.Elapsed.TotalMilliseconds);
            Environment.Exit(1);
            return;
        }

        // Ensure output directory exists.
        var outputDir = output.Directory;
        if (outputDir is not null && !outputDir.Exists)
            outputDir.Create();

        var builder = template is not null
            ? new PresentationBuilder(template.FullName)
            : new PresentationBuilder();

        var result = builder.Build(presentationSpec);

        if (result.Success && result.PptxBytes != null)
            PresentationBuilder.SaveToFile(result, output.FullName);

        foreach (var w in result.Warnings) warnings.Add(w);
        foreach (var e in result.Errors)   errors.Add(e);

        sw.Stop();
        PrintBuildResult(
            success:    result.Success && errors.Count == 0,
            slideCount: result.SlideCount,
            warnings,
            errors,
            sw.Elapsed.TotalMilliseconds);

        Environment.Exit(result.Success && errors.Count == 0 ? 0 : 1);
    }
    catch (Exception ex)
    {
        sw.Stop();
        errors.Add($"Unexpected error: {ex.Message}");
        PrintBuildResult(success: false, slideCount: 0, warnings, errors, sw.Elapsed.TotalMilliseconds);
        Environment.Exit(1);
    }
}, specOption, outOption, templateOption);

rootCommand.AddCommand(buildCommand);

// ── validate subcommand ───────────────────────────────────────────────────────

var validateFileArg = new Argument<FileInfo>("file", "PPTX file to validate");
var validateCommand = new Command("validate", "Validate a PPTX file using OpenXML validation");
validateCommand.AddArgument(validateFileArg);

validateCommand.SetHandler((FileInfo file) =>
{
    var validationErrors = new List<string>();

    if (!file.Exists)
    {
        validationErrors.Add($"File not found: {file.FullName}");
        PrintValidateResult(valid: false, validationErrors);
        Environment.Exit(1);
        return;
    }

    try
    {
        using var doc = PresentationDocument.Open(file.FullName, isEditable: false);
        var validator = new OpenXmlValidator();
        var issues    = validator.Validate(doc).ToList();

        foreach (var issue in issues)
        {
            var msg = $"[{issue.ErrorType}] {issue.Description}";
            if (!string.IsNullOrEmpty(issue.Path?.XPath))
                msg += $" at {issue.Path.XPath}";
            validationErrors.Add(msg);
        }

        var valid = validationErrors.Count == 0;
        PrintValidateResult(valid, validationErrors);
        Environment.Exit(valid ? 0 : 1);
    }
    catch (Exception ex)
    {
        validationErrors.Add($"Could not open file: {ex.Message}");
        PrintValidateResult(valid: false, validationErrors);
        Environment.Exit(1);
    }
}, validateFileArg);

rootCommand.AddCommand(validateCommand);

// ── validate-spec subcommand ──────────────────────────────────────────────────

var validateSpecFileArg = new Argument<FileInfo>("spec", "YAML spec file to validate");
var strictOption = new Option<bool>("--strict", "Treat warnings as errors (exit non-zero on any issue)");
var validateSpecCommand = new Command("validate-spec",
    "Validate a YAML spec file for content limit violations before building");
validateSpecCommand.AddArgument(validateSpecFileArg);
validateSpecCommand.AddOption(strictOption);

validateSpecCommand.SetHandler(async (FileInfo specFile, bool strict) =>
{
    var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    if (!specFile.Exists)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            valid    = false,
            warnings = Array.Empty<object>(),
            errors   = new[] { new { slide = -1, field = "file", message = $"File not found: {specFile.FullName}" } },
        }, jsonOpts));
        Environment.Exit(1);
        return;
    }

    PresentationSpec presentationSpec;
    try
    {
        var yamlText = await File.ReadAllTextAsync(specFile.FullName);
        presentationSpec = SlideSpecDeserializer.Deserialize(yamlText);
    }
    catch (Exception ex)
    {
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            valid    = false,
            warnings = Array.Empty<object>(),
            errors   = new[] { new { slide = -1, field = "yaml", message = $"Failed to parse YAML: {ex.Message}" } },
        }, jsonOpts));
        Environment.Exit(1);
        return;
    }

    var specResult = Validator.ValidateSpec(presentationSpec);

    var warnings = specResult.Warnings
        .Select(i => new { slide = i.SlideIndex, field = i.Field, message = i.Message })
        .ToArray();
    var errors = specResult.Errors
        .Select(i => new { slide = i.SlideIndex, field = i.Field, message = i.Message })
        .ToArray();

    var isValid = strict
        ? specResult.IsValid && !specResult.Warnings.Any()
        : specResult.IsValid;

    Console.WriteLine(JsonSerializer.Serialize(
        new { valid = isValid, strict, warnings, errors }, jsonOpts));
    Environment.Exit(isValid ? 0 : 1);
}, validateSpecFileArg, strictOption);

rootCommand.AddCommand(validateSpecCommand);

// ── render subcommand ─────────────────────────────────────────────────────────

var renderFileArg = new Argument<FileInfo>("file",      "PPTX file to render");
var outDirOption  = new Option<DirectoryInfo>("--out-dir", "Output directory for rendered slide images") { IsRequired = true };
var renderCommand = new Command("render", "Render PPTX slides to images (not yet implemented)");
renderCommand.AddArgument(renderFileArg);
renderCommand.AddOption(outDirOption);

renderCommand.SetHandler((FileInfo _, DirectoryInfo __) =>
{
    var result = new { error = "render command not yet implemented" };
    Console.WriteLine(JsonSerializer.Serialize(result,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    Environment.Exit(1);
}, renderFileArg, outDirOption);

rootCommand.AddCommand(renderCommand);

// ── dump-template subcommand ─────────────────────────────────────────────────

var dumpTemplateFileArg = new Argument<FileInfo>("template", "Template PPTX file to dump");
var dumpTemplateCommand = new Command("dump-template",
    "Dump all slide indices and text content from a template PPTX for mapping verification");
dumpTemplateCommand.AddArgument(dumpTemplateFileArg);

dumpTemplateCommand.SetHandler((FileInfo templateFile) =>
{
    if (!templateFile.Exists)
    {
        Console.Error.WriteLine($"File not found: {templateFile.FullName}");
        Environment.Exit(1);
        return;
    }

    try
    {
        using var doc = PresentationDocument.Open(templateFile.FullName, isEditable: false);
        var presPart = doc.PresentationPart
            ?? throw new InvalidOperationException("No PresentationPart found.");
        var slideIds = presPart.Presentation.SlideIdList!.Elements<SlideId>().ToList();

        var slides = new List<object>();
        for (int i = 0; i < slideIds.Count; i++)
        {
            var rId = slideIds[i].RelationshipId!.Value!;
            var slidePart = (SlidePart)presPart.GetPartById(rId);

            // Extract all text from the slide
            var texts = new List<string>();
            foreach (var shape in slidePart.Slide.Descendants<PShape>())
            {
                var shapeName = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value ?? "";
                var textBody = shape.TextBody;
                if (textBody == null) continue;

                var shapeText = string.Join(" | ",
                    textBody.Descendants<DParagraph>()
                        .Select(p => string.Join("", p.Descendants<DRun>().Select(r => r.Text?.Text ?? "")))
                        .Where(t => !string.IsNullOrWhiteSpace(t)));

                if (!string.IsNullOrWhiteSpace(shapeText))
                    texts.Add($"[{shapeName}] {shapeText}");
            }

            slides.Add(new { index = i, textCount = texts.Count, texts });
        }

        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        Console.WriteLine(JsonSerializer.Serialize(
            new { slideCount = slideIds.Count, slides }, jsonOpts));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}, dumpTemplateFileArg);

rootCommand.AddCommand(dumpTemplateCommand);

// ── Dispatch ──────────────────────────────────────────────────────────────────

return await rootCommand.InvokeAsync(args);

// ── Local helpers ─────────────────────────────────────────────────────────────

static void PrintBuildResult(
    bool         success,
    int          slideCount,
    List<string> warnings,
    List<string> errors,
    double       buildTimeMs)
{
    var result = new
    {
        success,
        slideCount,
        warnings,
        errors,
        buildTimeMs = Math.Round(buildTimeMs, 1)
    };
    Console.WriteLine(JsonSerializer.Serialize(result,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}

static void PrintValidateResult(bool valid, List<string> errors)
{
    var result = new
    {
        valid,
        errors,
        errorCount = errors.Count
    };
    Console.WriteLine(JsonSerializer.Serialize(result,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
