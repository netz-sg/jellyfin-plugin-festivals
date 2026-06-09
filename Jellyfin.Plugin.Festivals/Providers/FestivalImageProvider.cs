using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Festivals.Data;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace Jellyfin.Plugin.Festivals.Providers;

/// <summary>
/// Supplies hero/poster/artist images for festivals, editions and recordings from the plugin image store.
/// </summary>
public class FestivalImageProvider : IDynamicImageProvider
{
    private readonly FestivalStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalImageProvider"/> class.
    /// </summary>
    /// <param name="store">The festival store.</param>
    public FestivalImageProvider(FestivalStore store) => _store = store;

    /// <inheritdoc />
    public string Name => "Festivals";

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Series or Season or Episode;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => item switch
    {
        Series => new[] { ImageType.Primary, ImageType.Backdrop },
        Season => new[] { ImageType.Primary },
        Episode => new[] { ImageType.Primary },
        _ => Array.Empty<ImageType>()
    };

    /// <inheritdoc />
    public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
    {
        var fileName = ResolveFileName(item, type);
        if (fileName is null)
        {
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        var fullPath = Path.Combine(_store.ImageDir, fileName);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(new DynamicImageResponse { HasImage = false });
        }

        return Task.FromResult(new DynamicImageResponse
        {
            Path = fullPath,
            Protocol = MediaProtocol.File,
            HasImage = true,
            Format = FormatFromExtension(fullPath)
        });
    }

    private string? ResolveFileName(BaseItem item, ImageType type)
    {
        switch (item)
        {
            case Series series:
                var festival = _store.FindFestival(series.Name);
                if (festival is null)
                {
                    return null;
                }

                return type == ImageType.Backdrop ? festival.HeroImage : festival.Poster;

            case Season season when !string.IsNullOrEmpty(season.Path):
                var seasonYear = YearResolver.FromFolderName(Path.GetFileName(season.Path), season.IndexNumber);
                return _store.FindYear(season.SeriesName, seasonYear)?.Poster;

            case Episode episode when !string.IsNullOrEmpty(episode.Path):
                var episodeYear = YearResolver.FromFolderName(Path.GetFileName(Path.GetDirectoryName(episode.Path)), episode.ParentIndexNumber);
                var file = Path.GetFileNameWithoutExtension(episode.Path);
                return _store.FindRecording(episode.SeriesName, episodeYear, file)?.ArtistImage;

            default:
                return null;
        }
    }

    private static ImageFormat FormatFromExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => ImageFormat.Png,
            ".webp" => ImageFormat.Webp,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            _ => ImageFormat.Jpg
        };
    }
}
