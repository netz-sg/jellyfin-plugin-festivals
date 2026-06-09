using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.Festivals.Data;

/// <summary>
/// Resolves the festival edition year from a season folder name, because Jellyfin
/// parses a folder like "2026" into a season <em>number</em> (e.g. 20), not the year.
/// </summary>
public static class YearResolver
{
    private static readonly Regex _yearRegex = new(@"(19|20)\d{2}", RegexOptions.Compiled);

    /// <summary>
    /// Resolves the year from a folder name, falling back to a season index number.
    /// </summary>
    /// <param name="folderName">The season folder name (e.g. "2026", "Season 2026").</param>
    /// <param name="fallback">The season index number to use when no year is found.</param>
    /// <returns>The resolved year (or 0 when nothing matches).</returns>
    public static int FromFolderName(string? folderName, int? fallback)
    {
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            var match = _yearRegex.Match(folderName);
            if (match.Success && int.TryParse(match.Value, out var year))
            {
                return year;
            }

            if (int.TryParse(folderName.Trim(), out var plain))
            {
                return plain;
            }
        }

        return fallback ?? 0;
    }
}
