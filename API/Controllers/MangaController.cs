using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;
using API.Workers;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
public class MangaController(IServiceScope scope) : Controller
{
    /// <summary>
    /// Returns all cached <see cref="Manga"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetAllManga()
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        Manga[] ret = context.Mangas.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns all cached <see cref="Manga"/> with <paramref name="MangaIds"/>
    /// </summary>
    /// <param name="MangaIds">Array of <<see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetManga([FromBody]string[] MangaIds)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        Manga[] ret = context.Mangas.Where(m => MangaIds.Contains(m.Key)).ToArray();
        return Ok(ret);
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
    public IActionResult GetManga(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        return Ok(manga);
    }

    /// <summary>
    /// Delete <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><<see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteManga(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        
        context.Mangas.Remove(manga);
        
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);
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
    public IActionResult MergeIntoManga(string MangaIdFrom, string MangaIdInto)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaIdFrom) is not { } from)
            return NotFound(nameof(MangaIdFrom));
        if (context.Mangas.Find(MangaIdInto) is not { } into)
            return NotFound(nameof(MangaIdInto));
        
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
    public IActionResult GetCover(string MangaId, [FromQuery]int? width, [FromQuery]int? height)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        
        if (!System.IO.File.Exists(manga.CoverFileNameInCache))
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is DownloadCoverFromMangaconnectorWorker w && w.MangaConnectorId.ObjId == MangaId))
            {
                Response.Headers.Append("Retry-After", $"{TrangaSettings.workCycleTimeout * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, TrangaSettings.workCycleTimeout * 2  / 1000);
            }
            else
                return NoContent();
        }

        Image image = Image.Load(manga.CoverFileNameInCache);

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
        image.Save(ms, new JpegEncoder(){Quality = 100});
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
    public IActionResult GetChapters(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        
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
    public IActionResult GetChaptersDownloaded(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));

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
    public IActionResult GetChaptersNotDownloaded(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        
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
    public IActionResult GetLatestChapter(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        
        List<Chapter> chapters = manga.Chapters.ToList();
        if (chapters.Count == 0)
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is RetrieveMangaChaptersFromMangaconnectorWorker w && w.MangaConnectorId.ObjId == MangaId && w.State < WorkerExecutionState.Completed))
            {
                Response.Headers.Append("Retry-After", $"{TrangaSettings.workCycleTimeout * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, TrangaSettings.workCycleTimeout * 2/ 1000);
            }else
                return Ok(0);
        }
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(Status500InternalServerError, "Max chapter could not be found");
        
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
    public IActionResult GetLatestChapterDownloaded(string MangaId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        
        List<Chapter> chapters = manga.Chapters.ToList();
        if (chapters.Count == 0)
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is RetrieveMangaChaptersFromMangaconnectorWorker w && w.MangaConnectorId.ObjId == MangaId && w.State < WorkerExecutionState.Completed))
            {
                Response.Headers.Append("Retry-After", $"{TrangaSettings.workCycleTimeout * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, TrangaSettings.workCycleTimeout * 2/ 1000);
            }else
                return NoContent();
        }
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(Status412PreconditionFailed, "Max chapter could not be found");
        
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
    public IActionResult IgnoreChaptersBefore(string MangaId, [FromBody]float chapterThreshold)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        
        manga.IgnoreChaptersBefore = chapterThreshold;
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);

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
    public IActionResult MoveFolder(string MangaId, string LibraryId)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound(nameof(MangaId));
        if(context.LocalLibraries.Find(LibraryId) is not { } library)
            return NotFound(nameof(LibraryId));

        MoveMangaLibraryWorker moveLibrary = new(manga, library, scope);
        UpdateChaptersDownloadedWorker updateDownloadedFiles = new(manga, scope, [moveLibrary]);
        
        Tranga.AddWorkers([moveLibrary, updateDownloadedFiles]);
        
        return Accepted();
    }
}