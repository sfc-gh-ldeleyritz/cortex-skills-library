using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SnowflakePptx.Schema;

// ── Top-level presentation spec ───────────────────────────────────────────

/// <summary>
/// Root of a deserialized YAML presentation spec.
/// </summary>
public sealed class PresentationSpec
{
    [YamlMember(Alias = "presentation")]
    public PresentationMeta Presentation { get; set; } = new();

    [YamlMember(Alias = "slides")]
    public List<SlideSpec> Slides { get; set; } = new();

    /// <summary>
    /// Vertical spacing multiplier between title and subtitle lines.
    /// Defaults to 1.5 (matches Python schema default).
    /// </summary>
    [YamlMember(Alias = "title_subtitle_spacing")]
    public float TitleSubtitleSpacing { get; set; } = 1.5f;
}

// ── Presentation metadata ─────────────────────────────────────────────────

/// <summary>
/// Presentation-level metadata block (the "presentation:" key).
/// </summary>
public sealed class PresentationMeta
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    [YamlMember(Alias = "author")]
    public string Author { get; set; } = string.Empty;
}

// ── Slide spec ────────────────────────────────────────────────────────────

/// <summary>
/// Represents a single slide in the spec.
///
/// Because different slide types carry very different fields, the entire
/// YAML mapping is captured as a <see cref="Dictionary{TKey,TValue}"/>.
/// The <see cref="Type"/> property surfaces the "type" key directly for
/// convenience.  All remaining fields are accessible via
/// <see cref="Content"/> or the typed helper methods.
/// </summary>
public sealed class SlideSpec
{
    // Internal backing store — populated by the deserializer.
    private Dictionary<string, object?> _raw = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The slide type string (e.g. "title", "content", "two_column").
    /// </summary>
    [YamlIgnore]
    public string Type => GetString("type") ?? string.Empty;

    /// <summary>
    /// All fields from the YAML mapping (including "type").
    /// </summary>
    [YamlIgnore]
    public IReadOnlyDictionary<string, object?> Content => _raw;

    // Exposed for the deserializer to populate.
    internal Dictionary<string, object?> RawInternal
    {
        get => _raw;
        set => _raw = value ?? new(StringComparer.OrdinalIgnoreCase);
    }

    // ── Typed helper accessors ────────────────────────────────────────────

    /// <summary>Returns the string value for <paramref name="key"/>, or null.</summary>
    public string? GetString(string key)
    {
        if (_raw.TryGetValue(key, out var val))
            return val?.ToString();
        return null;
    }

    /// <summary>Returns the string value for <paramref name="key"/>, or <paramref name="defaultValue"/>.</summary>
    public string GetString(string key, string defaultValue) =>
        GetString(key) ?? defaultValue;

    /// <summary>Returns a list of strings for <paramref name="key"/>, or an empty list.</summary>
    public List<string> GetStringList(string key)
    {
        if (!_raw.TryGetValue(key, out var val) || val is null)
            return new List<string>();

        if (val is List<object?> objList)
            return objList.Select(o => o?.ToString() ?? string.Empty).ToList();

        if (val is List<string> strList)
            return strList;

        return new List<string>();
    }

    /// <summary>
    /// Returns a list of raw objects for <paramref name="key"/> (e.g. agenda items
    /// which may be strings or dicts).  Returns an empty list if absent.
    /// </summary>
    public List<object?> GetList(string key)
    {
        if (!_raw.TryGetValue(key, out var val) || val is null)
            return new List<object?>();

        if (val is List<object?> objList)
            return objList;

        return new List<object?>();
    }

    /// <summary>Returns true if the key is present and non-null.</summary>
    public bool HasKey(string key) =>
        _raw.TryGetValue(key, out var val) && val is not null;

    /// <summary>
    /// Returns a nested dictionary for <paramref name="key"/>, or null.
    /// Useful for structured sub-objects (e.g. speaker entries).
    /// </summary>
    public Dictionary<string, object?>? GetDict(string key)
    {
        if (!_raw.TryGetValue(key, out var val))
            return null;
        return val as Dictionary<string, object?>;
    }
}

// ── Deserialization factory ───────────────────────────────────────────────

/// <summary>
/// Factory for building a YamlDotNet deserializer that correctly handles
/// <see cref="PresentationSpec"/> and its dynamic <see cref="SlideSpec"/> list.
///
/// Usage:
/// <code>
/// var spec = SlideSpecDeserializer.Deserialize(yamlText);
/// </code>
/// </summary>
public static class SlideSpecDeserializer
{
    /// <summary>
    /// Deserializes a YAML string into a <see cref="PresentationSpec"/>.
    ///
    /// Slide entries are deserialized as raw <c>Dictionary&lt;object, object&gt;</c>
    /// and then wrapped into <see cref="SlideSpec"/> instances so that every
    /// snake_case field is accessible regardless of slide type.
    /// </summary>
    public static PresentationSpec Deserialize(string yaml)
    {
        // Deserialize the whole document as a plain object graph.
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var root = deserializer.Deserialize<Dictionary<object, object?>>(yaml)
                   ?? new Dictionary<object, object?>();

        var spec = new PresentationSpec();

        if (root.TryGetValue("presentation", out var presObj) &&
            presObj is Dictionary<object, object?> presDict)
        {
            spec.Presentation.Title  = GetStr(presDict, "title");
            spec.Presentation.Author = GetStr(presDict, "author");
        }

        if (root.TryGetValue("title_subtitle_spacing", out var spacingObj) &&
            spacingObj is not null &&
            float.TryParse(
                spacingObj.ToString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var spacing))
        {
            spec.TitleSubtitleSpacing = spacing;
        }

        if (root.TryGetValue("slides", out var slidesObj) &&
            slidesObj is List<object?> slideList)
        {
            foreach (var item in slideList)
            {
                if (item is Dictionary<object, object?> rawSlide)
                    spec.Slides.Add(BuildSlideSpec(rawSlide));
            }
        }

        return spec;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static SlideSpec BuildSlideSpec(Dictionary<object, object?> raw)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in raw)
        {
            var key = kvp.Key?.ToString() ?? string.Empty;
            dict[key] = NormalizeValue(kvp.Value);
        }

        var slide = new SlideSpec();
        slide.RawInternal = dict;
        return slide;
    }

    /// <summary>
    /// Recursively converts YamlDotNet's <c>Dictionary&lt;object,object?&gt;</c>
    /// and <c>List&lt;object?&gt;</c> nodes into string-keyed equivalents so that
    /// downstream code can always use <c>Dictionary&lt;string,object?&gt;</c>.
    /// </summary>
    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            Dictionary<object, object?> d =>
                d.ToDictionary(
                    kv => kv.Key?.ToString() ?? string.Empty,
                    kv => NormalizeValue(kv.Value),
                    StringComparer.OrdinalIgnoreCase),

            List<object?> list =>
                list.Select(NormalizeValue).ToList(),

            _ => value
        };
    }

    private static string GetStr(Dictionary<object, object?> dict, string key)
    {
        if (dict.TryGetValue(key, out var val))
            return val?.ToString() ?? string.Empty;
        return string.Empty;
    }
}
