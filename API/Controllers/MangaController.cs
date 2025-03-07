using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class MangaController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Returns all cached Manga
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<Manga[]>(Status200OK)]
    public IActionResult GetAllManga()
    {
        Manga[] ret = context.Manga.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns all cached Manga with IDs
    /// </summary>
    /// <param name="ids">Array of Manga-IDs</param>
    /// <response code="200"></response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Manga[]>(Status200OK)]
    public IActionResult GetManga([FromBody]string[] ids)
    {
        Manga[] ret = context.Manga.Where(m => ids.Contains(m.MangaId)).ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Return Manga with ID
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType<Manga>(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetManga(string id)
    {
        Manga? ret = context.Manga.Find(id);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }

    /// <summary>
    /// Delete Manga with ID
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult DeleteManga(string id)
    {
        try
        {
            Manga? ret = context.Manga.Find(id);
            if (ret is null)
                return NotFound();
            
            context.Remove(ret);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Returns Cover of Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <response code="200">JPEG Image</response>
    /// <response code="204">Cover not loaded</response>
    /// <response code="404">Manga with ID not found</response>
    [HttpGet("{id}/Cover")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetCover(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound();
        if (!System.IO.File.Exists(m.CoverFileNameInCache))
            return NoContent();

        Image image = Image.Load(m.CoverFileNameInCache);
        using MemoryStream ms = new();
        image.Save(ms, new JpegEncoder(){Quality = 100});
        return File(ms, "image/jpeg");
    }

    /// <summary>
    /// Returns all Chapters of Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found</response>
    [HttpGet("{id}/Chapters")]
    [ProducesResponseType<Chapter[]>(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetChapters(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound();
        
        Chapter[] ret = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns the latest Chapter of requested Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404">Manga with ID not found.</response>
    /// <response code="500">Could not retrieve the maximum chapter-number</response>
    [HttpGet("{id}/Chapter/Latest")]
    [ProducesResponseType<Chapter>(Status200OK)]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetLatestChapter(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound();
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(500, "Max chapter could not be found");
        
        return Ok(max);
    }
    
    /// <summary>
    /// Configure the cut-off for Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found.</response>
    [HttpPatch("{id}/IgnoreChaptersBefore")]
    [ProducesResponseType<float>(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult IgnoreChaptersBefore(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound();
        return Ok(m.IgnoreChapterBefore);
    }
    
    /// <summary>
    /// Move the Directory the .cbz-files are located in
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <param name="folder">New Directory-Path</param>
    /// <remarks>NOT IMPLEMENTED</remarks>
    [HttpPost("{id}/MoveFolder")]
    public IActionResult MoveFolder(string id, [FromBody]string folder)
    {
        throw new NotImplementedException();
    }
}