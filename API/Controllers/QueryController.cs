using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class QueryController(MangaContext context) : Controller
{
    /// <summary>
    /// Returns the <see cref="Author"/> with <paramref name="AuthorId"/>
    /// </summary>
    /// <param name="AuthorId"><see cref="Author"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Author"/> with <paramref name="AuthorId"/> not found</response>
    [HttpGet("Author/{AuthorId}")]
    [ProducesResponseType<Author>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetAuthor(string AuthorId)
    {
        if (context.Authors.Find(AuthorId) is not { } author)
            return NotFound();
        
        return Ok(author);
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> which where Authored by <see cref="Author"/> with <paramref name="AuthorId"/>
    /// </summary>
    /// <param name="AuthorId"><see cref="Author"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Author"/> with <paramref name="AuthorId"/></response>
    [HttpGet("Mangas/WithAuthorId/{AuthorId}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangaWithAuthorIds(string AuthorId)
    {
        if (context.Authors.Find(AuthorId) is not { } author)
            return NotFound();
        
        return Ok(context.Mangas.Where(m => m.Authors.Contains(author)));
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> with <see cref="Tag"/>
    /// </summary>
    /// <param name="Tag"><see cref="Tag"/>.Tag</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Tag"/> not found</response>
    [HttpGet("Mangas/WithTag/{Tag}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangasWithTag(string Tag)
    {
        if (context.Tags.Find(Tag) is not { } tag)
            return NotFound();
        
        return Ok(context.Mangas.Where(m => m.MangaTags.Contains(tag)));
    }
    
    /// <summary>
    /// Returns <see cref="Chapter"/> with <paramref name="ChapterId"/>
    /// </summary>
    /// <param name="ChapterId"><see cref="Chapter"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Chapter"/> with <paramref name="ChapterId"/> not found</response>
    [HttpGet("Chapter/{ChapterId}")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    public IActionResult GetChapter(string ChapterId)
    {
        if (context.Chapters.Find(ChapterId) is not { } chapter)
            return NotFound();
        
        return Ok(chapter);
    }
}