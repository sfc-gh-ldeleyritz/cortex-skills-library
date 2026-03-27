using DocumentFormat.OpenXml.Validation;

namespace SnowflakePptx.Schema;

// ── Result types ────────────────────────────────────────────────────────────

/// <summary>Severity level of a single validation issue.</summary>
public enum Severity { Warning, Error }

/// <summary>A single validation issue found during spec or file validation.</summary>
public sealed record ValidationIssue(
    int      SlideIndex,
    string   Field,
    string   Message,
    Severity Severity = Severity.Warning)
{
    /// <summary>Backward-compatible string accessor.</summary>
    public string SeverityString => Severity == Severity.Error ? "error" : "warning";
}

/// <summary>Aggregated result of YAML spec validation.</summary>
public sealed class ValidationResult
{
    private readonly List<ValidationIssue> _issues = new();

    /// <summary>All issues in the order they were added.</summary>
    public IReadOnlyList<ValidationIssue> Issues => _issues;

    /// <summary>True when there are no error-severity issues.</summary>
    public bool IsValid => _issues.All(i => i.Severity != Severity.Error);

    /// <summary>Error-severity issues only.</summary>
    public IEnumerable<ValidationIssue> Errors =>
        _issues.Where(i => i.Severity == Severity.Error);

    /// <summary>Warning-severity issues only.</summary>
    public IEnumerable<ValidationIssue> Warnings =>
        _issues.Where(i => i.Severity == Severity.Warning);

    internal void AddError(int slideIndex, string field, string message) =>
        _issues.Add(new ValidationIssue(slideIndex, field, message, Severity.Error));

    internal void AddWarning(int slideIndex, string field, string message) =>
        _issues.Add(new ValidationIssue(slideIndex, field, message, Severity.Warning));
}

// ── Content limits ───────────────────────────────────────────────────────────

/// <summary>Character and item limits for a single content field.</summary>
internal sealed record FieldLimit(
    int? MaxLength    = null,
    int? MinLength    = null,
    int? MaxItems     = null,
    int? MaxLines     = null,
    int  CharsPerLine = 0);

/// <summary>Content limits keyed by logical field name within a slide type.</summary>
internal sealed class SlideTypeLimits
{
    private readonly Dictionary<string, FieldLimit> _fields;

    internal SlideTypeLimits(Dictionary<string, FieldLimit> fields)
        => _fields = fields;

    internal FieldLimit? GetLimit(string field)
        => _fields.TryGetValue(field, out var l) ? l : null;

    internal IEnumerable<KeyValuePair<string, FieldLimit>> Fields => _fields;
}

// ── Spec validator ───────────────────────────────────────────────────────────

/// <summary>
/// Validates a <see cref="PresentationSpec"/> and, optionally, a rendered PPTX
/// file using the OpenXML SDK validator.
/// Ported from Python schema.py and validator.py.
/// </summary>
public static class Validator
{
    private const int NotesMaxLength       = 4000;
    private const int NotesMinLength       = 200;
    private const int MaxSlideCountWarning = 40;
    private const int MaxSlideCountError   = 50;

    // Maximum table rows before a density warning is emitted.
    private const int MaxTableRows = 5;

    private static readonly Dictionary<string, SlideTypeLimits> ContentLimits =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Title slides
            ["title"] = new(new()
            {
                ["line1"]    = new(MaxLength: 40),
                ["line2"]    = new(MaxLength: 40),
                ["subtitle"] = new(MaxLength: 80),
                ["date"]     = new(MaxLength: 30),
            }),
            ["title_wave"] = new(new()
            {
                ["title"]       = new(MaxLength: 40),
                ["subtitle"]    = new(MaxLength: 80),
                ["attribution"] = new(MaxLength: 60),
            }),
            ["title_customer_logo"] = new(new()
            {
                ["title"]       = new(MaxLength: 60),
                ["subtitle"]    = new(MaxLength: 80),
                ["attribution"] = new(MaxLength: 60),
            }),
            ["title_headshot"] = new(new()
            {
                ["line1"]    = new(MaxLength: 40),
                ["line2"]    = new(MaxLength: 40),
                ["subtitle"] = new(MaxLength: 50),   // headshot reduces available width
                ["date"]     = new(MaxLength: 30),
            }),

