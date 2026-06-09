namespace Jellyfin.Plugin.Festivals.Model;

/// <summary>
/// Plugin runtime settings (separate from the enrichment data).
/// </summary>
public class FestivalSettings
{
    /// <summary>
    /// Gets or sets the root folder that contains the festival recordings.
    /// Discovery is scoped to series whose path lives under this folder.
    /// </summary>
    public string? LibraryPath { get; set; }
}

/// <summary>
/// A selectable library location shown in the setup step.
/// </summary>
public class LibraryInfo
{
    /// <summary>Gets or sets the library display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the filesystem path of this library location.</summary>
    public string Path { get; set; } = string.Empty;
}
