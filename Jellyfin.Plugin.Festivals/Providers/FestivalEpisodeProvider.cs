using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Festivals.Data;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Festivals.Providers;

/// <summary>
/// Enriches an Episode (a single recording) with artist information from the festival store.
/// </summary>
public class FestivalEpisodeProvider : ICustomMetadataProvider<Episode>
{
    private readonly FestivalStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalEpisodeProvider"/> class.
    /// </summary>
    /// <param name="store">The festival store.</param>
    public FestivalEpisodeProvider(FestivalStore store) => _store = store;

    /// <inheritdoc />
    public string Name => "Festivals";

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(item.Path))
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        var seasonFolder = Path.GetFileName(Path.GetDirectoryName(item.Path));
        var year = YearResolver.FromFolderName(seasonFolder, item.ParentIndexNumber);
        var fileName = Path.GetFileNameWithoutExtension(item.Path);
        var recording = _store.FindRecording(item.SeriesName, year, fileName);
        if (recording is null)
        {
            return Task.FromResult(ItemUpdateType.None);
        }

        item.Name = recording.Artist;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(recording.Bio))
        {
            parts.Add(recording.Bio!);
        }

        var meta = new List<string>();
        if (!string.IsNullOrWhiteSpace(recording.Stage))
        {
            meta.Add($"Bühne: {recording.Stage}");
        }

        if (!string.IsNullOrWhiteSpace(recording.Date))
        {
            meta.Add($"Datum: {recording.Date}");
        }

        if (meta.Count > 0)
        {
            parts.Add(string.Join(" · ", meta));
        }

        if (!string.IsNullOrWhiteSpace(recording.Setlist))
        {
            parts.Add($"Setlist:\n{recording.Setlist}");
        }

        item.Overview = string.Join("\n\n", parts);

        if (recording.Genres.Count > 0)
        {
            item.Genres = recording.Genres.ToArray();
        }

        return Task.FromResult(ItemUpdateType.MetadataEdit);
    }
}