            // Section / Chapter — dividers are visual breaks; keep text punchy
            ["section"] = new(new()
            {
                ["title"]    = new(MaxLength: 40),
                ["subtitle"] = new(MaxLength: 60),
            }),
            ["chapter_particle"] = new(new()
            {
                ["title"]    = new(MaxLength: 40),
                ["subtitle"] = new(MaxLength: 60),
            }),

            // Content — title wraps at ~40-45 chars in the large title font
            ["content"] = new(new()
            {
                ["title"]   = new(MaxLength: 45),
                ["bullets"] = new(MaxLength: 120, MaxItems: 6),
            }),

            // Two column titled
            ["two_column_titled"] = new(new()
            {
                ["title"]       = new(MaxLength: 50),
                ["col_title"]   = new(MaxLength: 40),
                ["col_content"] = new(MaxLength: 200),
            }),

            // Three column
            ["three_column_titled"] = new(new()
            {
                ["title"]       = new(MaxLength: 50),
                ["col_title"]   = new(MaxLength: 25),
                ["col_content"] = new(MaxLength: 120, MinLength: 50, MaxLines: 5, CharsPerLine: 28),
            }),
            ["three_column_icons"] = new(new()
            {
                ["title"]       = new(MaxLength: 50),
                ["col_title"]   = new(MaxLength: 25),
                ["col_content"] = new(MaxLength: 100, MinLength: 50, MaxLines: 4, CharsPerLine: 25),
            }),

            // Four column
            ["four_column_numbers"] = new(new()
            {
                ["title"]       = new(MaxLength: 50),
                ["number"]      = new(MaxLength: 7),
                ["col_content"] = new(MaxLength: 90, MinLength: 30, MaxLines: 3, CharsPerLine: 23),
            }),
            ["four_column_icons"] = new(new()
            {
                ["title"]       = new(MaxLength: 50),
                ["col_title"]   = new(MaxLength: 20),
                ["col_content"] = new(MaxLength: 80, MinLength: 40, MaxLines: 4, CharsPerLine: 18),
            }),

            // Split -- title is half-width (~50% of slide), so limit is stricter
            ["split"] = new(new()
            {
                ["title"]         = new(MaxLength: 30),   // was 60 -- split titles are half-width
                ["content_title"] = new(MaxLength: 50),
                ["content"]       = new(MaxLength: 300),
                ["items"]         = new(MaxLength: 80, MaxItems: 5),
            }),

            // Quote
            ["quote"] = new(new()
            {
                ["quote_text"]  = new(MaxLength: 200),
                ["attribution"] = new(MaxLength: 80),
            }),
            ["quote_photo"] = new(new()
            {
                ["quote_text"]  = new(MaxLength: 200),
                ["attribution"] = new(MaxLength: 80),
            }),
            ["quote_simple"] = new(new()
            {
                ["quote_text"]  = new(MaxLength: 200),
                ["attribution"] = new(MaxLength: 40, MaxLines: 1, CharsPerLine: 40),
            }),

            // Agenda
            ["agenda"] = new(new()
            {
                ["items"]       = new(MaxLength: 40, MaxItems: 6),
                ["description"] = new(MaxLength: 200),
            }),

            // Tables
            ["table_styled"] = new(new()
            {
                ["title"]    = new(MaxLength: 80),
                ["subtitle"] = new(MaxLength: 80),
            }),
            ["table_striped"] = new(new()
            {
                ["title"]    = new(MaxLength: 80),
                ["subtitle"] = new(MaxLength: 80),
            }),

