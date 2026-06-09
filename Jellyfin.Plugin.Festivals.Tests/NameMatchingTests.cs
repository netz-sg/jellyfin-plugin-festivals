using Jellyfin.Plugin.Festivals.Data;
using Xunit;

namespace Jellyfin.Plugin.Festivals.Tests;

public class NameMatchingTests
{
    [Theory]
    [InlineData("Rock am Ring", "rock am ring")]
    [InlineData("  Rock   am Ring ", "rock am ring")]
    [InlineData("Bring Me The Horizon", "bring me the horizon")]
    [InlineData("Sabaton (Live)", "sabaton live")]
    [InlineData("AC/DC", "acdc")]
    [InlineData("Rock-am-Ring", "rock am ring")]
    public void Normalize_collapses_case_space_and_punctuation(string input, string expected)
    {
        Assert.Equal(expected, NameMatching.Normalize(input));
    }

    [Fact]
    public void Normalize_handles_null_and_empty()
    {
        Assert.Equal(string.Empty, NameMatching.Normalize(null));
        Assert.Equal(string.Empty, NameMatching.Normalize("   "));
    }

    [Fact]
    public void Matches_ignores_case_and_surrounding_space()
    {
        Assert.True(NameMatching.Matches("Sabaton", " sabaton "));
        Assert.False(NameMatching.Matches("Sabaton", "Wacken"));
    }
}
