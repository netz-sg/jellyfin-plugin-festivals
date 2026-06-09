using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Festivals.Data;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Festivals.Providers;

/// <summary>
/// Enriches a Season (festival edition year) from the festival store.
/// </summary>
public class FestivalSeasonProvider : ICustomMetadataProvider<Season>
{
    private readonly FestivalStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalSeasonProvider"/> class.
    /// </summary>
    /// <param name="store">The festival store.</param>
    public FestivalSeasonProvider(FestivalStore store) => _store = store;

    /// <inheritdoc />
    public string Name => "Festivals";

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Season item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        var folder = string.IsNullOrEmpty(item.Path) ? null : Path.GetFileName(item.Path);
        var year = YearResolver.FromFolderName(folder, item.IndexNumber);
        if (year <= 0)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        // Always show the real year as the season title, even when Jellyfin parsed
        // the folder "2026" into a season number like 20.
        item.Name = year.ToString(CultureInfo.InvariantCulture);

        var edition = _store.FindYear(item.SeriesName, year);
        if (edition is not null && !string.IsNullOrWhiteSpace(edition.Description))
        {
            item.Overview = edition.Description;
        }

        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}
