using System.Collections.Generic;

namespace Jellyfin.Plugin.Festivals.Model;

/// <summary>
/// A single recorded performance (maps to a Jellyfin Episode).
/// </summary>
public class Recording
{
    /// <summary>Gets or sets the file name (without extension) this recording matches.</summary>
    public string FileMatch { get; set; } = string.Empty;

    /// <summary>Gets or sets the performing artist (used as the episode title).</summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>Gets or sets the stage name.</summary>
    public string? Stage { get; set; }

    /// <summary>Gets or sets the performance date/time as free text.</summary>
    public string? Date { get; set; }

    /// <summary>Gets or sets the genres for this performance.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>Gets or sets the artist biography / description.</summary>
    public string? Bio { get; set; }

    /// <summary>Gets or sets the stored artist image file name (in the plugin image dir).</summary>
    public string? ArtistImage { get; set; }

    /// <summary>Gets or sets the setlist as free text.</summary>
    public string? Setlist { get; set; }
}

/// <summary>
/// One edition year of a festival (maps to a Jellyfin Season).
/// </summary>
public class FestivalYear
{
    /// <summary>Gets or sets the year (e.g. 2026); also the season index number.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets an optional description for this edition.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a stored poster image file name for this edition.</summary>
    public string? Poster { get; set; }

    /// <summary>Gets or sets the recordings of this edition.</summary>
    public List<Recording> Recordings { get; set; } = new();
}

/// <summary>
/// A festival (maps to a Jellyfin Series).
/// </summary>
public class Festival
{
    /// <summary>Gets or sets a stable identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the festival name (matched against the series folder name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the festival description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the festival genres.</summary>
    public List<string> Genres { get; set; } = new();

    /// <summary>Gets or sets the stored hero/backdrop image file name.</summary>
    public string? HeroImage { get; set; }

    /// <summary>Gets or sets the stored poster image file name.</summary>
    public string? Poster { get; set; }

    /// <summary>Gets or sets the festival editions.</summary>
    public List<FestivalYear> Years { get; set; } = new();
}

/// <summary>
/// Root database container persisted as JSON.
/// </summary>
public class FestivalDatabase
{
    /// <summary>Gets or sets all festivals.</summary>
    public List<Festival> Festivals { get; set; } = new();
}
