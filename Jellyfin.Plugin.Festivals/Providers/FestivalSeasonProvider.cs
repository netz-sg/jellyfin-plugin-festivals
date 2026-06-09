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
        var year = item.IndexNumber;
        if (year is null)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        var edition = _store.FindYear(item.SeriesName, year.Value);
        if (edition is null)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        item.Name = year.Value.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(edition.Description))
        {
            item.Overview = edition.Description;
        }

        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}
