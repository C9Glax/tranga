using API.Controllers.Requests;
using API.MangaDownloadClients;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SettingsController() : ControllerBase
{
    /// <summary>
    /// Get all <see cref="Tranga.Settings"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<TrangaSettings>(Status200OK, "application/json")]
    public Ok<TrangaSettings> GetSettings()
    {
        return TypedResults.Ok(Tranga.Settings);
    }
    
    /// <summary>
    /// Get the current UserAgent used by Tranga
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("UserAgent")]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    public Ok<string> GetUserAgent()
    {
        return TypedResults.Ok(Tranga.Settings.UserAgent);
    }
    
    /// <summary>
    /// Set a new UserAgent
    /// </summary>
    /// <response code="200"></response>
    [HttpPatch("UserAgent")]
    [ProducesResponseType(Status200OK)]
    public Ok SetUserAgent([FromBody]string userAgent)
    {
        //TODO Validate
        Tranga.Settings.SetUserAgent(userAgent);
        return TypedResults.Ok();
    }
    
    /// <summary>
    /// Reset the UserAgent to default
    /// </summary>
    /// <response code="200"></response>
    [HttpDelete("UserAgent")]
    [ProducesResponseType(Status200OK)]
    public Ok ResetUserAgent()
    {
        Tranga.Settings.SetUserAgent(TrangaSettings.DefaultUserAgent);
        return TypedResults.Ok();
    }
    
    /// <summary>
    /// Returns Level of Image-Compression for Images
    /// </summary>
    /// <response code="200">JPEG ImageCompression-level as Integer</response>
    [HttpGet("ImageCompressionLevel")]
    [ProducesResponseType<int>(Status200OK, "text/plain")]
    public Ok<int> GetImageCompression()
    {
        return TypedResults.Ok(Tranga.Settings.ImageCompression);
    }
    
    /// <summary>
    /// Set the Image-Compression-Level for Images
    /// </summary>
    /// <param name="level">100 to disable, 0-99 for JPEG ImageCompression-Level</param>
    /// <response code="200"></response>
    /// <response code="400">Level outside permitted range</response>
    [HttpPatch("ImageCompressionLevel/{level}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    public Results<Ok, BadRequest> SetImageCompression(int level)
    {
        if (level < 1 || level > 100)
            return TypedResults.BadRequest();
        Tranga.Settings.UpdateImageCompression(level);
        return TypedResults.Ok();
    }
    
    /// <summary>
    /// Get state of Black/White-Image setting
    /// </summary>
    /// <response code="200">True if enabled</response>
    [HttpGet("BWImages")]
    [ProducesResponseType<bool>(Status200OK, "text/plain")]
    public Ok<bool> GetBwImagesToggle()
    {
        return TypedResults.Ok(Tranga.Settings.BlackWhiteImages);
    }
    
    /// <summary>
    /// Enable/Disable conversion of Images to Black and White
    /// </summary>
    /// <param name="enabled">true to enable</param>
    /// <response code="200"></response>
    [HttpPatch("BWImages/{enabled}")]
    [ProducesResponseType(Status200OK)]
    public Ok SetBwImagesToggle(bool enabled)
    {
        Tranga.Settings.SetBlackWhiteImageEnabled(enabled);
        return TypedResults.Ok();
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
    public Ok<string> GetCustomNamingScheme()
    {
        return TypedResults.Ok(Tranga.Settings.ChapterNamingScheme);
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
    /// %Y Year (Obj)
    ///
    /// ?_(...) replace _ with a value from above:
    /// Everything inside the braces will only be added if the value of %_ is not null
    /// </remarks>
    /// <response code="200"></response>
    [HttpPatch("ChapterNamingScheme")]
    [ProducesResponseType(Status200OK)]
    public Ok SetCustomNamingScheme([FromBody]string namingScheme)
    {
        //TODO Move old Chapters
        Tranga.Settings.SetChapterNamingScheme(namingScheme);
        
        return TypedResults.Ok();
    }

    /// <summary>
    /// Sets the FlareSolverr-URL
    /// </summary>
    /// <param name="flareSolverrUrl">URL of FlareSolverr-Instance</param>
    /// <response code="200"></response>
    [HttpPatch("FlareSolverr/Url")]
    [ProducesResponseType(Status200OK)]
    public Ok SetFlareSolverrUrl([FromBody]string flareSolverrUrl)
    {
        Tranga.Settings.SetFlareSolverrUrl(flareSolverrUrl);
        return TypedResults.Ok();
    }

    /// <summary>
    /// Resets the FlareSolverr-URL (HttpClient does not use FlareSolverr anymore)
    /// </summary>
    /// <response code="200"></response>
    [HttpDelete("FlareSolverr/Url")]
    [ProducesResponseType(Status200OK)]
    public Ok ClearFlareSolverrUrl()
    {
        Tranga.Settings.SetFlareSolverrUrl(string.Empty);
        return TypedResults.Ok();
    }

    /// <summary>
    /// Test FlareSolverr
    /// </summary>
    /// <response code="200">FlareSolverr is working!</response>
    /// <response code="500">FlareSolverr is not working</response>
    [HttpPost("FlareSolverr/Test")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok, InternalServerError>> TestFlareSolverrReachable()
    {
        const string knownProtectedUrl = "https://prowlarr.servarr.com/v1/ping";
        FlareSolverrDownloadClient client = new(new ());
        HttpResponseMessage result = await client.MakeRequest(knownProtectedUrl, RequestType.Default);
        return result.IsSuccessStatusCode ? TypedResults.Ok() : TypedResults.InternalServerError(); 
    }

    /// <summary>
    /// Returns the language in which Manga are downloaded
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("DownloadLanguage")]
    [ProducesResponseType<string>(Status200OK,  "text/plain")]
    public Ok<string> GetDownloadLanguage()
    {
        return TypedResults.Ok(Tranga.Settings.DownloadLanguage);
    }

    /// <summary>
    /// Sets the language in which Manga are downloaded
    /// </summary>
    /// <response code="200"></response>
    [HttpPatch("DownloadLanguage/{Language}")]
    [ProducesResponseType(Status200OK)]
    public Ok SetDownloadLanguage(string Language)
    {
        //TODO Validation
        Tranga.Settings.SetDownloadLanguage(Language);
        return TypedResults.Ok();
    }
    

    /// <summary>
    /// Sets the time when Libraries are refreshed
    /// </summary>
    /// <response code="200"></response>
    [HttpPatch("LibraryRefresh")]
    [ProducesResponseType(Status200OK)]
    public Ok SetLibraryRefresh([FromBody]PatchLibraryRefreshRecord requestData)
    {
        Tranga.Settings.SetLibraryRefreshSetting(requestData.Setting);
        if(requestData.RefreshLibraryWhileDownloadingEveryMinutes is { } value)
            Tranga.Settings.SetRefreshLibraryWhileDownloadingEveryMinutes(value);
        return TypedResults.Ok();
    }
}