using SnowflakePptx.Schema;
using Xunit;

namespace SnowflakePptx.Tests;

/// <summary>
/// Unit tests covering Validator enhancements from PPTX_GENERATION_REVIEW.md:
///   R1 – Width-aware validation for split slide titles (30-char limit + word heuristic)
///   R2 – Line-count estimation for narrow column text boxes
///   R6 – Overflow-critical fields produce errors (not warnings)
///   R5 – validate-spec logic via Validator.ValidateSpec
/// </summary>
public class ValidatorTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Build a SlideSpec directly without YAML parsing.</summary>
    private static SlideSpec MakeSlide(string type, Dictionary<string, string> fields)
    {
        var slide = new SlideSpec();
        var raw   = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            { ["type"] = type };
        foreach (var kv in fields)
            raw[kv.Key] = kv.Value;
        slide.RawInternal = raw;
        return slide;
    }

    /// <summary>Wrap slides in a minimal valid presentation (title + content + thank_you).</summary>
    private static PresentationSpec Wrap(params SlideSpec[] slides)
    {
        var spec = new PresentationSpec();
        spec.Slides.Add(MakeSlide("title", new() { ["title_line1"] = "T" }));
        spec.Slides.AddRange(slides);
        spec.Slides.Add(MakeSlide("thank_you", new()));
        return spec;
    }

    // ── R1: Split title 30-char limit ─────────────────────────────────────────

    [Fact]
    public void Split_TitleWithin30Chars_NoCharLimitIssue()
    {
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = "Performance",   // 11 chars — well within 30
            ["content"] = "Benchmark results for TPC-H 100GB.",
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "title" && i.Message.Contains("char limit for split"));
    }

    [Fact]
    public void Split_TitleExceeds30Chars_ProducesWarning()
    {
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = "Performance Benchmarks Detailed",  // 31 chars — over limit
            ["content"] = "Some content here.",
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "title" && i.Message.Contains("char limit for split"));
        Assert.NotNull(issue);
        Assert.Equal(Severity.Warning, issue.Severity);
    }

    // ── R1: Word-length heuristic ─────────────────────────────────────────────

    [Fact]
    public void Split_TitleWithLongWord_ProducesWordHeuristicWarning()
    {
        // "Containerization" = 16 chars — exceeds the 15-char threshold
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = "Containerization",
            ["content"] = "Overview of container orchestration.",
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "title" && w.Message.Contains("word") && w.Message.Contains("wrapping"));
        Assert.NotNull(warning);
    }

    [Fact]
    public void Split_TitleWithShortWords_NoWordHeuristicWarning()
    {
        // "Platform" = 8 chars — well under 15-char threshold
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = "AI Platform",
            ["content"] = "Overview of the AI platform.",
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w =>
            w.Field == "title" && w.Message.Contains("word") && w.Message.Contains("wrapping"));
    }

    // ── R2: Line-count estimation — three_column_icons ────────────────────────

    [Fact]
    public void ThreeColumnIcons_ColContentWithinLineLimit_NoLineCountWarning()
    {
        // "7 GA SQL functions" = 18 chars → 1 line at 25 chars/line
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "AI Features",
            ["col1_title"]   = "ML",
            ["col1_content"] = "7 GA SQL functions",           // 18 chars → 1 line
            ["col2_title"]   = "Search",
            ["col2_content"] = "Hybrid search across data",    // 25 chars → 1 line
            ["col3_title"]   = "Agents",
            ["col3_content"] = "Agentic framework support",    // 25 chars → 1 line
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("lines exceeds"));
    }

    [Fact]
    public void ThreeColumnIcons_ColContentExceedsLineLimit_ProducesLineCountWarning()
    {
        // 126 chars → ceil(126/25) = 6 lines — exceeds max 4
        const string longContent = "AI_COMPLETE, AI_CLASSIFY, AI_EXTRACT, AI_SUMMARIZE, AI_TRANSLATE, AI_SENTIMENT, AI_ANOMALY, AI_FORECAST, AI_CLUSTER, AI_EMBED";
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "Cortex AI Functions",
            ["col1_title"]   = "Functions",
            ["col1_content"] = longContent,
            ["col2_title"]   = "Search",
            ["col2_content"] = "Hybrid vector search across all your enterprise data sources",
            ["col3_title"]   = "Agents",
            ["col3_content"] = "Autonomous AI agents with built-in governance controls",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("lines exceeds"));
        Assert.NotNull(warning);
        Assert.Contains("4-line maximum", warning.Message);
    }

    // ── R2: Line-count estimation — four_column_numbers ───────────────────────

    [Fact]
    public void FourColumnNumbers_ColContentWithinLineLimit_NoLineCountWarning()
    {
        var spec = Wrap(MakeSlide("four_column_numbers", new()
        {
            ["title"]        = "Stats",
            ["col1_number"]  = "5",
            ["col1_content"] = "Siloed systems",     // 14 chars → 1 line
            ["col2_number"]  = "10M+",
            ["col2_content"] = "Daily transactions", // 18 chars → 1 line (exactly at charsPerLine)
            ["col3_number"]  = "$2M",
            ["col3_content"] = "Annual cost",
            ["col4_number"]  = "40%",
            ["col4_content"] = "Cost target",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("lines exceeds"));
    }

    [Fact]
    public void FourColumnNumbers_ColContentExceedsLineLimit_ProducesLineCountWarning()
    {
        // 70 chars → ceil(70/23) = 4 lines — exceeds max 3
        const string content = "Siloed legacy systems spread across many global regions needing urgent modernization"; // 83 chars
        var spec = Wrap(MakeSlide("four_column_numbers", new()
        {
            ["title"]        = "Business Impact",
            ["col1_number"]  = "5",
            ["col1_content"] = content,
            ["col2_number"]  = "10M+",
            ["col2_content"] = "Daily transactions",
            ["col3_number"]  = "$2M",
            ["col3_content"] = "Annual cost",
            ["col4_number"]  = "40%",
            ["col4_content"] = "Cost reduction",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("lines exceeds"));
        Assert.NotNull(warning);
        Assert.Contains("3-line maximum", warning.Message);
    }

    // ── R2: Line-count estimation — three_column_titled ───────────────────────

    [Fact]
    public void ThreeColumnTitled_ColContentExceedsLineLimit_ProducesWarning()
    {
        // 6 paragraphs of explicit newlines → exceeds max 5 lines
        const string content = "Line1\nLine2\nLine3\nLine4\nLine5\nLine6";  // 6 lines via \n
        var spec = Wrap(MakeSlide("three_column_titled", new()
        {
            ["title"]        = "Overview",
            ["col1_title"]   = "Col1",
            ["col1_content"] = content,
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Short",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Short",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("lines exceeds"));
        Assert.NotNull(warning);
    }

    // ── R2: Line-count estimation — four_column_icons ─────────────────────────

    [Fact]
    public void FourColumnIcons_ColContentExceedsLineLimit_ProducesWarning()
    {
        // Word-wrap estimation: ~18 chars/line for four_column_icons.
        // This 74-char string with spaces wraps to 5 lines, exceeding the 4-line max.
        var content73 = "Enterprise grade multi cloud analytics with deep governance and compliance";
        var spec = Wrap(MakeSlide("four_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = "Speed",
            ["col1_content"] = content73,
            ["col2_title"]   = "Scale",
            ["col2_content"] = "Elastic compute scaling across multiple cloud regions",
            ["col3_title"]   = "Security",
            ["col3_content"] = "Enterprise compliance and governance built into every layer",
            ["col4_title"]   = "Sharing",
            ["col4_content"] = "Native marketplace with secure cross-cloud data sharing",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("lines exceeds"));
        Assert.NotNull(warning);
        Assert.Contains("4-line maximum", warning.Message);
    }

    // ── R6: Overflow-critical fields produce errors ───────────────────────────

    [Fact]
    public void ThreeColumnIcons_ColContentExceedsCharLimit_ProducesError()
    {
        // 101 chars exceeds the 100-char limit for three_column_icons col_content
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = "Col1",
            ["col1_content"] = new string('x', 101),  // over 100-char limit
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Short content that meets the minimum length requirement for this field",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Short content that meets the minimum length requirement for this field",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var error = result.Errors.FirstOrDefault(e =>
            e.Field == "col1_content" && e.Message.Contains("char limit"));
        Assert.NotNull(error);
        Assert.Equal(Severity.Error, error.Severity);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ThreeColumnIcons_ColContentAtLimit_NoCharLimitError()
    {
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = "Col1",
            ["col1_content"] = new string('x', 100),  // exactly at the 100-char limit
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Short content that meets the minimum length requirement for this field",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Short content that meets the minimum length requirement for this field",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Errors, e =>
            e.Field == "col1_content" && e.Message.Contains("char limit"));
    }

    [Fact]
    public void SplitContent_ExceedsCharLimit_ProducesError()
    {
        // 'content' is overflow-critical; 301 chars exceeds the 300-char limit
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = "Short",
            ["content"] = new string('x', 301),
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var error = result.Errors.FirstOrDefault(e =>
            e.Field == "content" && e.Message.Contains("char limit"));
        Assert.NotNull(error);
        Assert.Equal(Severity.Error, error.Severity);
    }

    [Fact]
    public void Title_ExceedsCharLimit_ProducesWarningNotError()
    {
        // 'title' is NOT overflow-critical — should stay a warning
        var spec = Wrap(MakeSlide("content", new()
        {
            ["title"] = new string('x', 61),  // exceeds 60-char limit for 'content' type
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "title" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
        Assert.Equal(Severity.Warning, issue.Severity);
    }

    // ── R6: col_title is also overflow-critical ───────────────────────────────

    [Fact]
    public void ThreeColumnIcons_ColTitleExceedsLimit_ProducesError()
    {
        // col_title limit for three_column_icons is 25 chars; 26 should be an error
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = new string('x', 26),  // over 25-char limit
            ["col1_content"] = "Short content",
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Short content",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Short content",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var error = result.Errors.FirstOrDefault(e => e.Field == "col1_title");
        Assert.NotNull(error);
        Assert.Equal(Severity.Error, error.Severity);
    }

    // ── R5: validate-spec logic (ValidateSpec integration tests) ─────────────

    [Fact]
    public void ValidateSpec_EmptySlideList_ReturnsError()
    {
        var spec = new PresentationSpec();
        var result = Validator.ValidateSpec(spec);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "slides");
    }

    [Fact]
    public void ValidateSpec_UnknownSlideType_ReturnsError()
    {
        var spec = Wrap(MakeSlide("nonexistent_type", new()));
        var result = Validator.ValidateSpec(spec);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unknown slide type"));
    }

    [Fact]
    public void ValidateSpec_ValidMinimalSpec_NoErrors()
    {
        var spec = Wrap(MakeSlide("content", new()
        {
            ["title"] = "My Slide",
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateSpec_SplitTitleExactly30Chars_NoCharLimitIssue()
    {
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = new string('x', 30),  // exactly at the 30-char limit
            ["content"] = "Some content.",
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "title" && i.Message.Contains("char limit for split"));
    }

    [Fact]
    public void ValidateSpec_SplitTitleAt31Chars_ProducesWarning()
    {
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = new string('x', 31),  // one over the 30-char limit
            ["content"] = "Some content.",
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "title" && i.Message.Contains("char limit for split"));
        Assert.NotNull(issue);
        Assert.Equal(Severity.Warning, issue.Severity);
    }

    [Fact]
    public void ValidateSpec_IssuesSerializeToCorrectFormat()
    {
        // Confirm that ValidationIssue properties are populated correctly
        // (validates the data contract used by the validate-spec command)
        var spec = Wrap(MakeSlide("split", new()
        {
            ["title"]   = new string('x', 31),
            ["content"] = new string('x', 301),  // error
            ["notes"]   = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        // title issue: warning at slide index 1 (index 0 is title, index 2 is thank_you)
        var titleIssue = result.Warnings.First(w =>
            w.Field == "title" && w.Message.Contains("char limit for split"));
        Assert.Equal(1, titleIssue.SlideIndex);
        Assert.False(string.IsNullOrEmpty(titleIssue.Message));

        // content error: error at slide index 1
        var contentError = result.Errors.First(e => e.Field == "content");
        Assert.Equal(1, contentError.SlideIndex);
        Assert.Equal("error", contentError.SeverityString);
    }

    // ── R1.4: MinLength enforcement tests ─────────────────────────────────────

    [Fact]
    public void ThreeColumnIcons_ColContentBelowMinLength_ProducesWarning()
    {
        // 30 chars is below the 50-char minimum for three_column_icons col_content
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = "Col1",
            ["col1_content"] = "ML/AI pipelines with Spark.",   // 27 chars — below 50 min
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Industry-leading orchestration with deep Apache Spark integration",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Fully serverless analytics engine for GCP-native teams handling queries",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("below the recommended"));
        Assert.NotNull(warning);
        Assert.Contains("50 char minimum", warning.Message);
    }

    [Fact]
    public void ThreeColumnIcons_ColContentAboveMinLength_NoMinLengthWarning()
    {
        // 70 chars is above the 50-char minimum
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = "Col1",
            ["col1_content"] = "Industry-leading ML/AI pipeline orchestration with deep Spark support",  // 70 chars
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Industry-leading orchestration with deep Apache Spark integration",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Fully serverless analytics engine for GCP-native teams handling queries",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w =>
            w.Field == "col1_content" && w.Message.Contains("below the recommended"));
    }

    [Fact]
    public void FourColumnNumbers_ColContentBelowMinLength_ProducesWarning()
    {
        // 21 chars is below the 30-char minimum for four_column_numbers col_content
        var spec = Wrap(MakeSlide("four_column_numbers", new()
        {
            ["title"]        = "Stats",
            ["col1_number"]  = "18.3%",
            ["col1_content"] = "Cloud DW market share",   // 21 chars — below 30 min
            ["col2_number"]  = "3",
            ["col2_content"] = "Major clouds supported natively",
            ["col3_number"]  = "99.99%",
            ["col3_content"] = "Uptime SLA with disaster recovery",
            ["col4_number"]  = "50-70%",
            ["col4_content"] = "Savings versus Databricks reported",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("below the recommended"));
        Assert.NotNull(warning);
        Assert.Contains("30 char minimum", warning.Message);
    }

    [Fact]
    public void FourColumnIcons_ColContentBelowMinLength_ProducesWarning()
    {
        // 30 chars is below the 40-char minimum for four_column_icons col_content
        var spec = Wrap(MakeSlide("four_column_icons", new()
        {
            ["title"]        = "Features",
            ["col1_title"]   = "Speed",
            ["col1_content"] = "Fast multi-cloud SQL analytics",  // 30 chars — below 40 min
            ["col2_title"]   = "Scale",
            ["col2_content"] = "Elastic compute scaling across multiple cloud regions",
            ["col3_title"]   = "Security",
            ["col3_content"] = "Enterprise compliance and governance built into every layer",
            ["col4_title"]   = "Sharing",
            ["col4_content"] = "Native marketplace with secure cross-cloud data sharing",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "col1_content" && w.Message.Contains("below the recommended"));
        Assert.NotNull(warning);
        Assert.Contains("40 char minimum", warning.Message);
    }

    // ── R7.1: quote_simple attribution limit tests ────────────────────────────

    [Fact]
    public void QuoteSimple_AttributionExceeds40Chars_ProducesWarning()
    {
        var spec = Wrap(MakeSlide("quote_simple", new()
        {
            ["quote_text"]  = "Snowflake was an amazing partnership.",
            ["attribution"] = "Enterprise Customer, Migrated from Databricks",  // 46 chars — over 40
            ["notes"]       = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "attribution" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    [Fact]
    public void QuoteSimple_AttributionWithin40Chars_NoCharLimitIssue()
    {
        var spec = Wrap(MakeSlide("quote_simple", new()
        {
            ["quote_text"]  = "Snowflake was an amazing partnership.",
            ["attribution"] = "Enterprise Customer",  // 19 chars — well within 40
            ["notes"]       = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "attribution" && i.Message.Contains("char limit"));
    }

    // ── Issue 2: Divider slide capitalization warnings ───────────────────────

    [Fact]
    public void Section_MixedCaseTitle_ProducesDividerCapWarning()
    {
        var spec = Wrap(MakeSlide("section", new()
        {
            ["title"] = "Hooks and Guardrails",
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "title" && w.Message.Contains("auto-uppercased"));
        Assert.NotNull(warning);
    }

    [Fact]
    public void ChapterParticle_MixedCaseTitle_ProducesDividerCapWarning()
    {
        var spec = Wrap(MakeSlide("chapter_particle", new()
        {
            ["title"] = "Next Steps",
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "title" && w.Message.Contains("auto-uppercased"));
        Assert.NotNull(warning);
    }

    [Fact]
    public void Section_AllCapsTitle_NoDividerCapWarning()
    {
        var spec = Wrap(MakeSlide("section", new()
        {
            ["title"] = "HOOKS AND GUARDRAILS",
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w =>
            w.Field == "title" && w.Message.Contains("auto-uppercased"));
    }

    // ── Issue 3: Agenda item format validation ───────────────────────────────

    [Fact]
    public void Agenda_MultiKeyDictWithTitle_ProducesWarning()
    {
        // Build an agenda slide with the two-key format
        var slide = new SlideSpec();
        var agendaItem = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = "The Agentic Engineering Era",
            ["subitems"] = new List<object?> { "Industry trends" },
        };
        var items = new List<object?> { agendaItem };
        slide.RawInternal = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "agenda",
            ["items"] = items,
        };

        var spec = Wrap(slide);
        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "items" && w.Message.Contains("multi-key dict format"));
        Assert.NotNull(warning);
    }

    [Fact]
    public void Agenda_SingleKeyDict_NoFormatWarning()
    {
        // Build an agenda slide with the preferred single-key format
        var slide = new SlideSpec();
        var innerDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["subitems"] = new List<object?> { "Industry trends" },
        };
        var agendaItem = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["The Agentic Engineering Era"] = innerDict,
        };
        var items = new List<object?> { agendaItem };
        slide.RawInternal = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "agenda",
            ["items"] = items,
        };

        var spec = Wrap(slide);
        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w =>
            w.Field == "items" && w.Message.Contains("multi-key"));
    }

    [Fact]
    public void Agenda_MalformedDictNoTitle_ProducesError()
    {
        var slide = new SlideSpec();
        var malformed = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["foo"] = "bar",
            ["baz"] = "qux",
        };
        var items = new List<object?> { malformed };
        slide.RawInternal = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "agenda",
            ["items"] = items,
        };

        var spec = Wrap(slide);
        var result = Validator.ValidateSpec(spec);

        var error = result.Errors.FirstOrDefault(e =>
            e.Field == "items" && e.Message.Contains("no 'title' key"));
        Assert.NotNull(error);
    }

    // ── Tightened title limits (content slides) ─────────────────────────────

    [Fact]
    public void Content_TitleExceeds45Chars_ProducesWarning()
    {
        // content title limit reduced from 60 to 45
        var spec = Wrap(MakeSlide("content", new()
        {
            ["title"] = new string('x', 46),  // over 45-char limit
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "title" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    [Fact]
    public void Content_TitleAt45Chars_NoCharLimitIssue()
    {
        var spec = Wrap(MakeSlide("content", new()
        {
            ["title"] = new string('x', 45),  // exactly at limit
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "title" && i.Message.Contains("char limit"));
    }

    [Fact]
    public void ColumnSlide_TitleExceeds50Chars_ProducesWarning()
    {
        // three_column_icons title limit reduced from 60 to 50
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = new string('x', 51),  // over 50-char limit
            ["col1_title"]   = "Col1",
            ["col1_content"] = "Short content that meets the minimum length requirement for this field",
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Short content that meets the minimum length requirement for this field",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Short content that meets the minimum length requirement for this field",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "title" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    [Fact]
    public void ColumnSlide_TitleAt50Chars_NoCharLimitIssue()
    {
        var spec = Wrap(MakeSlide("three_column_icons", new()
        {
            ["title"]        = new string('x', 50),
            ["col1_title"]   = "Col1",
            ["col1_content"] = "Short content that meets the minimum length requirement for this field",
            ["col2_title"]   = "Col2",
            ["col2_content"] = "Short content that meets the minimum length requirement for this field",
            ["col3_title"]   = "Col3",
            ["col3_content"] = "Short content that meets the minimum length requirement for this field",
            ["notes"]        = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "title" && i.Message.Contains("char limit"));
    }

    // ── Tightened divider subtitle limits ────────────────────────────────────

    [Fact]
    public void Section_SubtitleExceeds60Chars_ProducesWarning()
    {
        // section subtitle limit reduced from 100 to 60
        var spec = Wrap(MakeSlide("section", new()
        {
            ["title"]    = "SECTION",
            ["subtitle"] = new string('x', 61),  // over 60
            ["notes"]    = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "subtitle" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    [Fact]
    public void Section_SubtitleWithin60Chars_NoWarning()
    {
        var spec = Wrap(MakeSlide("section", new()
        {
            ["title"]    = "SECTION",
            ["subtitle"] = new string('x', 60),  // exactly at limit
            ["notes"]    = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "subtitle" && i.Message.Contains("char limit"));
    }

    [Fact]
    public void ChapterParticle_SubtitleExceeds60Chars_ProducesWarning()
    {
        var spec = Wrap(MakeSlide("chapter_particle", new()
        {
            ["title"]    = "CHAPTER",
            ["subtitle"] = new string('x', 61),
            ["notes"]    = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "subtitle" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    [Fact]
    public void Section_TitleExceeds40Chars_ProducesWarning()
    {
        // section title limit reduced from 60 to 40
        var spec = Wrap(MakeSlide("section", new()
        {
            ["title"] = new string('X', 41),  // over 40
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "title" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    // ── Title line-wrap check ───────────────────────────────────────────────

    [Fact]
    public void Content_TitleWrapsTo2Lines_ProducesLineWrapWarning()
    {
        // 35 chars at ~30 chars/line = 2 lines
        var spec = Wrap(MakeSlide("content", new()
        {
            ["title"] = "ThoughtSpot AI Analytics Overview",  // 33 chars → 2 lines at 30 cpl
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        var warning = result.Warnings.FirstOrDefault(w =>
            w.Field == "title" && w.Message.Contains("may wrap to"));
        Assert.NotNull(warning);
    }

    [Fact]
    public void Content_ShortTitle_NoLineWrapWarning()
    {
        var spec = Wrap(MakeSlide("content", new()
        {
            ["title"] = "Short Title Here",  // 16 chars → 1 line
            ["notes"] = new string('x', 250),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Warnings, w =>
            w.Field == "title" && w.Message.Contains("may wrap to"));
    }

    // ── Title headshot subtitle limit ───────────────────────────────────────

    [Fact]
    public void TitleHeadshot_SubtitleExceeds50Chars_ProducesWarning()
    {
        var spec = Wrap(MakeSlide("title_headshot", new()
        {
            ["title_line1"] = "TITLE",
            ["subtitle"]    = new string('x', 51),  // over 50-char headshot limit
        }));

        var result = Validator.ValidateSpec(spec);

        var issue = result.Issues.FirstOrDefault(i =>
            i.Field == "subtitle" && i.Message.Contains("char limit"));
        Assert.NotNull(issue);
    }

    [Fact]
    public void TitleHeadshot_SubtitleWithin50Chars_NoWarning()
    {
        var spec = Wrap(MakeSlide("title_headshot", new()
        {
            ["title_line1"] = "TITLE",
            ["subtitle"]    = new string('x', 50),
        }));

        var result = Validator.ValidateSpec(spec);

        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "subtitle" && i.Message.Contains("char limit"));
    }

    // ── Agenda scalar check fix ─────────────────────────────────────────────

    [Fact]
    public void Agenda_ItemsListDoesNotTriggerScalarLengthCheck()
    {
        // Previously, the serialized list representation would trigger a false
        // positive "exceeds 40 char limit" warning on the "items" field.
        var slide = new SlideSpec();
        var items = new List<object?> { "Introduction", "Main Content", "Summary", "Next Steps" };
        slide.RawInternal = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"]  = "agenda",
            ["items"] = items,
        };

        var spec = Wrap(slide);
        var result = Validator.ValidateSpec(spec);

        // Should NOT have any "exceeds 40 char limit" warning on "items" field
        Assert.DoesNotContain(result.Issues, i =>
            i.Field == "items" && i.Message.Contains("char limit for agenda"));
    }
}
