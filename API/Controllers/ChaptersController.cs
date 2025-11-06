using API.Controllers.DTOs;
using API.Controllers.Requests;
using API.Schema.MangaContext;
using API.Workers.MangaDownloadWorkers;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Chapter = API.Controllers.DTOs.Chapter;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class ChaptersController(MangaContext context) : ControllerBase
{
    /// <summary>
    /// Returns all <see cref="Schema.MangaContext.Chapter"/> of <see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <param name="filter"></param>
    /// <param name="page">Page to request (default 1)</param>
    /// <param name="pageSize">Size of Page (default 10)</param>
    /// <response code="200"></response>
    /// <response code="400">Page data wrong</response>
    /// <response code="500">Error during Database request</response>
    [HttpPost("Manga/{MangaId}")]
    [ProducesResponseType<PagedResponse<Chapter>>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<PagedResponse<Chapter>>, BadRequest, InternalServerError>> GetChapters(string MangaId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]ChapterFilterRecord? filter = null, [FromQuery]int page = 1, [FromQuery]int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return TypedResults.BadRequest();

        IQueryable<Schema.MangaContext.Chapter> queryable = context.Chapters
            .Include(ch => ch.MangaConnectorIds)
            .Where(ch => ch.ParentMangaId == MangaId);
        
        if (filter is not null)
        {
            if(filter.Downloaded.HasValue)
                queryable = queryable.Where(ch => ch.Downloaded == filter.Downloaded.Value);
            if(filter.Name is not null && !string.IsNullOrWhiteSpace(filter.Name))
                queryable = queryable.Where(ch => ch.Title != null && ch.Title.Contains(filter.Name));
            if(filter.VolumeNumber is not null)
                queryable = queryable.Where(ch => ch.VolumeNumber == filter.VolumeNumber);
            if(filter.ChapterNumber is not null && !string.IsNullOrWhiteSpace(filter.ChapterNumber))
                queryable = queryable.Where(ch => ch.ChapterNumber == filter.ChapterNumber);
        }

        if (await queryable.ToListAsync(HttpContext.RequestAborted) is not { } dbChapters)
            return TypedResults.InternalServerError();
        PagedResponse<Chapter> pagedResponse = dbChapters.OrderDescending().CreatePagedResponse(page, pageSize)
            .ToType(c =>
            {
                IEnumerable<DTOs.MangaConnectorId<Chapter>> ids = c.MangaConnectorIds.Select(id =>
                    new DTOs.MangaConnectorId<Chapter>(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
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
            
        IEnumerable<DTOs.MangaConnectorId<Chapter>> ids = c.MangaConnectorIds.Select(id =>
            new DTOs.MangaConnectorId<Chapter>(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
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
            
        IEnumerable<DTOs.MangaConnectorId<Chapter>> ids = c.MangaConnectorIds.Select(id =>
            new DTOs.MangaConnectorId<Chapter>(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
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
        
        IEnumerable<DTOs.MangaConnectorId<Chapter>> ids = chapter.MangaConnectorIds.Select(id =>
            new DTOs.MangaConnectorId<Chapter>(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
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
        if (await context.Chapters.Where(c => c.Key == ChapterId).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(ChapterId));
        return TypedResults.Ok();
    }

    /// <summary>
    /// Returns the <see cref="DTOs.MangaConnectorId{Chapter}"/> with <see cref="DTOs.MangaConnectorId{Chapter}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="DTOs.MangaConnectorId{Chapter}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="DTOs.MangaConnectorId{Chapter}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpGet("ConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType<DTOs.MangaConnectorId<Chapter>>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<DTOs.MangaConnectorId<Chapter>>, NotFound<string>>> GetChapterMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToChapter.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, HttpContext.RequestAborted) is not { } mcIdManga)
            return TypedResults.NotFound(nameof(MangaConnectorIdId));

        DTOs.MangaConnectorId<Chapter> result = new (mcIdManga.Key, mcIdManga.MangaConnectorName, mcIdManga.ObjId, mcIdManga.WebsiteUrl, mcIdManga.UseForDownload);
        
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Deletes the <see cref="DTOs.MangaConnectorId{Chapter}"/> with <see cref="DTOs.MangaConnectorId{Chapter}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="DTOs.MangaConnectorId{Chapter}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="DTOs.MangaConnectorId{Chapter}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpDelete("ConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok, NotFound<string>>> DeleteChapterMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToChapter.Where(c => c.Key == MangaConnectorIdId).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(MangaConnectorIdId));
        return TypedResults.Ok();
    }

    /// <summary>
    /// (Un-)Marks <see cref="Chapter"/> as requested for Download from <see cref="API.MangaConnectors.MangaConnector"/>
    /// </summary>
    /// <param name="ChapterId"><see cref="Chapter"/> with <paramref name="ChapterId"/></param>
    /// <param name="MangaConnectorName"><see cref="API.MangaConnectors.MangaConnector"/> with <paramref name="MangaConnectorName"/></param>
    /// <param name="IsRequested">true to mark as requested, false to mark as not-requested</param>
    /// <response code="200"></response>
    /// <response code="404"><paramref name="ChapterId"/> or <paramref name="MangaConnectorName"/> not found</response>
    /// <response code="428"><see cref="Chapter"/> is not linked to <see cref="API.MangaConnectors.MangaConnector"/> yet. Search for <see cref="Chapter"/> on <see cref="API.MangaConnectors.MangaConnector"/> first (to create a <see cref="DTOs.MangaConnectorId{T}"/>).</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{ChapterId}/DownloadFrom/{MangaConnectorName}/{IsRequested}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound,  "text/plain")]
    [ProducesResponseType<string>(Status428PreconditionRequired,  "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError,  "text/plain")]
    public async Task<Results<Ok, NotFound<string>, StatusCodeHttpResult, InternalServerError<string>>> MarkAsRequested(string ChapterId, string MangaConnectorName, bool IsRequested)
    {
        if (await context.Chapters.FirstOrDefaultAsync(ch => ch.Key == ChapterId, HttpContext.RequestAborted) is not { } _)
            return TypedResults.NotFound(nameof(ChapterId));
        if(!Tranga.TryGetMangaConnector(MangaConnectorName, out API.MangaConnectors.MangaConnector? _))
            return TypedResults.NotFound(nameof(MangaConnectorName));

        if (await context.MangaConnectorToChapter
                .FirstOrDefaultAsync(id => id.MangaConnectorName == MangaConnectorName && id.ObjId == ChapterId, HttpContext.RequestAborted)
            is not { } chId)
        {
            return TypedResults.StatusCode(Status428PreconditionRequired);
        }

        chId.UseForDownload = IsRequested;
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);

        if (IsRequested)
        {
            DownloadChapterFromMangaconnectorWorker worker = new(chId);
            Tranga.AddWorker(worker);
        }
        
        return TypedResults.Ok();
    }
}