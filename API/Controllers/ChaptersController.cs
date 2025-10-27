using API.Controllers.DTOs;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Chapter = API.Controllers.DTOs.Chapter;
using Manga = JikanDotNet.Manga;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class ChaptersController(MangaContext context) : Controller
{
    /// <summary>
    /// Returns all <see cref="Schema.MangaContext.Chapter"/> of <see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <param name="page">Page to request (default 1)</param>
    /// <param name="pageSize">Size of Page (default 10)</param>
    /// <response code="200"></response>
    /// <response code="400">Page data wrong</response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found</response>
    [HttpGet("Manga/{MangaId}")]
    [ProducesResponseType<List<Chapter>>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<Results<Ok<PagedResponse<Chapter>>, BadRequest, NotFound<string>>> GetChapters(string MangaId, [FromQuery]int page = 1, [FromQuery]int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return TypedResults.BadRequest();

        if(await context.Mangas.Include(m => m.Chapters).FirstOrDefaultAsync(m => m.Key == MangaId) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));

        PagedResponse<Chapter> pagedResponse = manga.Chapters.OrderDescending().CreatePagedResponse(page, pageSize)
            .ToType(c =>
            {
                IEnumerable<MangaConnectorId> ids = c.MangaConnectorIds.Select(id =>
                    new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
                return new Chapter(c.Key, c.ParentMangaId, c.VolumeNumber, c.ChapterNumber, c.Title, ids, c.Downloaded,
                    c.FileName);
            });

        return TypedResults.Ok(pagedResponse);
    }
    
    /// <summary>
    /// Returns all downloaded <see cref="Chapter"/> for <see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <param name="page">Page to request (default 1)</param>
    /// <param name="pageSize">Size of Page (default 10)</param>
    /// <response code="200"></response>
    /// <response code="400">Page data wrong</response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found.</response>
    /// <response code="500">Error during Database request</response>
    [HttpGet("Downloaded/{MangaId}")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok<PagedResponse<Chapter>>, BadRequest, NotFound<string>, InternalServerError<string>>> GetChaptersDownloaded(string MangaId, [FromQuery]int page = 1, [FromQuery]int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return TypedResults.BadRequest();

        if(await context.Mangas.Include(m => m.Chapters.Where(c => c.Downloaded)).FirstOrDefaultAsync(m => m.Key == MangaId) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));

        PagedResponse<Chapter> pagedResponse = manga.Chapters.OrderDescending().CreatePagedResponse(page, pageSize)
            .ToType(c =>
            {
                IEnumerable<MangaConnectorId> ids = c.MangaConnectorIds.Select(id =>
                    new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
                return new Chapter(c.Key, c.ParentMangaId, c.VolumeNumber, c.ChapterNumber, c.Title, ids, c.Downloaded,
                    c.FileName);
            });

        return TypedResults.Ok(pagedResponse);
    }
    
    /// <summary>
    /// Returns all <see cref="Chapter"/> not downloaded for <see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="400">Page data wrong</response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found.</response>
    [HttpGet("NotDownloaded/{MangaId}")]
    [ProducesResponseType<List<Chapter>>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<PagedResponse<Chapter>>, BadRequest, NotFound<string>>> GetChaptersNotDownloaded(string MangaId, [FromQuery]int page = 1, [FromQuery]int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return TypedResults.BadRequest();

        if(await context.Mangas.Include(m => m.Chapters.Where(c => c.Downloaded == false)).FirstOrDefaultAsync(m => m.Key == MangaId) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));

        PagedResponse<Chapter> pagedResponse = manga.Chapters.OrderDescending().CreatePagedResponse(page, pageSize)
            .ToType(c =>
            {
                IEnumerable<MangaConnectorId> ids = c.MangaConnectorIds.Select(id =>
                    new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
                return new Chapter(c.Key, c.ParentMangaId, c.VolumeNumber, c.ChapterNumber, c.Title, ids, c.Downloaded,
                    c.FileName);
            });

        return TypedResults.Ok(pagedResponse);
    }
    
    /// <summary>
    /// Returns the latest <see cref="Chapter"/> of requested <see cref="Schema.MangaContext.Manga"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found.</response>
    [HttpGet("LatestAvailable/{MangaId}")]
    [ProducesResponseType<int>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<Chapter>, NoContent, NotFound<string>>> GetLatestChapter(string MangaId)
    {
        if(await context.Chapters.Include(ch => ch.MangaConnectorIds)
               .Where(ch => ch.ParentMangaId == MangaId)
               .ToListAsync(HttpContext.RequestAborted)
           is not { } dbChapters)
            return TypedResults.NotFound(nameof(MangaId));

        Schema.MangaContext.Chapter? c = dbChapters.Max();
        if (c is null)
            return TypedResults.NoContent();
            
        IEnumerable<MangaConnectorId> ids = c.MangaConnectorIds.Select(id =>
            new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
        return TypedResults.Ok(new Chapter(c.Key, c.ParentMangaId, c.VolumeNumber, c.ChapterNumber, c.Title, ids, c.Downloaded, c.FileName));
    }
    
    /// <summary>
    /// Returns the latest <see cref="Chapter"/> of requested <see cref="Schema.MangaContext.Manga"/> that is downloaded
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found.</response>
    /// <response code="412">Could not retrieve the maximum chapter-number</response>
    /// <response code="503">Retry after timeout, updating value</response>
    [HttpGet("LatestDownloaded/{MangaId}")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status412PreconditionFailed)]
    [ProducesResponseType(Status503ServiceUnavailable)]
    public async Task<Results<Ok<Chapter>, NoContent, NotFound<string>, StatusCodeHttpResult>>  GetLatestChapterDownloaded(string MangaId)
    {
        if(await context.Chapters.Include(ch => ch.MangaConnectorIds)
               .Where(ch => ch.ParentMangaId == MangaId && ch.Downloaded)
               .ToListAsync(HttpContext.RequestAborted)
           is not { } dbChapters)
            return TypedResults.NotFound(nameof(MangaId));

        Schema.MangaContext.Chapter? c = dbChapters.Max();
        if (c is null)
            return TypedResults.NoContent();
            
        IEnumerable<MangaConnectorId> ids = c.MangaConnectorIds.Select(id =>
            new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
        return TypedResults.Ok(new Chapter(c.Key, c.ParentMangaId, c.VolumeNumber, c.ChapterNumber, c.Title, ids, c.Downloaded, c.FileName));
    }

    /// <summary>
    /// Configure the <see cref="Chapter"/> cut-off for <see cref="Schema.MangaContext.Manga"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <param name="chapterThreshold">Threshold (<see cref="Chapter"/> ChapterNumber)</param>
    /// <response code="202"></response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("IgnoreBefore/{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> IgnoreChaptersBefore(string MangaId, [FromBody]float chapterThreshold)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));
        
        manga.IgnoreChaptersBefore = chapterThreshold;
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);

        return TypedResults.Ok();
    }
    
    /// <summary>
    /// Returns <see cref="Chapter"/> with <paramref name="ChapterId"/>
    /// </summary>
    /// <param name="ChapterId"><see cref="Chapter"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Chapter"/> with <paramref name="ChapterId"/> not found</response>
    [HttpGet("{ChapterId}")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<Chapter>, NotFound<string>>> GetChapter (string ChapterId)
    {
        if (await context.Chapters.FirstOrDefaultAsync(c => c.Key == ChapterId, HttpContext.RequestAborted) is not { } chapter)
            return TypedResults.NotFound(nameof(ChapterId));
        
        IEnumerable<MangaConnectorId> ids = chapter.MangaConnectorIds.Select(id =>
            new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
        return TypedResults.Ok(new Chapter(chapter.Key, chapter.ParentMangaId, chapter.VolumeNumber, chapter.ChapterNumber, chapter.Title,ids, chapter.Downloaded, chapter.FileName));
    }
    
    /// <summary>
    /// Deletes <see cref="Chapter"/> with <paramref name="ChapterId"/>
    /// </summary>
    /// <param name="ChapterId"><see cref="Chapter"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Chapter"/> with <paramref name="ChapterId"/> not found</response>
    [HttpDelete("{ChapterId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok, NotFound<string>>> DeleteChapter (string ChapterId)
    {
        if (await context.Chapters.Where(c => c.Key == ChapterId).ExecuteDeleteAsync<Schema.MangaContext.Chapter>(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(ChapterId));
        return TypedResults.Ok();
    }

    /// <summary>
    /// Returns the <see cref="MangaConnectorId{Chapter}"/> with <see cref="MangaConnectorId{Chapter}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="MangaConnectorId{Chapter}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnectorId{Manga}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpGet("ConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType<MangaConnectorId>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<MangaConnectorId>, NotFound<string>>> GetChapterMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToChapter.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, HttpContext.RequestAborted) is not { } mcIdManga)
            return TypedResults.NotFound(nameof(MangaConnectorIdId));

        MangaConnectorId result = new (mcIdManga.Key, mcIdManga.MangaConnectorName, mcIdManga.ObjId, mcIdManga.WebsiteUrl, mcIdManga.UseForDownload);
        
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Deletes the <see cref="MangaConnectorId{Chapter}"/> with <see cref="MangaConnectorId{Chapter}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="MangaConnectorId{Chapter}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnectorId{Manga}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpDelete("ConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok, NotFound<string>>> DeleteChapterMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToChapter.Where(c => c.Key == MangaConnectorIdId).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(MangaConnectorIdId));
        return TypedResults.Ok();
    }
}