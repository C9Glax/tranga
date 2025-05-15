using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
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
public class MangaController(PgsqlContext context, ILog Log) : Controller
{
    /// <summary>
    /// Returns all cached Manga
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetAllManga()
    {
        Manga[] ret = context.Mangas.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns all cached Manga with IDs
    /// </summary>
    /// <param name="ids">Array of Manga-IDs</param>
    /// <response code="200"></response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetManga([FromBody]string[] ids)
    {
        Manga[] ret = context.Mangas.Where(m => ids.Contains(m.MangaId)).ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Return Manga with ID
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found</response>
    [HttpGet("{MangaId}")]
    [ProducesResponseType<Manga>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetManga(string MangaId)
    {
        Manga? ret = context.Mangas.Find(MangaId);
        if (ret is null)
            return NotFound();
        return Ok(ret);
    }

    /// <summary>
    /// Delete Manga with ID
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteManga(string MangaId)
    {
        try
        {
            Manga? ret = context.Mangas.Find(MangaId);
            if (ret is null)
                return NotFound();
            
            context.Remove(ret);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Returns Cover of Manga
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <param name="width">If width is provided, height needs to also be provided</param>
    /// <param name="height">If height is provided, width needs to also be provided</param>
    /// <response code="200">JPEG Image</response>
    /// <response code="204">Cover not loaded</response>
    /// <response code="400">The formatting-request was invalid</response>
    /// <response code="404">Manga with ID not found</response>
    /// <response code="503">Retry later, downloading cover</response>
    [HttpGet("{MangaId}/Cover")]
    [ProducesResponseType<byte[]>(Status200OK,"image/jpeg")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<int>(Status503ServiceUnavailable, "text/plain")]
    public IActionResult GetCover(string MangaId, [FromQuery]int? width, [FromQuery]int? height)
    {
        DateTime requestStarted = HttpContext.Features.Get<IHttpRequestTimeFeature>()?.RequestTime ?? DateTime.Now;
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        if (!System.IO.File.Exists(m.CoverFileNameInCache))
        {
            List<Job> coverDownloadJobs = context.Jobs.Where(j => j.JobType == JobType.DownloadMangaCoverJob).ToList();
            if (coverDownloadJobs.Any(j => j is DownloadMangaCoverJob dmc && dmc.MangaId == MangaId))
            {
                Response.Headers.Add("Retry-After", $"{TrangaSettings.startNewJobTimeoutMs * coverDownloadJobs.Count() * 2  / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, TrangaSettings.startNewJobTimeoutMs * coverDownloadJobs.Count() * 2  / 1000);
            }
            else
                return NoContent();
        }

        Image image = Image.Load(m.CoverFileNameInCache);

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
        return File(ms.GetBuffer(), "image/jpeg");
    }

    /// <summary>
    /// Returns all Chapters of Manga
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found</response>
    [HttpGet("{MangaId}/Chapters")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetChapters(string MangaId)
    {
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        Chapter[] ret = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns all downloaded Chapters for Manga with ID
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404">Manga with ID not found.</response>
    [HttpGet("{MangaId}/Chapters/Downloaded")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetChaptersDownloaded(string MangaId)
    {
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId && c.Downloaded == true).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
        return Ok(chapters);
    }
    
    /// <summary>
    /// Returns all Chapters not downloaded for Manga with ID
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404">Manga with ID not found.</response>
    [HttpGet("{MangaId}/Chapters/NotDownloaded")]
    [ProducesResponseType<Chapter[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetChaptersNotDownloaded(string MangaId)
    {
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId && c.Downloaded == false).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
        return Ok(chapters);
    }
    
    /// <summary>
    /// Returns the latest Chapter of requested Manga available on Website
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404">Manga with ID not found.</response>
    /// <response code="500">Could not retrieve the maximum chapter-number</response>
    /// <response code="503">Retry after timeout, updating value</response>
    [HttpGet("{MangaId}/Chapter/LatestAvailable")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    [ProducesResponseType<int>(Status503ServiceUnavailable, "text/plain")]
    public IActionResult GetLatestChapter(string MangaId)
    {
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToList();
        if (chapters.Count == 0)
        {
            List<Job> retrieveChapterJobs = context.Jobs.Where(j => j.JobType == JobType.RetrieveChaptersJob).ToList();
            if (retrieveChapterJobs.Any(j => j is RetrieveChaptersJob rcj && rcj.MangaId == MangaId))
            {
                Response.Headers.Add("Retry-After", $"{TrangaSettings.startNewJobTimeoutMs * retrieveChapterJobs.Count() * 2 / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, TrangaSettings.startNewJobTimeoutMs * retrieveChapterJobs.Count() * 2/ 1000);
            }else
                return NoContent();
        }
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(500, "Max chapter could not be found");
        
        return Ok(max);
    }
    
    /// <summary>
    /// Returns the latest Chapter of requested Manga that is downloaded
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404">Manga with ID not found.</response>
    /// <response code="500">Could not retrieve the maximum chapter-number</response>
    /// <response code="503">Retry after timeout, updating value</response>
    [HttpGet("{MangaId}/Chapter/LatestDownloaded")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    [ProducesResponseType<int>(Status503ServiceUnavailable, "text/plain")]
    public IActionResult GetLatestChapterDownloaded(string MangaId)
    {
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId && c.Downloaded == true).ToList();
        if (chapters.Count == 0)
        {
            List<Job> retrieveChapterJobs = context.Jobs.Where(j => j.JobType == JobType.RetrieveChaptersJob).ToList();
            if (retrieveChapterJobs.Any(j => j is RetrieveChaptersJob rcj && rcj.MangaId == MangaId))
            {
                Response.Headers.Add("Retry-After", $"{TrangaSettings.startNewJobTimeoutMs * retrieveChapterJobs.Count() * 2  / 1000:D}");
                return StatusCode(Status503ServiceUnavailable, TrangaSettings.startNewJobTimeoutMs * retrieveChapterJobs.Count() * 2  / 1000);
            }else
                return NoContent();
        }
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(500, "Max chapter could not be found");
        
        return Ok(max);
    }

    /// <summary>
    /// Configure the cut-off for Manga
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <param name="chapterThreshold">Threshold (Chapter Number)</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{MangaId}/IgnoreChaptersBefore")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult IgnoreChaptersBefore(string MangaId, [FromBody]float chapterThreshold)
    {
        Manga? m = context.Mangas.Find(MangaId);
        if (m is null)
            return NotFound();
        
        try
        {
            m.IgnoreChaptersBefore = chapterThreshold;
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Move Manga to different ToLibrary
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <param name="LibraryId">ToLibrary-Id</param>
    /// <response code="202">Folder is going to be moved</response>
    /// <response code="404">MangaId or LibraryId not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MangaId}/ChangeLibrary/{LibraryId}")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult MoveFolder(string MangaId, string LibraryId)
    {
        if (context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(context.LocalLibraries.Find(LibraryId) is not { } library)
            return NotFound();

        MoveMangaLibraryJob moveLibrary = new(manga, library);
        UpdateFilesDownloadedJob updateDownloadedFiles = new(manga, 0, dependsOnJobs: [moveLibrary]);
        
        try
        {
            context.Jobs.AddRange(moveLibrary, updateDownloadedFiles);
            context.SaveChanges();
            return Accepted();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
}