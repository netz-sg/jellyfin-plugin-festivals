using Jellyfin.Plugin.Festivals.Data;
using Xunit;

namespace Jellyfin.Plugin.Festivals.Tests;

public class YearResolverTests
{
    [Theory]
    [InlineData("2026", null, 2026)]
    [InlineData("Season 2026", null, 2026)]
    [InlineData("2026 (Live)", null, 2026)]
    [InlineData("1999", null, 1999)]
    public void FromFolderName_extracts_year(string folder, int? fallback, int expected)
    {
        Assert.Equal(expected, YearResolver.FromFolderName(folder, fallback));
    }

    [Fact]
    public void FromFolderName_uses_fallback_when_no_year()
    {
        Assert.Equal(2, YearResolver.FromFolderName("Season 2", 2));
        Assert.Equal(5, YearResolver.FromFolderName("Specials", 5));
        Assert.Equal(0, YearResolver.FromFolderName(null, null));
    }
}
