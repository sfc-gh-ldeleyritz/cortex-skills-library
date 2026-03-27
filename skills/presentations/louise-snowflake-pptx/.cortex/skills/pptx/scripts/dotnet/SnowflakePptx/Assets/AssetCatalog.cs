using System.Reflection;

namespace SnowflakePptx.Assets;

/// <summary>
/// Locates icon and image assets by keyword matching.
///
/// The catalog scans svg/, png/, jpg/ sub-directories under
/// <see cref="AssetRoot"/>.  Results are ranked by a simple scoring scheme
/// that rewards exact-name matches over substring matches.
/// </summary>
public sealed class AssetCatalog
{
    private static readonly string[] AssetSubdirs = ["svg", "png", "jpg"];

    // Supported image extensions for ResolveImagePath absolute-path check.
    private static readonly string[] ImageExtensions = [".svg", ".png", ".jpg", ".jpeg"];

    /// <summary>Root directory that contains the asset sub-directories.</summary>
    public string AssetRoot { get; }

    /// <summary>
    /// Initialises the catalog.
    /// </summary>
    /// <param name="assetRoot">
    ///   Explicit asset root.  When <see langword="null"/> the default root is
    ///   resolved by <see cref="ResolveDefaultAssetRoot"/>.
    /// </param>
    public AssetCatalog(string? assetRoot = null)
    {
        AssetRoot = assetRoot ?? ResolveDefaultAssetRoot();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches all asset sub-directories for files whose stem matches
    /// <paramref name="keyword"/> (case-insensitive).
    ///
    /// Returns a list of <c>(Score, Path)</c> tuples sorted by descending score:
    /// <list type="bullet">
    ///   <item>100 — exact stem match</item>
    ///   <item> 50 — stem starts with keyword</item>
    ///   <item> 25 — stem contains keyword</item>
    /// </list>
    /// Only entries with score > 0 are returned.
    /// </summary>
    public IReadOnlyList<(int Score, string Path)> FindAsset(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Array.Empty<(int, string)>();

        var needle  = keyword.Trim().ToLowerInvariant();
        var results = new List<(int Score, string Path)>();

        if (!Directory.Exists(AssetRoot))
            return results;

        foreach (var subdir in AssetSubdirs)
        {
            var dir = Path.Combine(AssetRoot, subdir);
            if (!Directory.Exists(dir))
                continue;

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                var stem  = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                var score = Score(stem, needle);
                if (score > 0)
                    results.Add((score, file));
            }
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        return results;
    }

    /// <summary>
    /// Resolves an image reference to an absolute filesystem path.
    ///
    /// <list type="number">
    ///   <item>If <paramref name="imageRef"/> is already an absolute path that
    ///         exists, returns it directly.</item>
    ///   <item>Otherwise tries to locate the file in each asset sub-directory.</item>
    ///   <item>If still not found, falls back to <see cref="FindAsset"/> keyword
    ///         search and returns the top result.</item>
    /// </list>
    /// Returns <see langword="null"/> if nothing is found.
    /// </summary>
    public string? ResolveImagePath(string imageRef)
    {
        if (string.IsNullOrWhiteSpace(imageRef))
            return null;

        // 1. Already absolute and exists?
        if (Path.IsPathRooted(imageRef) && File.Exists(imageRef))
            return imageRef;

        // 2. Try each asset sub-directory with the ref as-is (or as a filename).
        var filename = Path.GetFileName(imageRef);
        if (!string.IsNullOrEmpty(filename))
        {
            foreach (var subdir in AssetSubdirs)
            {
                var candidate = Path.Combine(AssetRoot, subdir, filename);
                if (File.Exists(candidate))
                    return candidate;
            }

            // Also try the ref as a relative path from AssetRoot.
            var fromRoot = Path.Combine(AssetRoot, imageRef);
            if (File.Exists(fromRoot))
                return fromRoot;
        }

        // 3. Keyword search using the stem (strip extension if present).
        var stem    = Path.GetFileNameWithoutExtension(imageRef);
        var keyword = string.IsNullOrEmpty(stem) ? imageRef : stem;
        var hits    = FindAsset(keyword);
        return hits.Count > 0 ? hits[0].Path : null;
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the default asset root relative to the executing assembly.
    ///
    /// Priority order:
    /// <list type="number">
    ///   <item><c>{assemblyDir}/assets/</c></item>
    ///   <item><c>{assemblyDir}/../../snowflake/assets/</c>
    ///         (dev layout: .../dotnet/SnowflakePptx/bin/Debug/net9.0/ →
    ///         .../snowflake/assets/)</item>
    /// </list>
    /// Returns the first existing directory, or the first candidate as a
    /// fallback (even if it does not yet exist).
    /// </summary>
    private static string ResolveDefaultAssetRoot()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                          ?? Directory.GetCurrentDirectory();

        // Candidate 1: assets/ sibling to the executable
        var c1 = Path.GetFullPath(Path.Combine(assemblyDir, "assets"));
        if (Directory.Exists(c1))
            return c1;

        // Candidate 2: navigate up from bin/Debug/net9.0/ through dotnet/SnowflakePptx
        // to the sibling snowflake/assets/ directory.
        //   assemblyDir = .../dotnet/SnowflakePptx/bin/Debug/net9.0
        //   target      = .../snowflake/assets
        try
        {
            var up = assemblyDir;
            for (var i = 0; i < 5; i++)
            {
                var parent = Path.GetDirectoryName(up);
                if (parent is null) break;
                up = parent;

                var sibling = Path.Combine(up, "snowflake", "assets");
                if (Directory.Exists(sibling))
                    return sibling;
            }
        }
        catch
        {
            // Swallow navigation errors and fall through.
        }

        // Fallback: return c1 even though it may not exist yet.
        return c1;
    }

    private static int Score(string stem, string needle)
    {
        if (stem == needle)           return 100;
        if (stem.StartsWith(needle))  return  50;
        if (stem.Contains(needle))    return  25;
        return 0;
    }
}
