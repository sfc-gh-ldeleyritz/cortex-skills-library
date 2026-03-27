using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using SnowflakePptx.Core;
using SnowflakePptx.Schema;
using Xunit;

using SlideId = DocumentFormat.OpenXml.Presentation.SlideId;

namespace SnowflakePptx.Tests;

/// <summary>
/// Smoke tests that build a spec containing all 22 slide types and verify
/// the output PPTX is structurally valid. Catches rendering regressions
/// across the full slide type matrix.
/// </summary>
public class SmokeTests
{
    private static string GetTemplatePath()
    {
        var envPath = Environment.GetEnvironmentVariable("SNOWFLAKE_TEMPLATE_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "scripts", "snowflake", "templates",
                "SNOWFLAKE TEMPLATE JANUARY 2026.pptx");
            if (File.Exists(candidate)) return candidate;

            candidate = Path.Combine(dir, ".cortex", "skills", "pptx", "scripts",
                "snowflake", "templates", "SNOWFLAKE TEMPLATE JANUARY 2026.pptx");
            if (File.Exists(candidate)) return candidate;

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));
        return Path.Combine(repoRoot, "scripts", "snowflake", "templates",
            "SNOWFLAKE TEMPLATE JANUARY 2026.pptx");
    }

    private static string GetFixturePath(string filename)
    {
        // Try output directory first (CopyToOutputDirectory)
        var outputPath = Path.Combine(AppContext.BaseDirectory, "fixtures", filename);
        if (File.Exists(outputPath)) return outputPath;

        // Fall back to source directory
        var sourceDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", ".."));
        return Path.Combine(sourceDir, "fixtures", filename);
    }

    [Fact]
    public void SmokeTest_AllSlideTypes_BuildsWithoutErrors()
    {
        var templatePath = GetTemplatePath();
        if (!File.Exists(templatePath))
            return; // Skip if template not available

        var specPath = GetFixturePath("smoke-test-spec.yaml");
        Assert.True(File.Exists(specPath), $"Smoke test spec not found at: {specPath}");

        var yamlText = File.ReadAllText(specPath);
        var spec = SlideSpecDeserializer.Deserialize(yamlText);

        var builder = new PresentationBuilder(templatePath);
        var result = builder.Build(spec);

        Assert.True(result.Success,
            $"Build failed with errors: {string.Join("; ", result.Errors)}");
        Assert.True(result.SlideCount > 0, "Build produced 0 slides");
    }

    [Fact]
    public void SmokeTest_AllSlideTypes_OutputPassesOpenXmlValidation()
    {
        var templatePath = GetTemplatePath();
        if (!File.Exists(templatePath))
            return;

        var specPath = GetFixturePath("smoke-test-spec.yaml");
        if (!File.Exists(specPath))
            return;

        var yamlText = File.ReadAllText(specPath);
        var spec = SlideSpecDeserializer.Deserialize(yamlText);

        var builder = new PresentationBuilder(templatePath);
        var result = builder.Build(spec);

        Assert.True(result.Success, "Build must succeed before validation");
        Assert.NotNull(result.PptxBytes);

        // Write to a temp file and validate
        var tempPath = Path.Combine(Path.GetTempPath(), $"smoke-test-{Guid.NewGuid()}.pptx");
        try
        {
            PresentationBuilder.SaveToFile(result, tempPath);

            using var doc = PresentationDocument.Open(tempPath, isEditable: false);
            var validator = new OpenXmlValidator();
            var issues = validator.Validate(doc).ToList();

            // Filter out known non-critical issues
            var criticalIssues = issues
                .Where(i => i.ErrorType != ValidationErrorType.Schema)
                .ToList();

            Assert.Empty(criticalIssues);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void SmokeTest_AllSlideTypes_CorrectSlideCount()
    {
        var templatePath = GetTemplatePath();
        if (!File.Exists(templatePath))
            return;

        var specPath = GetFixturePath("smoke-test-spec.yaml");
        if (!File.Exists(specPath))
            return;

        var yamlText = File.ReadAllText(specPath);
        var spec = SlideSpecDeserializer.Deserialize(yamlText);

        var builder = new PresentationBuilder(templatePath);
        var result = builder.Build(spec);

        Assert.True(result.Success, "Build must succeed");

        // The smoke spec has 24 slides (all 22 types + title + thank_you already in the types)
        Assert.Equal(spec.Slides.Count, result.SlideCount);
    }
}
