using System.Text;

namespace Jellyfin.Plugin.Festivals.Data;

/// <summary>
/// Helpers for normalising and comparing festival / artist names against folder and file names.
/// </summary>
public static class NameMatching
{
    /// <summary>
    /// Normalises a name: lower-cased, trimmed, punctuation removed, whitespace collapsed to single spaces.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The normalised value.</returns>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var lastSpace = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastSpace = false;
            }
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                if (!lastSpace && sb.Length > 0)
                {
                    sb.Append(' ');
                    lastSpace = true;
                }
            }

            // any other punctuation is dropped
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Returns true if two names match after normalisation.
    /// </summary>
    /// <param name="a">First value.</param>
    /// <param name="b">Second value.</param>
    /// <returns>True when equal after normalisation.</returns>
    public static bool Matches(string? a, string? b) => Normalize(a) == Normalize(b);
}
