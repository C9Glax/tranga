using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class QueryController(PgsqlContext context) : Controller
{
    [HttpGet("Author/{AuthorId}")]
    [ProducesResponseType<Author>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetAuthor(string AuthorId)
    {
        Author? ret = context.Authors.Find(AuthorId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }
    
    [HttpGet("Mangas/WithAuthorId/{AuthorId}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangaWithAuthorIds(string AuthorId)
    {
        return Ok(context.Manga.Where(m => m.AuthorIds.Contains(AuthorId)));
    }
    
    [HttpGet("Link/{LinkId}")]
    [ProducesResponseType<Link>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetLink(string LinkId)
    {
        Link? ret = context.Link.Find(LinkId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }
    
    [HttpGet("Links/WithIds")]
    [ProducesResponseType<Link[]>(Status200OK, "application/json")]
    public IActionResult GetLink([FromBody]string[] LinkIds)
    {
        Link[] ret = context.Link.Where(l => LinkIds.Contains(l.LinkId)).ToArray();
        return Ok(ret);
    }
    
    [HttpGet("AltTitle/{AltTitleId}")]
    [ProducesResponseType<MangaAltTitle>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetAltTitle(string AltTitleId)
    {
        MangaAltTitle? ret = context.AltTitles.Find(AltTitleId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }
    
    [HttpGet("AltTitles/WithIds")]
    [ProducesResponseType<MangaAltTitle[]>(Status200OK, "application/json")]
    public IActionResult GetAltTitle([FromBody]string[] AltTitleIds)
    {
        MangaAltTitle[] ret = context.AltTitles.Where(a => AltTitleIds.Contains(a.AltTitleId)).ToArray();
        return Ok(ret);
    }
    
    [HttpGet("Mangas/WithTag/{Tag}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangasWithTag(string Tag)
    {
        return Ok(context.Manga.Where(m => m.Tags.Contains(Tag)));
    }
}