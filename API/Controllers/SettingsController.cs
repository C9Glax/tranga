using API.MangaDownloadClients;
using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SettingsController(PgsqlContext context, ILog Log) : Controller
{
    /// <summary>
    /// Get all Settings
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<JObject>(Status200OK, "application/json")]
    public IActionResult GetSettings()
    {
        return Ok(JObject.Parse(TrangaSettings.Serialize()));
    }
    
    /// <summary>
    /// Get the current UserAgent used by Tranga
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("UserAgent")]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    public IActionResult GetUserAgent()
    {
        return Ok(TrangaSettings.userAgent);
    }
    
    /// <summary>
    /// Set a new UserAgent
    /// </summary>
    /// <response code="200"></response>
    [HttpPatch("UserAgent")]
    [ProducesResponseType(Status200OK)]
    public IActionResult SetUserAgent([FromBody]string userAgent)
    {
        TrangaSettings.UpdateUserAgent(userAgent);
        return Ok();
    }
    
    /// <summary>
    /// Reset the UserAgent to default
    /// </summary>
    /// <response code="200"></response>
    [HttpDelete("UserAgent")]
    [ProducesResponseType(Status200OK)]
    public IActionResult ResetUserAgent()
    {
        TrangaSettings.UpdateUserAgent(TrangaSettings.DefaultUserAgent);
        return Ok();
    }
    
    /// <summary>
    /// Get all Request-Limits
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("RequestLimits")]
    [ProducesResponseType<Dictionary<RequestType,int>>(Status200OK, "application/json")]
    public IActionResult GetRequestLimits()
    {
        return Ok(TrangaSettings.requestLimits);
    }
    
    /// <summary>
    /// Update all Request-Limits to new values
    /// </summary>
    /// <remarks><h1>NOT IMPLEMENTED</h1></remarks>
    [HttpPatch("RequestLimits")]
    [ProducesResponseType(Status501NotImplemented)]
    public IActionResult SetRequestLimits()
    {
        return StatusCode(501);
    }

    /// <summary>
    /// Updates a Request-Limit value
    /// </summary>
    /// <param name="RequestType">Type of Request</param>
    /// <param name="requestLimit">New limit in Requests/Minute</param>
    /// <response code="200"></response>
    /// <response code="400">Limit needs to be greater than 0</response>
    [HttpPatch("RequestLimits/{RequestType}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    public IActionResult SetRequestLimit(RequestType RequestType, [FromBody]int requestLimit)
    {
        if (requestLimit <= 0)
            return BadRequest();
        TrangaSettings.UpdateRequestLimit(RequestType, requestLimit);
        return Ok();
    }
    
    /// <summary>
    /// Reset Request-Limit
    /// </summary>
    /// <response code="200"></response>
    [HttpDelete("RequestLimits/{RequestType}")]
    [ProducesResponseType<string>(Status200OK)]
    public IActionResult ResetRequestLimits(RequestType RequestType)
    {
        TrangaSettings.UpdateRequestLimit(RequestType, TrangaSettings.DefaultRequestLimits[RequestType]);
        return Ok();
    }
    
    /// <summary>
    /// Reset Request-Limit
    /// </summary>
    /// <response code="200"></response>
    [HttpDelete("RequestLimits")]
    [ProducesResponseType<string>(Status200OK)]
    public IActionResult ResetRequestLimits()
    {
        TrangaSettings.ResetRequestLimits();
        return Ok();
    }
    
    /// <summary>
    /// Returns Level of Image-Compression for Images
    /// </summary>
    /// <response code="200">JPEG compression-level as Integer</response>
    [HttpGet("ImageCompression")]
    [ProducesResponseType<int>(Status200OK, "text/plain")]
    public IActionResult GetImageCompression()
    {
        return Ok(TrangaSettings.compression);
    }
    
    /// <summary>
    /// Set the Image-Compression-Level for Images
    /// </summary>
    /// <param name="level">100 to disable, 0-99 for JPEG compression-Level</param>
    /// <response code="200"></response>
    /// <response code="400">Level outside permitted range</response>
    [HttpPatch("ImageCompression")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    public IActionResult SetImageCompression([FromBody]int level)
    {
        if (level < 1 || level > 100)
            return BadRequest();
        TrangaSettings.UpdateCompressImages(level);
        return Ok();
    }
    
    /// <summary>
    /// Get state of Black/White-Image setting
    /// </summary>
    /// <response code="200">True if enabled</response>
    [HttpGet("BWImages")]
    [ProducesResponseType<bool>(Status200OK, "text/plain")]
    public IActionResult GetBwImagesToggle()
    {
        return Ok(TrangaSettings.bwImages);
    }
    
    /// <summary>
    /// Enable/Disable conversion of Images to Black and White
    /// </summary>
    /// <param name="enabled">true to enable</param>
    /// <response code="200"></response>
    [HttpPatch("BWImages")]
    [ProducesResponseType(Status200OK)]
    public IActionResult SetBwImagesToggle([FromBody]bool enabled)
    {
        TrangaSettings.UpdateBwImages(enabled);
        return Ok();
    }
    
    /// <summary>
    /// Get state of April Fools Mode
    /// </summary>
    /// <remarks>April Fools Mode disables all downloads on April 1st</remarks>
    /// <response code="200">True if enabled</response>
    [HttpGet("AprilFoolsMode")]
    [ProducesResponseType<bool>(Status200OK, "text/plain")]
    public IActionResult GetAprilFoolsMode()
    {
        return Ok(TrangaSettings.aprilFoolsMode);
    }
    
    /// <summary>
    /// Enable/Disable April Fools Mode
    /// </summary>
    /// <remarks>April Fools Mode disables all downloads on April 1st</remarks>
    /// <param name="enabled">true to enable</param>
    /// <response code="200"></response>
    [HttpPatch("AprilFoolsMode")]
    [ProducesResponseType(Status200OK)]
    public IActionResult SetAprilFoolsMode([FromBody]bool enabled)
    {
        TrangaSettings.UpdateAprilFoolsMode(enabled);
        return Ok();
    }
    
    /// <summary>
    /// Gets the Chapter Naming Scheme
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// %M Obj Name
    /// %V Volume
    /// %C Chapter
    /// %T Title
    /// %A Author (first in list)
    /// %I Chapter Internal ID
    /// %i Obj Internal ID
    /// %Y Year (Obj)
    ///
    /// ?_(...) replace _ with a value from above:
    /// Everything inside the braces will only be added if the value of %_ is not null
    /// </remarks>
    /// <response code="200"></response>
    [HttpGet("ChapterNamingScheme")]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    public IActionResult GetCustomNamingScheme()
    {
        return Ok(TrangaSettings.chapterNamingScheme);
    }
    
    /// <summary>
    /// Sets the Chapter Naming Scheme
    /// </summary>
    /// <remarks>
    /// Placeholders:
    /// %M Obj Name
    /// %V Volume
    /// %C Chapter
    /// %T Title
    /// %A Author (first in list)
    /// %I Chapter Internal ID
    /// %i Obj Internal ID
    /// %Y Year (Obj)
    ///
    /// ?_(...) replace _ with a value from above:
    /// Everything inside the braces will only be added if the value of %_ is not null
    /// </remarks>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("ChapterNamingScheme")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult SetCustomNamingScheme([FromBody]string namingScheme)
    {
        try
        {
            Dictionary<Chapter, string> oldPaths = context.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
            TrangaSettings.UpdateChapterNamingScheme(namingScheme);
            MoveFileOrFolderJob[] newJobs = oldPaths
                .Select(kv => new MoveFileOrFolderJob(kv.Value, kv.Key.FullArchiveFilePath)).ToArray();
            context.Jobs.AddRange(newJobs);
            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e);
        }
    }

    /// <summary>
    /// Creates a UpdateCoverJob for all Obj
    /// </summary>
    /// <response code="200">Array of JobIds</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("CleanupCovers")]
    [ProducesResponseType<string[]>(Status200OK)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CleanupCovers()
    {
        try
        {
            Tranga.RemoveStaleFiles(context);
            List<UpdateCoverJob> newJobs = context.Mangas.ToList().Select(m => new UpdateCoverJob(m, 0)).ToList();
            context.Jobs.AddRange(newJobs);
            return Ok(newJobs.Select(j => j.Key));
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e);
        }
    }

    /// <summary>
    /// Sets the FlareSolverr-URL
    /// </summary>
    /// <param name="flareSolverrUrl">URL of FlareSolverr-Instance</param>
    /// <response code="200"></response>
    [HttpPost("FlareSolverr/Url")]
    [ProducesResponseType(Status200OK)]
    public IActionResult SetFlareSolverrUrl([FromBody]string flareSolverrUrl)
    {
        TrangaSettings.UpdateFlareSolverrUrl(flareSolverrUrl);
        return Ok();
    }

    /// <summary>
    /// Resets the FlareSolverr-URL (HttpClient does not use FlareSolverr anymore)
    /// </summary>
    /// <response code="200"></response>
    [HttpDelete("FlareSolverr/Url")]
    [ProducesResponseType(Status200OK)]
    public IActionResult ClearFlareSolverrUrl()
    {
        TrangaSettings.UpdateFlareSolverrUrl(string.Empty);
        return Ok();
    }

    /// <summary>
    /// Test FlareSolverr
    /// </summary>
    /// <response code="200">FlareSolverr is working!</response>
    /// <response code="500">FlareSolverr is not working</response>
    [HttpPost("FlareSolverr/Test")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult TestFlareSolverrReachable()
    {
        const string knownProtectedUrl = "https://prowlarr.servarr.com/v1/ping";
        FlareSolverrDownloadClient client = new();
        RequestResult result = client.MakeRequestInternal(knownProtectedUrl);
        return (int)result.statusCode >= 200 && (int)result.statusCode < 300 ? Ok() : StatusCode(500, result.statusCode); 
    }
}