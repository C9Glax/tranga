using API.MangaConnectors;
using API.Schema.MangaContext;
using API.Workers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MangaController(MangaContext context) : Controller
{
    
    /// <summary>
    /// Returns all cached <see cref="Manga"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetAllManga ()
    {
        if(await context.Mangas.ToArrayAsync(HttpContext.RequestAborted) is not { } result)
            return StatusCode(Status500InternalServerError);
        
        return Ok(result);
    }
    
    /// <summary>
    /// Returns all cached <see cref="Manga"/>.Keys
    /// </summary>
    /// <response code="200"><see cref="Manga"/> Keys/IDs</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("Keys")]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetAllMangaKeys ()
    {
        if(await context.Mangas.Select(m => m.Key).ToArrayAsync(HttpContext.RequestAborted) is not { } result)
            return StatusCode(Status500InternalServerError);
        
        return Ok(result);
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> that are being downloaded from at least one <see cref="MangaConnector"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("Downloading")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetMangaDownloading ()
    {
        if(await context.MangaIncludeAll()
               .Where(m => m.MangaConnectorIds.Any(id => id.UseForDownload))
               .ToArrayAsync(HttpContext.RequestAborted) is not { } result)
            return StatusCode(Status500InternalServerError);
        
        return Ok(result);
    }
    
    /// <summary>
    /// Returns all cached <see cref="Manga"/> with <paramref name="MangaIds"/>
    /// </summary>
    /// <param name="MangaIds">Array of <see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetManga ([FromBody]string[] MangaIds)
    {
        if(await context.MangaIncludeAll()
               .Where(m => MangaIds.Contains(m.Key))
               .ToArrayAsync(HttpContext.RequestAborted) is not { } result)
            return StatusCode(Status500InternalServerError);
        
        return Ok(result);
    }

    /// <summary>
    /// Return <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    [HttpGet("{MangaId}")]
    [ProducesResponseType<Manga>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetManga (string MangaId)
    {
        if (await context.MangaIncludeAll().FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        return Ok(manga);
    }

    /// <summary>
    /// Delete <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> DeleteManga (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        context.Mangas.Remove(manga);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Ok();
    }


    /// <summary>
    /// Merge two <see cref="Manga"/> into one. THIS IS NOT REVERSIBLE!
    /// </summary>
    /// <param name="MangaIdFrom"><see cref="Manga"/>.Key of <see cref="Manga"/> merging data from (getting deleted)</param>
    /// <param name="MangaIdInto"><see cref="Manga"/>.Key of <see cref="Manga"/> merging data into</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaIdFrom"/> or <paramref name="MangaIdInto"/> not found</response>
    [HttpPatch("{MangaIdFrom}/MergeInto/{MangaIdInto}")]
    [ProducesResponseType<byte[]>(Status200OK,"image/jpeg")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> MergeIntoManga (string MangaIdFrom, string MangaIdInto)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaIdFrom, HttpContext.RequestAborted) is not { } from)
            return NotFound(nameof(MangaIdFrom));
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaIdInto, HttpContext.RequestAborted) is not { } into)
            return NotFound(nameof(MangaIdInto));
        
        foreach (CollectionEntry collectionEntry in context.Entry(from).Collections)
            await collectionEntry.LoadAsync(HttpContext.RequestAborted);
        await context.Entry(from).Navigation(nameof(Manga.Library)).LoadAsync(HttpContext.RequestAborted);
        
        foreach (CollectionEntry collectionEntry in context.Entry(into).Collections)
            await collectionEntry.LoadAsync(HttpContext.RequestAborted);
        await context.Entry(into).Navigation(nameof(Manga.Library)).LoadAsync(HttpContext.RequestAborted);
        
        BaseWorker[] newJobs = into.MergeFrom(from, context);
        Tranga.AddWorkers(newJobs);
        
        return Ok();
    }

    /// <summary>
    /// Returns Cover of <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="width">If <paramref name="width"/> is provided, <paramref name="height"/> needs to also be provided</param>
    /// <param name="height">If <paramref name="height"/> is provided, <paramref name="width"/> needs to also be provided</param>
    /// <response code="200">JPEG Image</response>
    /// <response code="204">Cover not loaded</response>
    /// <response code="400">The formatting-request was invalid</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="503">Retry later, downloading cover</response>
    [HttpGet("{MangaId}/Cover")]
    [ProducesResponseType<byte[]>(Status200OK,"image/jpeg")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<int>(Status503ServiceUnavailable, "text/plain")]
    public async Task<IActionResult> GetCover (string MangaId, [FromQuery]int? width, [FromQuery]int? height)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        if (!System.IO.File.Exists(manga.CoverFileNameInCache))
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is DownloadCoverFromMangaconnectorWorker w && context.MangaConnectorToManga.Find(w.MangaConnectorIdId)?.ObjId == MangaId))
            {
                Response.Headers.Append("Retry-After", $"{Tranga.Settings.WorkCycleTimeoutMs * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, Tranga.Settings.WorkCycleTimeoutMs * 2  / 1000);
            }
            else
                return NoContent();
        }

        Image image = await Image.LoadAsync(manga.CoverFileNameInCache, HttpContext.RequestAborted);

        if (width is { } w && height is { } h)
        {
            if (width < 10 || height < 10 || width > 65535 || height > 65535)
                return BadRequest();
            image.Mutate(i => i.ApplyProcessor(new ResizeProcessor(new ResizeOptions()
            {
                Mode = ResizeMode.Max,
                Size = new Size(w, h)
            }, image.Size)));
        }
        
        using MemoryStream ms = new();
        await image.SaveAsync(ms, new JpegEncoder(){Quality = 100}, HttpContext.RequestAborted);
        DateTime lastModified = new FileInfo(manga.CoverFileNameInCache).LastWriteTime;
        HttpContext.Response.Headers.CacheControl = "public";
        return File(ms.GetBuffer(), "image/jpeg", new DateTimeOffset(lastModified), EntityTagHeaderValue.Parse($"\"{lastModified.Ticks}\""));
    }

    /// <summary>
    /// Returns all <see cref="Chapter"/> of <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    [HttpGet("{MangaId}/Chapters")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetChapters (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        await context.Entry(manga).Collection(m => m.Chapters).LoadAsync();
        
        Chapter[] chapters = manga.Chapters.ToArray();
        return Ok(chapters);
    }
    
    /// <summary>
    /// Returns all downloaded <see cref="Chapter"/> for <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found.</response>
    [HttpGet("{MangaId}/Chapters/Downloaded")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetChaptersDownloaded (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        await context.Entry(manga).Collection(m => m.Chapters).LoadAsync();

        List<Chapter> chapters = manga.Chapters.Where(c => c.Downloaded).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
        return Ok(chapters);
    }
    
    /// <summary>
    /// Returns all <see cref="Chapter"/> not downloaded for <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found.</response>
    [HttpGet("{MangaId}/Chapters/NotDownloaded")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetChaptersNotDownloaded (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        await context.Entry(manga).Collection(m => m.Chapters).LoadAsync(HttpContext.RequestAborted);
        
        List<Chapter> chapters = manga.Chapters.Where(c => c.Downloaded == false).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
        return Ok(chapters);
    }
    
    /// <summary>
    /// Returns the latest <see cref="Chapter"/> of requested <see cref="Manga"/> available on <see cref="MangaConnector"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found.</response>
    /// <response code="412">Could not retrieve the maximum chapter-number</response>
    /// <response code="503">Retry after timeout, updating value</response>
    [HttpGet("{MangaId}/Chapter/LatestAvailable")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    [ProducesResponseType<int>(Status503ServiceUnavailable, "text/plain")]
    public async Task<IActionResult> GetLatestChapter (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        await context.Entry(manga).Collection(m => m.Chapters).LoadAsync(HttpContext.RequestAborted);
        
        List<Chapter> chapters = manga.Chapters.ToList();
        if (chapters.Count == 0)
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is RetrieveMangaChaptersFromMangaconnectorWorker w && context.MangaConnectorToManga.Find(w.MangaConnectorIdId)?.ObjId == MangaId && w.State < WorkerExecutionState.Completed))
            {
                Response.Headers.Append("Retry-After", $"{Tranga.Settings.WorkCycleTimeoutMs * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, Tranga.Settings.WorkCycleTimeoutMs * 2/ 1000);
            }else
                return Ok(0);
        }
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(Status500InternalServerError, "Max chapter could not be found");
        
        foreach (CollectionEntry collectionEntry in context.Entry(max).Collections)
            await collectionEntry.LoadAsync(HttpContext.RequestAborted);
        await context.Entry(max).Navigation(nameof(Chapter.ParentManga)).LoadAsync(HttpContext.RequestAborted);
        
        return Ok(max);
    }
    
    /// <summary>
    /// Returns the latest <see cref="Chapter"/> of requested <see cref="Manga"/> that is downloaded
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found.</response>
    /// <response code="412">Could not retrieve the maximum chapter-number</response>
    /// <response code="503">Retry after timeout, updating value</response>
    [HttpGet("{MangaId}/Chapter/LatestDownloaded")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status412PreconditionFailed, "text/plain")]
    [ProducesResponseType<int>(Status503ServiceUnavailable, "text/plain")]
    public async Task<IActionResult> GetLatestChapterDownloaded (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        await context.Entry(manga).Collection(m => m.Chapters).LoadAsync(HttpContext.RequestAborted);
        
        List<Chapter> chapters = manga.Chapters.ToList();
        if (chapters.Count == 0)
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is RetrieveMangaChaptersFromMangaconnectorWorker w && context.MangaConnectorToManga.Find(w.MangaConnectorIdId)?.ObjId == MangaId && w.State < WorkerExecutionState.Completed))
            {
                Response.Headers.Append("Retry-After", $"{Tranga.Settings.WorkCycleTimeoutMs * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, Tranga.Settings.WorkCycleTimeoutMs * 2/ 1000);
            }else
                return NoContent();
        }
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(Status412PreconditionFailed, "Max chapter could not be found");
        
        foreach (CollectionEntry collectionEntry in context.Entry(max).Collections)
            await collectionEntry.LoadAsync(HttpContext.RequestAborted);
        await context.Entry(max).Navigation(nameof(Chapter.ParentManga)).LoadAsync(HttpContext.RequestAborted);
        
        return Ok(max);
    }

    /// <summary>
    /// Configure the <see cref="Chapter"/> cut-off for <see cref="Manga"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="chapterThreshold">Threshold (<see cref="Chapter"/> ChapterNumber)</param>
    /// <response code="202"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{MangaId}/IgnoreChaptersBefore")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> IgnoreChaptersBefore (string MangaId, [FromBody]float chapterThreshold)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        
        manga.IgnoreChaptersBefore = chapterThreshold;
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);

        return Accepted();
    }

    /// <summary>
    /// Move <see cref="Manga"/> to different <see cref="FileLibrary"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="LibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <response code="202">Folder is going to be moved</response>
    /// <response code="404"><paramref name="MangaId"/> or <paramref name="LibraryId"/> not found</response>
    [HttpPost("{MangaId}/ChangeLibrary/{LibraryId}")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> ChangeLibrary (string MangaId, string LibraryId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));
        if (await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == LibraryId, HttpContext.RequestAborted) is not { } library)
            return NotFound(nameof(LibraryId));
        
        foreach (CollectionEntry collectionEntry in context.Entry(manga).Collections)
            await collectionEntry.LoadAsync(HttpContext.RequestAborted);
        await context.Entry(manga).Navigation(nameof(Manga.Library)).LoadAsync(HttpContext.RequestAborted);

        MoveMangaLibraryWorker moveLibrary = new(manga, library);
        
        Tranga.AddWorkers([moveLibrary]);
        
        return Accepted();
    }

    /// <summary>
    /// (Un-)Marks <see cref="Manga"/> as requested for Download from <see cref="MangaConnector"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/> with <paramref name="MangaId"/></param>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/> with <paramref name="MangaConnectorName"/></param>
    /// <param name="IsRequested">true to mark as requested, false to mark as not-requested</param>
    /// <response code="200"></response>
    /// <response code="404"><paramref name="MangaId"/> or <paramref name="MangaConnectorName"/> not found</response>
    /// <response code="412"><see cref="Manga"/> was not linked to <see cref="MangaConnector"/>, so nothing changed</response>
    /// <response code="428"><see cref="Manga"/> is not linked to <see cref="MangaConnector"/> yet. Search for <see cref="Manga"/> on <see cref="MangaConnector"/> first (to create a <see cref="MangaConnectorId{T}"/>).</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MangaId}/SetAsDownloadFrom/{MangaConnectorName}/{IsRequested}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound,  "text/plain")]
    [ProducesResponseType<string>(Status412PreconditionFailed,  "text/plain")]
    [ProducesResponseType<string>(Status428PreconditionRequired,  "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError,  "text/plain")]
    public async Task<IActionResult> MarkAsRequested (string MangaId, string MangaConnectorName, bool IsRequested)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } _)
            return NotFound(nameof(MangaId));
        if(!Tranga.TryGetMangaConnector(MangaConnectorName, out MangaConnector? _))
            return NotFound(nameof(MangaConnectorName));

        if (context.MangaConnectorToManga
                .FirstOrDefault(id => id.MangaConnectorName == MangaConnectorName && id.ObjId == MangaId)
            is not { } mcId)
        {
            if(IsRequested)
                return StatusCode(Status428PreconditionRequired, "Don't know how to download this Manga from MangaConnector");
            else
                return StatusCode(Status412PreconditionFailed, "Not linked anyways.");
        }

        mcId.UseForDownload = IsRequested;
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        

        DownloadCoverFromMangaconnectorWorker downloadCover = new(mcId);
        RetrieveMangaChaptersFromMangaconnectorWorker retrieveChapters = new(mcId, Tranga.Settings.DownloadLanguage);
        Tranga.AddWorkers([downloadCover, retrieveChapters]);
        
        return Ok();
    }
    
    /// <summary>
    /// Initiate a search for <see cref="Manga"/> on a different <see cref="MangaConnector"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/> with <paramref name="MangaId"/></param>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnector"/> with Name not found</response>
    /// <response code="412"><see cref="MangaConnector"/> with Name is disabled</response>
    [HttpPost("{MangaId}/SearchOn/{MangaConnectorName}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status406NotAcceptable)]
    public async Task<IActionResult> SearchOnDifferentConnector (string MangaId, string MangaConnectorName)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return NotFound(nameof(MangaId));

        return new SearchController(context).SearchManga(MangaConnectorName, manga.Name);
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> which where Authored by <see cref="Author"/> with <paramref name="AuthorId"/>
    /// </summary>
    /// <param name="AuthorId"><see cref="Author"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Author"/> with <paramref name="AuthorId"/></response>
    [HttpGet("WithAuthorId/{AuthorId}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetMangaWithAuthorIds (string AuthorId)
    {
        if (await context.Authors.FirstOrDefaultAsync(a => a.Key == AuthorId, HttpContext.RequestAborted) is not { } author)
            return NotFound(nameof(AuthorId));
        
        return Ok(context.Mangas.Where(m => m.Authors.Contains(author)));
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> with <see cref="Tag"/>
    /// </summary>
    /// <param name="Tag"><see cref="Tag"/>.Tag</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Tag"/> not found</response>
    [HttpGet("WithTag/{Tag}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetMangasWithTag (string Tag)
    {
        if (await context.Tags.FirstOrDefaultAsync(t => t.Tag == Tag, HttpContext.RequestAborted) is not { } tag)
            return NotFound(nameof(Tag));
        
        return Ok(context.Mangas.Where(m => m.MangaTags.Contains(tag)));
    }
}