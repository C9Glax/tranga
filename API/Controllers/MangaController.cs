using API.APIEndpointRecords;
using API.Schema;
using API.Schema.Jobs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MangaController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Returns all cached Manga
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult GetAllManga()
    {
        Manga[] ret = context.Manga.ToArray();
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
        Manga[] ret = context.Manga.Where(m => ids.Contains(m.MangaId)).ToArray();
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
        Manga? ret = context.Manga.Find(MangaId);
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
            Manga? ret = context.Manga.Find(MangaId);
            if (ret is null)
                return NotFound();
            
            context.Remove(ret);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Returns Cover of Manga
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <param name="formatRequest">Formatting/Resizing Request</param>
    /// <response code="200">JPEG Image</response>
    /// <response code="204">Cover not loaded</response>
    /// <response code="400">The formatting-request was invalid</response>
    /// <response code="404">Manga with ID not found</response>
    [HttpPost("{MangaId}/Cover")]
    [ProducesResponseType<byte[]>(Status200OK,"image/jpeg")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetCover(string MangaId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]CoverFormatRequestRecord? formatRequest)
    {
        Manga? m = context.Manga.Find(MangaId);
        if (m is null)
            return NotFound();
        if (!System.IO.File.Exists(m.CoverFileNameInCache))
            return NoContent();

        Image image = Image.Load(m.CoverFileNameInCache);

        if (formatRequest is not null)
        {
            if(!formatRequest.Validate())
                return BadRequest();
            image.Mutate(i => i.ApplyProcessor(new ResizeProcessor(new ResizeOptions()
            {
                Mode = ResizeMode.Max,
                Size = formatRequest.size
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
        Manga? m = context.Manga.Find(MangaId);
        if (m is null)
            return NotFound();
        
        Chapter[] ret = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns the latest Chapter of requested Manga available on Website
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="204">No available chapters</response>
    /// <response code="404">Manga with ID not found.</response>
    /// <response code="500">Could not retrieve the maximum chapter-number</response>
    [HttpGet("{MangaId}/Chapter/LatestAvailable")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult GetLatestChapter(string MangaId)
    {
        Manga? m = context.Manga.Find(MangaId);
        if (m is null)
            return NotFound();
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
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
    [HttpGet("{MangaId}/Chapter/LatestDownloaded")]
    [ProducesResponseType<Chapter>(Status200OK, "application/json")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult GetLatestChapterDownloaded(string MangaId)
    {
        Manga? m = context.Manga.Find(MangaId);
        if (m is null)
            return NotFound();
        
        List<Chapter> chapters = context.Chapters.Where(c => c.ParentMangaId == m.MangaId && c.Downloaded == true).ToList();
        if (chapters.Count == 0)
            return NoContent();
        
        Chapter? max = chapters.Max();
        if (max is null)
            return StatusCode(500, "Max chapter could not be found");
        
        return Ok(max);
    }
    
    /// <summary>
    /// Configure the cut-off for Manga
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Manga with ID not found.</response>
    [HttpPatch("{MangaId}/IgnoreChaptersBefore")]
    [ProducesResponseType<float>(Status200OK, "text/plain")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult IgnoreChaptersBefore(string MangaId)
    {
        Manga? m = context.Manga.Find(MangaId);
        if (m is null)
            return NotFound();
        return Ok(m.IgnoreChapterBefore);
    }
    
    /// <summary>
    /// Move the Directory the .cbz-files are located in
    /// </summary>
    /// <param name="MangaId">Manga-ID</param>
    /// <param name="folder">New Directory-Path</param>
    /// <response code="202">Folder is going to be moved</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MangaId}/MoveFolder")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult MoveFolder(string MangaId, [FromBody]string folder)
    {
        Manga? manga = context.Manga.Find(MangaId);
        if (manga is null)
            return NotFound();
        MoveFileOrFolderJob dep = manga.UpdateFolderName(TrangaSettings.downloadLocation, folder);
        UpdateFilesDownloadedJob up = new UpdateFilesDownloadedJob(0, manga.MangaId, null, [dep.JobId]);
        
        try
        {
            context.Jobs.AddRange([dep, up]);
            context.SaveChanges();
            return Accepted();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}