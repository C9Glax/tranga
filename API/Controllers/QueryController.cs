using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soenneker.Utils.String.NeedlemanWunsch;
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
    public async Task<IActionResult> GetAuthor (string AuthorId)
    {
        if (await context.Authors.FirstOrDefaultAsync(a => a.Key == AuthorId, HttpContext.RequestAborted) is not { } author)
            return NotFound(nameof(AuthorId));
        
        return Ok(author);
    }
    
    /// <summary>
    /// Returns <see cref="Chapter"/> with <paramref name="ChapterId"/>
    /// </summary>
    /// <param name="ChapterId"><see cref="Chapter"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Chapter"/> with <paramref name="ChapterId"/> not found</response>
    [HttpGet("Chapter/{ChapterId}")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetChapter (string ChapterId)
    {
        if (await context.Chapters.FirstOrDefaultAsync(c => c.Key == ChapterId, HttpContext.RequestAborted) is not { } chapter)
            return NotFound(nameof(ChapterId));
        
        return Ok(chapter);
    }

    /// <summary>
    /// Returns the <see cref="MangaConnectorId{Manga}"/> with <see cref="MangaConnectorId{Manga}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="MangaConnectorId{Manga}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnectorId{Manga}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpGet("Manga/MangaConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType<MangaConnectorId<Manga>>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetMangaMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToManga.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, HttpContext.RequestAborted) is not { } mcIdManga)
            return NotFound(nameof(MangaConnectorIdId));
        
        return Ok(mcIdManga);
    }

    /// <summary>
    /// Returns <see cref="Manga"/> with names similar to <see cref="Manga"/> (identified by <paramref name="MangaId"/>)
    /// </summary>
    /// <param name="MangaId">Key of <see cref="Manga"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("Manga/{MangaId}/SimilarName")]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetSimilarManga (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        string name = manga.Name;
        
        if(await context.Mangas.Where(m => m.Key != MangaId).ToDictionaryAsync(m => m.Key, m => m.Name, HttpContext.RequestAborted) is not { } mangaNames)
            return StatusCode(Status500InternalServerError);
        
        string[] similarIds = mangaNames
            .Where(kv => NeedlemanWunschStringUtil.CalculateSimilarityPercentage(name, kv.Value) > 0.8)
            .Select(kv => kv.Key).ToArray();
        
        return Ok(similarIds);
    }

    /// <summary>
    /// Returns the <see cref="MangaConnectorId{Chapter}"/> with <see cref="MangaConnectorId{Chapter}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="MangaConnectorId{Manga}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnectorId{Chapter}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpGet("Chapter/MangaConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType<MangaConnectorId<Chapter>>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetChapterMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToManga.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, HttpContext.RequestAborted) is not { } mcIdChapter)
            return NotFound(nameof(MangaConnectorIdId));
        
        return Ok(mcIdChapter);
    }
}