using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Plugin.Festivals.Data;
using Jellyfin.Plugin.Festivals.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Festivals.Api;

/// <summary>
/// REST API backing the Festivals configuration page.
/// </summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("Festivals")]
[Produces("application/json")]
public class FestivalApiController : ControllerBase
{
    private readonly FestivalStore _store;
    private readonly FestivalDiscovery _discovery;

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalApiController"/> class.
    /// </summary>
    /// <param name="store">The festival store.</param>
    /// <param name="discovery">The festival discovery service.</param>
    public FestivalApiController(FestivalStore store, FestivalDiscovery discovery)
    {
        _store = store;
        _discovery = discovery;
    }

    /// <summary>
    /// Gets the raw saved festival database (enrichment only).
    /// </summary>
    /// <returns>The database.</returns>
    [HttpGet]
    public ActionResult<FestivalDatabase> GetDatabase() => _store.GetAll();

    /// <summary>
    /// Gets the available library locations to choose from in the setup step.
    /// </summary>
    /// <returns>The library locations.</returns>
    [HttpGet("Libraries")]
    public ActionResult<IReadOnlyList<LibraryInfo>> GetLibraries() => Ok(_discovery.GetLibraries());

    /// <summary>
    /// Gets the plugin settings (selected festivals folder).
    /// </summary>
    /// <returns>The settings.</returns>
    [HttpGet("Config")]
    public ActionResult<FestivalSettings> GetConfig() => _store.GetSettings();

    /// <summary>
    /// Saves the plugin settings (selected festivals folder).
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <returns>No content.</returns>
    [HttpPost("Config")]
    public ActionResult SaveConfig([FromBody] FestivalSettings settings)
    {
        _store.SaveSettings(settings);
        return NoContent();
    }

    /// <summary>
    /// Gets the festival tree discovered from the configured folder, overlaid with saved enrichment.
    /// This is what the configuration page renders, so nothing has to be typed by hand.
    /// </summary>
    /// <returns>The merged tree.</returns>
    [HttpGet("Tree")]
    public ActionResult<FestivalDatabase> GetTree()
    {
        var rootPath = _store.GetSettings().LibraryPath;
        var discovered = _discovery.Discover(rootPath);
        Merge(discovered, _store.GetAll());
        return discovered;
    }

    private static void Merge(FestivalDatabase discovered, FestivalDatabase saved)
    {
        foreach (var festival in discovered.Festivals)
        {
            var savedFestival = saved.Festivals.FirstOrDefault(x => NameMatching.Matches(x.Name, festival.Name));
            if (savedFestival is null)
            {
                continue;
            }

            festival.Description = savedFestival.Description;
            festival.Location = savedFestival.Location;
            festival.Genres = savedFestival.Genres;
            festival.HeroImage = savedFestival.HeroImage;
            festival.Poster = savedFestival.Poster;

            foreach (var edition in festival.Years)
            {
                var savedYear = savedFestival.Years.FirstOrDefault(x => x.Year == edition.Year);
                if (savedYear is null)
                {
                    continue;
                }

                edition.Description = savedYear.Description;
                edition.Poster = savedYear.Poster;

                foreach (var recording in edition.Recordings)
                {
                    var savedRecording = savedYear.Recordings
                        .FirstOrDefault(x => NameMatching.Matches(x.FileMatch, recording.FileMatch));
                    if (savedRecording is null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(savedRecording.Artist))
                    {
                        recording.Artist = savedRecording.Artist;
                    }

                    recording.Stage = savedRecording.Stage;
                    recording.Date = savedRecording.Date;
                    recording.Genres = savedRecording.Genres;
                    recording.Bio = savedRecording.Bio;
                    recording.Setlist = savedRecording.Setlist;
                    recording.ArtistImage = savedRecording.ArtistImage;
                }
            }
        }
    }

    /// <summary>
    /// Replaces the full festival database.
    /// </summary>
    /// <param name="database">The new database.</param>
    /// <returns>No content.</returns>
    [HttpPut]
    public ActionResult UpdateDatabase([FromBody] FestivalDatabase database)
    {
        _store.Replace(database);
        return NoContent();
    }

    /// <summary>
    /// Uploads an image and returns the stored file name.
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    /// <returns>The stored file name.</returns>
    [HttpPost("Images")]
    public async Task<ActionResult> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var ext = Path.GetExtension(file.FileName);
        var fileName = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(_store.ImageDir, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream).ConfigureAwait(false);
        }

        return Ok(new { fileName });
    }

    /// <summary>
    /// Serves a previously uploaded image.
    /// </summary>
    /// <param name="name">The stored file name.</param>
    /// <returns>The image file.</returns>
    [HttpGet("Images/{name}")]
    [AllowAnonymous]
    public ActionResult GetImage([FromRoute] string name)
    {
        // Guard against path traversal.
        var safeName = Path.GetFileName(name);
        var fullPath = Path.Combine(_store.ImageDir, safeName);
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        return PhysicalFile(fullPath, ContentTypeFor(fullPath));
    }

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        _ => "image/jpeg"
    };
}
