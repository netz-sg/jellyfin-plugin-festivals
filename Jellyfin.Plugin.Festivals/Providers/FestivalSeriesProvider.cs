using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Festivals.Data;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Festivals.Providers;

/// <summary>
/// Enriches a Series (festival) from the festival store.
/// </summary>
public class FestivalSeriesProvider : ICustomMetadataProvider<Series>
{
    private readonly FestivalStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalSeriesProvider"/> class.
    /// </summary>
    /// <param name="store">The festival store.</param>
    public FestivalSeriesProvider(FestivalStore store) => _store = store;

    /// <inheritdoc />
    public string Name => "Festivals";

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        var festival = _store.FindFestival(item.Name);
        if (festival is null)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(festival.Description))
        {
            parts.Add(festival.Description!);
        }

        if (!string.IsNullOrWhiteSpace(festival.Location))
        {
            parts.Add($"📍 {festival.Location}");
        }

        item.Overview = string.Join("\n\n", parts);

        if (festival.Genres.Count > 0)
        {
            item.Genres = festival.Genres.ToArray();
        }

        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}
