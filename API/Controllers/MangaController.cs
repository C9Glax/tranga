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
    [HttpPost]
    [ProducesResponseType<Manga[]>(Status200OK)]
    public IActionResult GetManga([FromBody]string[] ids)
    {
        Manga[] ret = context.Manga.Where(m => ids.Contains(m.MangaId)).ToArray();
        return Ok(ret);
    }

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

    [HttpGet("{id}/Cover")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetCover(string id)
    {
        return StatusCode(500, "Not implemented");
    }

    [HttpGet("{id}/Chapters")]
    [ProducesResponseType<Chapter[]>(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound)]
    public IActionResult GetChapters(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound("Manga could not be found");
        Chapter[] ret = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToArray();
        return Ok(ret);
    }
    
    [HttpGet("{id}/Chapter/Latest")]
    [ProducesResponseType<Chapter>(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound)]
    public IActionResult GetLatestChapter(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound("Manga could not be found");
        Chapter? c = context.Chapters.Find(m.LatestChapterAvailableId);
        if (c is null)
            return NotFound("Chapter could not be found");
        return Ok(c);
    }
    
    [HttpPatch("{id}/IgnoreChaptersBefore")]
    [ProducesResponseType<float>(Status200OK)]
    public IActionResult IgnoreChaptersBefore(string id)
    {
        Manga? m = context.Manga.Find(id);
        if (m is null)
            return NotFound("Manga could not be found");
        return Ok(m.IgnoreChapterBefore);
    }
    
    [HttpPost("{id}/MoveFolder")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult MoveFolder(string id, [FromBody]string folder)
    {
        return StatusCode(500, "Not implemented");
    }
}