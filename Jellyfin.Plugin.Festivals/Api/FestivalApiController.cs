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

    /// <summary>
    /// Initializes a new instance of the <see cref="FestivalApiController"/> class.
    /// </summary>
    /// <param name="store">The festival store.</param>
    public FestivalApiController(FestivalStore store) => _store = store;

    /// <summary>
    /// Gets the full festival database.
    /// </summary>
    /// <returns>The database.</returns>
    [HttpGet]
    public ActionResult<FestivalDatabase> GetDatabase() => _store.GetAll();

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
