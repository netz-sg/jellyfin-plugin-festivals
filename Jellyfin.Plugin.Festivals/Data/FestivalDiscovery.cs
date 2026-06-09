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
            .Select(v => new LibraryInfo
            {
                Id = v.ItemId ?? string.Empty,
                Name = v.Name,
                Path = (v.Locations ?? Array.Empty<string>()).FirstOrDefault() ?? string.Empty
            })
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Builds the festival tree from the library selected in the settings.
    /// </summary>
    /// <param name="settings">The plugin settings (library id / path).</param>
    /// <returns>The discovered database (structure only, no enrichment).</returns>
    public FestivalDatabase Discover(FestivalSettings settings)
    {
        var result = new FestivalDatabase();

        foreach (var series in ResolveSeries(settings))
        {
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

                var seasonFolder = string.IsNullOrEmpty(episode.Path)
                    ? null
                    : Path.GetFileName(Path.GetDirectoryName(episode.Path));
                var year = YearResolver.FromFolderName(seasonFolder, episode.ParentIndexNumber);
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

            festival.Years = festival.Years.OrderByDescending(y => y.Year).ToList();
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

    /// <summary>
    /// Produces diagnostic counts to explain discovery results.
    /// </summary>
    /// <param name="settings">The plugin settings.</param>
    /// <returns>The diagnostics.</returns>
    public DiagnosticsResult Diagnose(FestivalSettings settings)
    {
        var allSeries = AllSeries();
        var matched = ResolveSeries(settings);
        return new DiagnosticsResult
        {
            SelectedLibraryId = settings.LibraryId,
            SelectedPath = settings.LibraryPath,
            TotalSeries = allSeries.Count,
            MatchedSeries = matched.Count,
            SampleSeriesPaths = allSeries.Take(8)
                .Select(s => s.Path ?? "(no path)")
                .ToList(),
            Libraries = GetLibraries().ToList()
        };
    }

    /// <summary>
    /// Returns the ids of all series, seasons and episodes under the configured library,
    /// so a metadata refresh can be queued for them after saving.
    /// </summary>
    /// <param name="settings">The plugin settings.</param>
    /// <returns>The item ids.</returns>
    public IReadOnlyList<Guid> ResolveItemIds(FestivalSettings settings)
    {
        var ids = new List<Guid>();
        foreach (var series in ResolveSeries(settings))
        {
            ids.Add(series.Id);
            foreach (var child in series.GetRecursiveChildren())
            {
                if (child is Season || child is Episode)
                {
                    ids.Add(child.Id);
                }
            }
        }

        return ids;
    }

    private List<Series> ResolveSeries(FestivalSettings settings)
    {
        // Preferred: use the chosen library's own item hierarchy.
        if (!string.IsNullOrWhiteSpace(settings.LibraryId)
            && Guid.TryParse(settings.LibraryId, out var libId)
            && _libraryManager.GetItemById(libId) is Folder root)
        {
            return root.GetRecursiveChildren()
                .OfType<Series>()
                .ToList();
        }

        // Fallback: filter all series by path.
        if (!string.IsNullOrWhiteSpace(settings.LibraryPath))
        {
            return AllSeries()
                .Where(s => IsUnder(s.Path, settings.LibraryPath!))
                .ToList();
        }

        return new List<Series>();
    }

    private List<Series> AllSeries()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series }
        }).OfType<Series>().ToList();
    }

    private static bool IsUnder(string? path, string root)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var p = Normalize(path);
        var r = Normalize(root);
        return p == r || p.StartsWith(r + "/", StringComparison.Ordinal);
    }

    private static string Normalize(string p)
        => p.Replace('\\', '/').TrimEnd('/').ToLowerInvariant();
}
