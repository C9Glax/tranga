using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class MangaController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Returns all cached Manga with IDs
    /// </summary>
    /// <param name="ids">Array of Manga-IDs</param>
    /// <returns>Array of Manga</returns>
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
    /// <returns>Manga</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<Manga>(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetManga(string id)
    {
        Manga? ret = context.Manga.Find(id);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }

    /// <summary>
    /// Delete Manga with ID
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <returns>Nothing</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult DeleteManga(string id)
    {
        try
        {
            Manga? ret = context.Manga.Find(id);
            switch (ret is not null)
            {
                case true:
                    context.Remove(ret);
                    context.SaveChanges();
                    return Ok();
                case false: return NotFound();
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Create new Manga
    /// </summary>
    /// <param name="manga">Manga</param>
    /// <returns>Nothing</returns>
    [HttpPut]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult CreateManga([FromBody] Manga manga)
    {
        try
        {
            context.Manga.Add(manga);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Update Manga MetaData
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <param name="manga">New Manga-Info</param>
    /// <returns>Nothing</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult UpdateMangaMetadata(string id, [FromBody]Manga manga)
    {
        try
        {
            Manga? ret = context.Manga.Find(id);
            switch (ret is not null)
            {
                case true:
                    ret.UpdateWithInfo(manga);
                    context.Update(ret);
                    context.SaveChanges();
                    return Ok();
                case false: return NotFound();
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Returns URL of Cover of Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <returns>URL of Cover</returns>
    [HttpGet("{id}/Cover")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetCover(string id)
    {
        return StatusCode(500, "Not implemented"); //TODO
    }

    /// <summary>
    /// Returns all Chapters of Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <returns>Array of Chapters</returns>
    [HttpGet("{id}/Chapters")]
    [ProducesResponseType<Chapter[]>(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound)]
    public IActionResult GetChapters(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound("Manga could not be found");
        Chapter[] ret = context.Chapters.Where(c => c.ParentManga.MangaId == m.MangaId).ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Adds/Creates new Chapter for Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <param name="chapters">Array of Chapters</param>
    /// <remarks>Manga-ID and all Chapters have to be the same</remarks>
    /// <returns>Nothing</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult CreateChapters(string id, [FromBody]Chapter[] chapters)
    {
        try
        {
            Manga? ret = context.Manga.Find(id);
            if(ret is null)
                return NotFound("Manga could not be found");
            if(chapters.All(c => c.ParentManga.MangaId == ret.MangaId))
                return BadRequest("Chapters belong to different Manga.");
            
            context.Chapters.AddRange(chapters);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    /// <summary>
    /// Returns the latest Chapter of requested Manga
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <returns>Latest Chapter</returns>
    [HttpGet("{id}/Chapter/Latest")]
    [ProducesResponseType<Chapter>(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound)]
    public IActionResult GetLatestChapter(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound("Manga could not be found");
        if (m.LatestChapterAvailable is null)
            return NotFound("Chapter could not be found");
        return Ok(m.LatestChapterAvailable);
    }
    
    /// <summary>
    /// Configure the cut-off for Manga
    /// </summary>
    /// <remarks>This is important for the DownloadNewChapters-Job</remarks>
    /// <param name="id">Manga-ID</param>
    /// <returns>Nothing</returns>
    [HttpPatch("{id}/IgnoreChaptersBefore")]
    [ProducesResponseType<float>(Status200OK)]
    public IActionResult IgnoreChaptersBefore(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound("Manga could not be found");
        return Ok(m.IgnoreChapterBefore);
    }
    
    /// <summary>
    /// Move the Directory the .cbz-files are located in
    /// </summary>
    /// <param name="id">Manga-ID</param>
    /// <param name="folder">New Directory-Path</param>
    /// <returns>Nothing</returns>
    [HttpPost("{id}/MoveFolder")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult MoveFolder(string id, [FromBody]string folder)
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
}