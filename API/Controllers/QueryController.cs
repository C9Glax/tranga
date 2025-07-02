using API.Schema.MangaContext;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class QueryController(MangaContext context, ILog Log) : Controller
{
    /// <summary>
    /// Returns the Author-Information for Author-ID
    /// </summary>
    /// <param name="AuthorId">Author-Id</param>
    /// <response code="200"></response>
    /// <response code="404">Author with ID not found</response>
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
    
    /// <summary>
    /// Returns all Mangas which where Authored by Author with AuthorId
    /// </summary>
    /// <param name="AuthorId">Author-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Author not found</response>
    [HttpGet("Mangas/WithAuthorId/{AuthorId}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangaWithAuthorIds(string AuthorId)
    {
        if(context.Authors.Find(AuthorId) is not { } a)
            return NotFound();
        return Ok(context.Mangas.Where(m => m.Authors.Contains(a)));
    }
    /*
    /// <summary>
    /// Returns Link-Information for Link-Id
    /// </summary>
    /// <param name="LinkId"></param>
    /// <response code="200"></response>
    /// <response code="404">Link with ID not found</response>
    [HttpGet("Link/{LinkId}")]
    [ProducesResponseType<Link>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetLink(string LinkId)
    {
        Link? ret = context.Links.Find(LinkId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns AltTitle-Information for AltTitle-Id
    /// </summary>
    /// <param name="AltTitleId"></param>
    /// <response code="200"></response>
    /// <response code="404">AltTitle with ID not found</response>
    [HttpGet("AltTitle/{AltTitleId}")]
    [ProducesResponseType<AltTitle>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetAltTitle(string AltTitleId)
    {
        AltTitle? ret = context.AltTitles.Find(AltTitleId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }*/
    
    /// <summary>
    /// Returns all Obj with Tag
    /// </summary>
    /// <param name="Tag"></param>
    /// <response code="200"></response>
    /// <response code="404">Tag not found</response>
    [HttpGet("Mangas/WithTag/{Tag}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangasWithTag(string Tag)
    {
        if(context.Tags.Find(Tag) is not { } t)
            return NotFound();
        return Ok(context.Mangas.Where(m => m.MangaTags.Contains(t)));
    }
    
    /// <summary>
    /// Returns Chapter-Information for Chapter-Id
    /// </summary>
    /// <param name="ChapterId"></param>
    /// <response code="200"></response>
    /// <response code="404">Chapter with ID not found</response>
    [HttpGet("Chapter/{ChapterId}")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    public IActionResult GetChapter(string ChapterId)
    {
        Chapter? ret = context.Chapters.Find(ChapterId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }
}