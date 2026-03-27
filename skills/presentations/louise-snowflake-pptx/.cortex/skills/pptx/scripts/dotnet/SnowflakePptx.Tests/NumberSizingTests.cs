using SnowflakePptx.Formatting;
using Xunit;

namespace SnowflakePptx.Tests;

/// <summary>
/// Tests for NumberSizing threshold tables and font calculation.
/// Covers Issue 9: consolidated number sizing with battle-tested values.
/// </summary>
public class NumberSizingTests
{
    // ── NumberThresholds: battle-tested values ────────────────────────────────

    [Theory]
    [InlineData("5%", 3600)]    // 2 chars → 36pt (<=4)
    [InlineData("$1B", 3600)]   // 3 chars → 36pt (<=4)
    [InlineData("35%", 3600)]   // 3 chars → 36pt (<=4)
    [InlineData("2.5x", 3600)]  // 4 chars → 36pt (<=4)
    [InlineData("$100K", 2800)] // 5 chars → 28pt
    [InlineData("60-70%", 2400)]// 6 chars → 24pt
    [InlineData("100-200", 1800)]// 7 chars → 18pt (<=8)
    [InlineData("100-200%", 1800)]// 8 chars → 18pt (<=8)
    [InlineData("1,000,000", 1400)]// 9 chars → 14pt (>8)
    public void NumberThresholds_CorrectSizeForLength(string text, int expectedHundredths)
    {
        var size = NumberSizing.CalculateFontSizeHundredths(text, "number");
        Assert.Equal(expectedHundredths, size);
    }

    // ── Symmetric sizing picks the minimum ────────────────────────────────────

    [Fact]
    public void SymmetricSizing_PicksSmallestForLongestNumber()
    {
        var texts = new[] { "5%", "$100K", "2x" };
        // "$100K" is 5 chars → 28pt = 2800; others are smaller → 2800 wins (min)
        var size = NumberSizing.CalculateSymmetricFontSizeHundredths(texts, "number");
        Assert.Equal(2800, size);
    }

    [Fact]
    public void SymmetricSizing_AllShortNumbers_Gets36pt()
    {
        var texts = new[] { "5", "10", "$2M", "40%" };
        // All <=4 chars → 36pt = 3600
        var size = NumberSizing.CalculateSymmetricFontSizeHundredths(texts, "number");
        Assert.Equal(3600, size);
    }

    // ── ContentInjector font table: sliding scale (covered via validator) ─────

    [Fact]
    public void NumberThresholds_MatchExpectedValues()
    {
        // Verify the thresholds match the battle-tested values from production
        var t = NumberSizing.NumberThresholds;
        Assert.Equal(5, t.Length);
        Assert.Equal((4, 36), t[0]);
        Assert.Equal((5, 28), t[1]);
        Assert.Equal((6, 24), t[2]);
        Assert.Equal((8, 18), t[3]);
        Assert.Equal((999, 14), t[4]);
    }
}
