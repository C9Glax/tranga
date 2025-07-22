using API.MangaConnectors;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult GetAuthor(string AuthorId)
    {
        if (context.Authors.Find(AuthorId) is not { } author)
            return NotFound();
        
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
    public IActionResult GetChapter(string ChapterId)
    {
        if (context.Chapters.Find(ChapterId) is not { } chapter)
            return NotFound();
        
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
    public IActionResult GetMangaMangaConnectorId(string MangaConnectorIdId)
    {
        if(context.MangaConnectorToManga.Find(MangaConnectorIdId) is not { } mcIdManga)
            return NotFound();
        
        return Ok(mcIdManga);
    }

    /// <summary>
    /// Returns all <see cref="Manga"/> that are being downloaded from at least one <see cref="MangaConnector"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Manga/Downloading")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetMangaDownloading()
    {
        Manga[] ret = context.MangaIncludeAll()
            .Where(m => m.MangaConnectorIds.Any(id => id.UseForDownload))
            .ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Returns <see cref="Manga"/> with names similar to <see cref="Manga"/> (identified by <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId">Key of <see cref="Manga"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    [HttpGet("Manga/{MangaId}/SimilarName")]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetSimilarManga(string MangaId)
    {
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        string name = manga.Name;
        Dictionary<string, string> mangaNames = context.Mangas.Where(m => m.Key != MangaId).ToDictionary(m => m.Key, m => m.Name);
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
    public IActionResult GetChapterMangaConnectorId(string MangaConnectorIdId)
    {
        if(context.MangaConnectorToChapter.Find(MangaConnectorIdId) is not { } mcIdChapter)
            return NotFound();
        
        return Ok(mcIdChapter);
    }
}