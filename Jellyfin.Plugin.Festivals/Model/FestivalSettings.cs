using System.Collections.Generic;

namespace Jellyfin.Plugin.Festivals.Model;

/// <summary>
/// Plugin runtime settings (separate from the enrichment data).
/// </summary>
public class FestivalSettings
{
    /// <summary>
    /// Gets or sets the id of the library chosen as the festivals source.
    /// Preferred over <see cref="LibraryPath"/> because it uses Jellyfin's own hierarchy.
    /// </summary>
    public string? LibraryId { get; set; }

    /// <summary>
    /// Gets or sets the root folder that contains the festival recordings.
    /// Used as a fallback when no <see cref="LibraryId"/> is set.
    /// </summary>
    public string? LibraryPath { get; set; }
}

/// <summary>
/// A selectable library location shown in the setup step.
/// </summary>
public class LibraryInfo
{
    /// <summary>Gets or sets the library item id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the library display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the filesystem path of this library location.</summary>
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Diagnostic information to explain why discovery did or did not find festivals.
/// </summary>
public class DiagnosticsResult
{
    /// <summary>Gets or sets the configured library id.</summary>
    public string? SelectedLibraryId { get; set; }

    /// <summary>Gets or sets the configured path.</summary>
    public string? SelectedPath { get; set; }

    /// <summary>Gets or sets the total number of series across all libraries.</summary>
    public int TotalSeries { get; set; }

    /// <summary>Gets or sets the number of series matched by the current selection.</summary>
    public int MatchedSeries { get; set; }

    /// <summary>Gets or sets a few example series paths to help diagnose path mismatches.</summary>
    public List<string> SampleSeriesPaths { get; set; } = new();

    /// <summary>Gets or sets the available libraries.</summary>
    public List<LibraryInfo> Libraries { get; set; } = new();
}