            // Empty limits (still valid types)
            ["safe_harbor"] = new(new()),
            ["thank_you"]   = new(new()),
        };

    // Slide types that should appear at most once.
    private static readonly HashSet<string> SingletonTypes =
        new(StringComparer.OrdinalIgnoreCase) { "title", "title_headshot", "safe_harbor", "thank_you" };

    // Slide types that benefit from speaker notes.
    private static readonly HashSet<string> ContentSlideTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "content", "two_column_titled", "three_column_titled",
            "three_column_icons", "four_column_numbers", "four_column_icons", "split",
        };

    // Slide types where subtitle should always be included (template has subtitle placeholder).
    private static readonly HashSet<string> SubtitleRequiredTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "content", "two_column_titled",
            "three_column_titled", "three_column_icons", "four_column_numbers",
            "four_column_icons", "split", "quote", "quote_photo",
        };

    // Fields where exceeding the character limit causes visible overflow (not just aesthetics).
    // Violations in these fields are promoted from warnings to errors.
    private static readonly HashSet<string> OverflowCriticalFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "col_content", "col_title", "col_number", "quote_text", "content",
        };

    // ── Variety enforcement: category definitions ────────────────────────

    // Categories of variable content slides for variety checking.
    // Structural bookends (title, safe_harbor, agenda, thank_you) and
    // dividers (section, chapter_particle) are excluded.
    private static readonly Dictionary<string, string[]> VarietyCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["column"] = new[] { "two_column_titled", "three_column_titled", "three_column_icons",
                                 "four_column_numbers", "four_column_icons" },
            ["quote"]  = new[] { "quote", "quote_photo", "quote_simple" },
            ["table"]  = new[] { "table_styled", "table_striped" },
            ["visual"] = new[] { "split" },
            ["bullet"] = new[] { "content" },
        };

    // Types excluded from variety counting (structural bookends, dividers, special).
    private static readonly HashSet<string> StructuralTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "title", "title_headshot", "title_wave", "title_customer_logo",
            "safe_harbor", "agenda", "thank_you",
            "section", "chapter_particle",
            "speaker_headshots",
        };

    // Divider types (reset consecutive-run tracking).
    private static readonly HashSet<string> DividerTypes =
        new(StringComparer.OrdinalIgnoreCase) { "section", "chapter_particle" };

    // Minimum variable-slide count before a category is expected to appear.
    private static readonly Dictionary<string, int> CategoryThresholds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["column"] = 4,
            ["quote"]  = 6,
            ["table"]  = 8,
            ["visual"] = 8,
        };

    // ── Public: YAML spec validation ─────────────────────────────────────

    /// <summary>
    /// Validates a deserialized <see cref="PresentationSpec"/> against the schema.
    ///
    /// Checks:
    /// <list type="bullet">
    ///   <item>Non-empty slides list.</item>
    ///   <item>Each slide has a recognised type.</item>
    ///   <item>Singleton types (title, safe_harbor, thank_you) appear at most once.</item>
    ///   <item>Content length and list-item count limits per slide type.</item>
    ///   <item>Column field limits (col1_*, col2_*, ...).</item>
    ///   <item>Speaker notes length.</item>
    ///   <item>Notes presence on content slides (warning only).</item>
    ///   <item>Structural rules: first slide should be a title variant; last should be thank_you.</item>
    ///   <item>Total slide count sanity (warn &gt;=40, error &gt;=50).</item>
    ///   <item>Variety: content-type 30% cap, category coverage, repetition before coverage,
    ///          consecutive same-type, intra-category variety, divider variety.</item>
    /// </list>
    /// </summary>
    public static ValidationResult ValidateSpec(PresentationSpec spec)
    {
        var result = new ValidationResult();
        var slides = spec.Slides;

        if (slides.Count == 0)
        {
            result.AddError(-1, "slides", "'slides' list is empty.");
            return result;
        }

        // Slide count sanity
        if (slides.Count >= MaxSlideCountError)
            result.AddError(-1, "slides",
                $"Presentation has {slides.Count} slides (max {MaxSlideCountError}). " +
                "Consider splitting into multiple decks.");
        else if (slides.Count >= MaxSlideCountWarning)
            result.AddWarning(-1, "slides",
                $"Presentation has {slides.Count} slides; consider splitting into multiple decks.");

        var seenSingletons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int idx = 0; idx < slides.Count; idx++)
        {
            var slide     = slides[idx];
            var slideType = slide.Type;

            // Validate slide type
            if (!TemplateMappings.AllTypes.Contains(slideType))
            {
                result.AddError(idx, "type",
                    $"Unknown slide type '{slideType}'. " +
                    $"Valid types: {string.Join(", ", TemplateMappings.AllTypes.OrderBy(t => t))}");
                continue; // cannot validate limits without a known type
            }

            // Singleton duplicate check
            if (SingletonTypes.Contains(slideType))
            {
                if (seenSingletons.TryGetValue(slideType, out int firstIdx))
                    result.AddWarning(idx, "type",
                        $"Duplicate singleton slide type '{slideType}' " +
                        $"(first appeared at index {firstIdx}).");
                else
                    seenSingletons[slideType] = idx;
            }

            // Content limits
            if (ContentLimits.TryGetValue(slideType, out var limits))
                ValidateSlideLimits(idx, slide, slideType, limits, result);

            // Agenda item format validation
            if (string.Equals(slideType, "agenda", StringComparison.OrdinalIgnoreCase))
            {
                var agendaItems = slide.GetList("items");
                if (agendaItems.Count > 0)
                    CheckAgendaItems(idx, agendaItems, result);
            }

            // Divider slide capitalization warning
            if (slideType is "section" or "section_dots" or "chapter_particle")
            {
                var dividerTitle = slide.GetString("title");
                if (!string.IsNullOrEmpty(dividerTitle) && dividerTitle != dividerTitle.ToUpperInvariant())
                    result.AddWarning(idx, "title",
                        $"Divider slide titles are auto-uppercased at render time. " +
                        $"Consider writing '{dividerTitle}' in ALL CAPS in the spec for clarity.");
            }

            // Notes length
            var notes = slide.GetString("notes");
            if (!string.IsNullOrEmpty(notes) && notes.Length > NotesMaxLength)
                result.AddWarning(idx, "notes",
                    $"Speaker notes ({notes.Length} chars) exceed {NotesMaxLength} char limit.");

            // Suggest notes on content slides; warn when notes are present but too thin
            if (ContentSlideTypes.Contains(slideType))
            {
                if (string.IsNullOrWhiteSpace(notes))
                    result.AddWarning(idx, "notes", "Content slide has no speaker notes.");
                else if (notes.Length < NotesMinLength)
                    result.AddWarning(idx, "notes",
                        $"Speaker notes ({notes.Length} chars) are very short (< {NotesMinLength} chars). " +
                        "Add talking points and a References section for content slides.");
            }

            // Require subtitle on slide types with subtitle placeholders
            if (SubtitleRequiredTypes.Contains(slideType) &&
                string.IsNullOrWhiteSpace(slide.GetString("subtitle")))
                result.AddWarning(idx, "subtitle",
                    $"'{slideType}' slide should include a subtitle (template has subtitle placeholder).");

            // Title line-wrap check: warn when a content slide title would wrap
            // to 2+ lines at the title font size (~30 chars/line).
            if (ContentSlideTypes.Contains(slideType))
            {
                var titleVal = slide.GetString("title");
                if (titleVal is not null)
                {
                    var titleLines = EstimateLineCount(titleVal, 30);
                    if (titleLines > 1)
                        result.AddWarning(idx, "title",
                            $"Title ({titleVal.Length} chars) may wrap to {titleLines} lines " +
                            "and overlap the subtitle (~30 chars/line at title font size).");
                }
            }

            // Table row density: warn when table has too many rows
            if (string.Equals(slideType, "table_styled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(slideType, "table_striped", StringComparison.OrdinalIgnoreCase))
            {
                var tableDict = slide.GetDict("table");
                if (tableDict is not null &&
                    tableDict.TryGetValue("rows", out var rowsObj) &&
                    rowsObj is List<object?> rows &&
                    rows.Count > MaxTableRows)
                {
                    result.AddWarning(idx, "table.rows",
                        $"Table has {rows.Count} rows (max recommended {MaxTableRows}). " +
                        "Dense tables are hard to read on slides.");
                }
            }

            // Code-block detection on split slides: markdown fences / indented code
            if (string.Equals(slideType, "split", StringComparison.OrdinalIgnoreCase))
            {
                var content = slide.GetString("content");
                if (content is not null && ContainsCodeBlock(content))
                    result.AddWarning(idx, "content",
                        "Split slide content appears to contain a code block (triple-backtick or " +
                        "4-space indent). Code blocks do not render in PPTX; use plain text instead.");
            }
        }

        // Structural rules
        var firstType = slides[0].Type;
        if (!string.Equals(firstType, "title",               StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(firstType, "title_headshot",      StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(firstType, "title_wave",          StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(firstType, "title_customer_logo", StringComparison.OrdinalIgnoreCase))
        {
            result.AddWarning(0, "type", "First slide should be a title slide.");
        }

        if (!string.Equals(slides[^1].Type, "thank_you", StringComparison.OrdinalIgnoreCase))
            result.AddWarning(slides.Count - 1, "type", "Last slide should be 'thank_you'.");

        // ── Slide variety checks ─────────────────────────────────────────

        // Gather variable content slides (non-structural, non-divider).
        var variableSlides = slides
            .Where(s => !StructuralTypes.Contains(s.Type))
            .ToList();
        var variableCount = variableSlides.Count;

        // Count usage per type among variable slides.
        var typeCounts = variableSlides
            .GroupBy(s => s.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Determine which categories are represented.
        var categoryUsed = VarietyCategories.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Any(t => typeCounts.ContainsKey(t)),
            StringComparer.OrdinalIgnoreCase);

        // Check 1: Content-type cap — warn if 'content' exceeds 30% of total slides.
        if (typeCounts.TryGetValue("content", out var contentCount) && slides.Count >= 6)
        {
            var pct = (double)contentCount / slides.Count * 100;
            if (pct > 30)
                result.AddWarning(-1, "variety",
                    $"Slide variety: 'content' type used {contentCount}/{slides.Count} times " +
                    $"({pct:F0}%) — exceeds 30% guideline. " +
                    "Use column layouts, split, quote, or table types for visual variety.");
        }

        // Check 2: Category coverage — warn when a category is absent in a deck
        // large enough to warrant it.
        foreach (var (category, threshold) in CategoryThresholds)
        {
            if (variableCount >= threshold && !categoryUsed[category])
            {
                var suggestions = string.Join(", ", VarietyCategories[category]);
                result.AddWarning(-1, "variety",
                    $"No '{category}' slide in a deck with {variableCount} content slides. " +
                    $"Consider adding one of: {suggestions}.");
            }
        }

        // Check 3: Repetition before coverage — warn when any type is reused while
        // applicable categories remain completely absent.
        var emptyApplicableCategories = CategoryThresholds
            .Where(kvp => variableCount >= kvp.Value && !categoryUsed[kvp.Key])
            .Select(kvp => kvp.Key)
            .ToList();

        if (emptyApplicableCategories.Count > 0)
        {
            foreach (var (type, count) in typeCounts.Where(kvp => kvp.Value >= 2))
            {
                result.AddWarning(-1, "variety",
                    $"'{type}' used {count} times while these categories have no slides: " +
                    $"{string.Join(", ", emptyApplicableCategories)}. " +
                    "Replace a duplicate with a type from an unused category.");
            }
        }

        // Check 4: Generalized consecutive same-type — warn on 2+ consecutive
        // slides of the same non-divider type. Dividers reset the run.
        string? prevType = null;
        int consecutiveRun = 0;
        for (int i = 0; i < slides.Count; i++)
        {
            var t = slides[i].Type;
            if (DividerTypes.Contains(t))
            {
                prevType = null;
                consecutiveRun = 0;
                continue;
            }

            if (string.Equals(t, prevType, StringComparison.OrdinalIgnoreCase))
            {
                consecutiveRun++;
                if (consecutiveRun >= 2)
                    result.AddWarning(i, "variety",
                        $"Slide {i + 1} is the {consecutiveRun + 1}th consecutive '{t}' slide. " +
                        "Use a different slide type for visual variety.");
            }
            else
            {
                prevType = t;
                consecutiveRun = 1;
            }
        }

        // Check 5: Intra-category variety — within categories that have 3+ members,
        // warn when a specific type is used 2+ times while sibling types are unused.
        foreach (var (category, members) in VarietyCategories)
        {
            if (members.Length < 3) continue; // skip small categories

            var usedMembers = members.Where(m => typeCounts.ContainsKey(m)).ToList();
            var unusedMembers = members.Where(m => !typeCounts.ContainsKey(m)).ToList();

            if (unusedMembers.Count > 0)
            {
                foreach (var member in usedMembers)
                {
                    if (typeCounts[member] >= 2)
                    {
                        result.AddWarning(-1, "variety",
                            $"'{member}' used {typeCounts[member]} times in the '{category}' category " +
                            $"while these sibling types are unused: {string.Join(", ", unusedMembers)}. " +
                            "Try a different type from the same category before repeating.");
                    }
                }
            }
        }

        // Check 6: Divider variety — if 3+ dividers are all the same type, suggest mixing.
        var dividerSlides = slides.Where(s => DividerTypes.Contains(s.Type)).ToList();
        if (dividerSlides.Count >= 3)
        {
            var dividerTypeCounts = dividerSlides
                .GroupBy(s => s.Type, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            if (dividerTypeCounts.Count == 1)
            {
                var usedType = dividerTypeCounts.Keys.First();
                var otherType = string.Equals(usedType, "section", StringComparison.OrdinalIgnoreCase)
                    ? "chapter_particle" : "section";
                result.AddWarning(-1, "variety",
                    $"All {dividerSlides.Count} dividers use '{usedType}'. " +
                    $"Mix in '{otherType}' for visual variety.");
            }
        }

        return result;
    }

    // ── Public: OpenXML PPTX validation ─────────────────────────────────

    /// <summary>
    /// Validates a rendered PPTX file using the OpenXML SDK structural validator.
    /// Returns a list of error description strings (empty list means no errors).
    /// </summary>
    /// <param name="pptxPath">Absolute path to the .pptx file.</param>
    public static IReadOnlyList<string> ValidatePptx(string pptxPath)
    {
        var errors = new List<string>();

        using var doc = DocumentFormat.OpenXml.Packaging.PresentationDocument
            .Open(pptxPath, isEditable: false);

        var sdkValidator = new OpenXmlValidator();

        foreach (var error in sdkValidator.Validate(doc))
        {
            errors.Add(
                $"[{error.ErrorType}] {error.Description} " +
                $"(Path: {error.Path?.XPath ?? "n/a"})");
        }

        return errors;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static void ValidateSlideLimits(
        int              idx,
        SlideSpec        slide,
        string           slideType,
        SlideTypeLimits  limits,
        ValidationResult result)
    {
        foreach (var (fieldName, limit) in limits.Fields)
        {
            // Direct named field — but skip scalar check when the field is actually
            // a list (e.g. agenda "items") to avoid false positives from the
            // serialized list representation.
            var directValue = slide.GetString(fieldName);
            var fieldIsList = slide.GetList(fieldName).Count > 0;
            if (directValue is not null && !fieldIsList)
            {
                CheckStringLength(idx, fieldName, directValue, limit, slideType, result);

                // R1: Split title word-length heuristic — a single long word forces wrapping
                // even when total character count is within the 30-char limit.
                if (fieldName == "title" &&
                    string.Equals(slideType, "split", StringComparison.OrdinalIgnoreCase))
                {
                    var longestWord = directValue.Split(' ').Max(w => w.Length);
                    if (longestWord > 15)
                        result.AddWarning(idx, fieldName,
                            $"Split title contains a {longestWord}-char word that may force " +
                            "line wrapping (half-width title box).");
                }
            }

            // List fields (bullets, items, etc.)
            if (slide.HasKey(fieldName))
            {
                var list = slide.GetList(fieldName);
                if (list.Count > 0)
                    CheckListField(idx, fieldName, list, limit, slideType, result);
            }

            // Column-based expansion: col_title -> col1_title, col2_title, ...
            if (fieldName.StartsWith("col_", StringComparison.OrdinalIgnoreCase))
            {
                var baseName = fieldName[4..]; // "title", "content", or "number"
                for (int colNum = 1; colNum <= 4; colNum++)
                {
                    var colKey   = $"col{colNum}_{baseName}";
                    var colValue = slide.GetString(colKey);
                    if (colValue is not null)
                    {
                        CheckStringLength(idx, colKey, colValue, limit, slideType, result);

                        // R2: Line count estimation for narrow column text boxes
                        if (limit.MaxLines.HasValue && limit.CharsPerLine > 0)
                        {
                            var lineCount = EstimateLineCount(colValue, limit.CharsPerLine);
                            if (lineCount > limit.MaxLines.Value)
                                result.AddWarning(idx, colKey,
                                    $"'{colKey}': estimated {lineCount} lines exceeds " +
                                    $"{limit.MaxLines.Value}-line maximum for {slideType} " +
                                    $"(~{limit.CharsPerLine} chars/line). " +
                                    "Shorten to prevent vertical overflow.");
                        }
                    }
                }
            }

            // Columns array format: [{title:..., content:...}, ...]
            var columns = slide.GetList("columns");
            if (columns.Count > 0)
                CheckColumnsArray(idx, columns, limits, slideType, result);
        }
    }

    private static void CheckStringLength(
        int idx, string field, string value,
        FieldLimit limit, string slideType, ValidationResult result)
    {
        if (limit.MaxLength.HasValue && value.Length > limit.MaxLength.Value)
        {
            var msg =
                $"'{field}' ({value.Length} chars) exceeds " +
                $"{limit.MaxLength.Value} char limit for {slideType}.";

            // Normalize colN_content → col_content, colN_title → col_title, etc.
            var normalizedField = NormalizeColFieldName(field);
            var isOverflowCritical = OverflowCriticalFields.Contains(field) ||
                                     OverflowCriticalFields.Contains(normalizedField);
            if (isOverflowCritical)
                result.AddError(idx, field, msg);
            else
                result.AddWarning(idx, field, msg);
        }

        // MinLength check — warn when content is too short (sparse slides)
        if (limit.MinLength.HasValue && value.Length > 0 && value.Length < limit.MinLength.Value)
        {
            result.AddWarning(idx, field,
                $"'{field}' ({value.Length} chars) is below the recommended " +
                $"{limit.MinLength.Value} char minimum for {slideType}. " +
                "Add more detail to avoid sparse-looking slides.");
        }
    }

    private static void CheckListField(
        int idx, string field, List<object?> list,
        FieldLimit limit, string slideType, ValidationResult result)
    {
        if (limit.MaxItems.HasValue && list.Count > limit.MaxItems.Value)
            result.AddWarning(idx, field,
                $"'{field}' has {list.Count} items; max {limit.MaxItems.Value} for {slideType}.");

        if (limit.MaxLength.HasValue)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var itemText = list[i] is Dictionary<string, object?> d
                    ? (d.TryGetValue("text", out var t) ? t?.ToString() ?? string.Empty : string.Empty)
                    : list[i]?.ToString() ?? string.Empty;

                if (itemText.Length > limit.MaxLength.Value)
                    result.AddWarning(idx, $"{field}[{i}]",
                        $"Item ({itemText.Length} chars) exceeds {limit.MaxLength.Value} char limit.");
            }
        }
    }

    /// <summary>
    /// Returns true when <paramref name="text"/> contains markdown-style code
    /// blocks (triple-backtick fences) or lines indented with 4+ leading spaces
    /// that look like code.
    /// </summary>
    private static bool ContainsCodeBlock(string text)
    {
        if (text.Contains("```"))
            return true;

        // Check for 4-space / tab indented lines (common markdown code pattern).
        // We require at least two consecutive indented lines to avoid false positives.
        int consecutiveIndented = 0;
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith("    ") || line.StartsWith("\t"))
            {
                consecutiveIndented++;
                if (consecutiveIndented >= 2)
                    return true;
            }
            else
            {
                consecutiveIndented = 0;
            }
        }

        return false;
    }

    private static void CheckColumnsArray(
        int idx, List<object?> columns,
        SlideTypeLimits limits, string slideType, ValidationResult result)
    {
        for (int colIdx = 0; colIdx < columns.Count; colIdx++)
        {
            if (columns[colIdx] is not Dictionary<string, object?> col)
                continue;

            foreach (var colField in new[] { "title", "content", "number" })
            {
                if (!col.TryGetValue(colField, out var rawVal) || rawVal is null)
                    continue;

                var colValue = rawVal.ToString() ?? string.Empty;

                // Resolve limit under "col_<field>" first, then plain "<field>"
                var colLimit = limits.GetLimit($"col_{colField}") ?? limits.GetLimit(colField);
                if (colLimit is null) continue;

                if (colLimit.MaxLength.HasValue && colValue.Length > colLimit.MaxLength.Value)
                {
                    var fieldKey         = $"columns[{colIdx}].{colField}";
                    var normalizedField  = $"col_{colField}";
                    var isOverflowCritical = OverflowCriticalFields.Contains(normalizedField);
                    var msg = $"Column {colField} ({colValue.Length} chars) exceeds " +
                              $"{colLimit.MaxLength.Value} char limit for {slideType}.";
                    if (isOverflowCritical)
                        result.AddError(idx, fieldKey, msg);
                    else
                        result.AddWarning(idx, fieldKey, msg);
                }

                // MinLength check for columns array format
                if (colLimit.MinLength.HasValue && colValue.Length > 0 && colValue.Length < colLimit.MinLength.Value)
                {
                    result.AddWarning(idx, $"columns[{colIdx}].{colField}",
                        $"Column {colField} ({colValue.Length} chars) is below the recommended " +
                        $"{colLimit.MinLength.Value} char minimum for {slideType}. " +
                        "Add more detail to avoid sparse-looking slides.");
                }

                // R2: Line count estimation
                if (colLimit.MaxLines.HasValue && colLimit.CharsPerLine > 0)
                {
                    var lineCount = EstimateLineCount(colValue, colLimit.CharsPerLine);
                    if (lineCount > colLimit.MaxLines.Value)
                        result.AddWarning(idx, $"columns[{colIdx}].{colField}",
                            $"Estimated {lineCount} lines exceeds {colLimit.MaxLines.Value}-line " +
                            $"maximum for {slideType} (~{colLimit.CharsPerLine} chars/line). " +
                            "Shorten to prevent vertical overflow.");
                }
            }
        }
    }

    /// <summary>
    /// Estimates the number of rendered lines for <paramref name="text"/> given a
    /// <paramref name="charsPerLine"/> estimate for the column width.
    /// Each explicit newline starts a new paragraph; each paragraph is further
    /// subdivided by word-wrap at <paramref name="charsPerLine"/> characters.
    /// Uses word-wrap simulation: words are not broken mid-word, so a single
    /// long word that exceeds <paramref name="charsPerLine"/> still occupies one line.
    /// </summary>
    internal static int EstimateLineCount(string text, int charsPerLine)
    {
        if (charsPerLine <= 0) return 1;
        int lines = 0;
        foreach (var paragraph in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines++;
                continue;
            }

            int currentLineLen = 0;
            int paraLines = 1;
            foreach (var word in paragraph.Split(' '))
            {
                var wordLen = word.Length;
                if (wordLen == 0) continue;

                if (currentLineLen == 0)
                {
                    // First word on the line — always fits (even if longer than charsPerLine).
                    currentLineLen = wordLen;
                }
                else if (currentLineLen + 1 + wordLen > charsPerLine)
                {
                    // Word doesn't fit — wrap to next line.
                    paraLines++;
                    currentLineLen = wordLen;
                }
                else
                {
                    currentLineLen += 1 + wordLen; // +1 for the space
                }
            }
            lines += paraLines;
        }
        return lines;
    }

    /// <summary>
    /// Normalises a column-numbered field name to its generic form so it can be
    /// looked up in <see cref="OverflowCriticalFields"/>.
    /// Examples: "col1_content" → "col_content", "col3_title" → "col_title".
    /// Non-column fields are returned unchanged.
    /// </summary>
    private static string NormalizeColFieldName(string field)
    {
        // Match colN_... where N is one or more digits
        if (field.Length > 4 &&
            field.StartsWith("col", StringComparison.OrdinalIgnoreCase) &&
            char.IsDigit(field[3]))
        {
            int underscorePos = field.IndexOf('_');
            if (underscorePos > 0)
                return "col_" + field[(underscorePos + 1)..];
        }
        return field;
    }

    /// <summary>
    /// Validates agenda item format: each dict item should use the single-key format
    /// ("Title": {subitems: [...]}) rather than the multi-key format ({title: "...", subitems: [...]}).
    /// The multi-key format is accepted by the builder but is error-prone; warn on it.
    /// </summary>
    private static void CheckAgendaItems(int idx, List<object?> items, ValidationResult result)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is Dictionary<string, object?> d && d.Count != 1)
            {
                // Multi-key dict — accepted but not preferred
                if (d.ContainsKey("title"))
                {
                    result.AddWarning(idx, "items",
                        $"Agenda item {i + 1} uses multi-key dict format ({{title: \"...\", subitems: [...]}}). " +
                        "Prefer single-key format: '\"Title\": {subitems: [...]}'.");
                }
                else
                {
                    result.AddError(idx, "items",
                        $"Agenda item {i + 1} has {d.Count} keys but no 'title' key. " +
                        "Use single-key dict format: '\"Title\": {subitems: [...]}'.");
                }
            }
        }
    }
}
