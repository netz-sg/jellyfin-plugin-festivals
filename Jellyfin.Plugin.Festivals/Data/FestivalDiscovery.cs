using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Festivals.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Festivals.Data;

/// <summary>
/// Discovers the festival hierarchy directly from the scanned Jellyfin library,
/// so the configuration page never requires manually typing folder/file names.
/// </summary>
public class FestivalDiscovery
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalDiscovery"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    public FestivalDiscovery(ILibraryManager libraryManager) => _libraryManager = libraryManager;

    /// <summary>
    /// Lists the available library locations the user can choose as their festivals folder.
    /// </summary>
    /// <returns>The library locations.</returns>
    public IReadOnlyList<LibraryInfo> GetLibraries()
    {
        return _libraryManager.GetVirtualFolders()
            .SelectMany(v => (v.Locations ?? Array.Empty<string>())
                .Select(loc => new LibraryInfo { Name = v.Name, Path = loc }))
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Builds the festival tree from the library contents under the given root folder.
    /// </summary>
    /// <param name="rootPath">The configured festivals root folder.</param>
    /// <returns>The discovered database (structure only, no enrichment).</returns>
    public FestivalDatabase Discover(string? rootPath)
    {
        var result = new FestivalDatabase();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return result;
        }

        var scopeLocations = new[] { rootPath };

        var seriesList = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series }
        });

        foreach (var item in seriesList)
        {
            if (item is not Series series)
            {
                continue;
            }

            if (!IsUnder(series.Path, scopeLocations))
            {
                continue;
            }

            var festival = new Festival
            {
                Id = series.Id.ToString("N"),
                Name = series.Name,
                Years = new List<FestivalYear>()
            };

            foreach (var child in series.GetRecursiveChildren())
            {
                if (child is not Episode episode)
                {
                    continue;
                }

                var year = episode.ParentIndexNumber ?? 0;
                var edition = festival.Years.FirstOrDefault(y => y.Year == year);
                if (edition is null)
                {
                    edition = new FestivalYear { Year = year, Recordings = new List<Recording>() };
                    festival.Years.Add(edition);
                }

                var file = string.IsNullOrEmpty(episode.Path)
                    ? episode.Name ?? string.Empty
                    : Path.GetFileNameWithoutExtension(episode.Path);

                edition.Recordings.Add(new Recording
                {
                    FileMatch = file,
                    Artist = episode.Name ?? file,
                    Genres = new List<string>()
                });
            }

            festival.Years = festival.Years
                .OrderByDescending(y => y.Year)
                .ToList();

            foreach (var edition in festival.Years)
            {
                edition.Recordings = edition.Recordings
                    .OrderBy(r => r.Artist, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            result.Festivals.Add(festival);
        }

        result.Festivals = result.Festivals
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return result;
    }

    private static bool IsUnder(string? path, string[] locations)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var normalized = Path.GetFullPath(path);
        return locations.Any(loc =>
            normalized.StartsWith(Path.GetFullPath(loc), StringComparison.OrdinalIgnoreCase));
    }
}
