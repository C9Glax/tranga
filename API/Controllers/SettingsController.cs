using System.Text.Json.Nodes;
using API.MangaDownloadClients;
using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SettingsController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all Settings
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<string>(StatusCodes.Status200OK, "application/json")]
    public IActionResult GetSettings()
    {
        return Ok(TrangaSettings.Serialize());
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
    /// Reset all Request-Limits
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
    public IActionResult SetImageCompression(int level)
    {
        if (level < 0 || level > 100)
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
    public IActionResult SetBwImagesToggle(bool enabled)
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
    public IActionResult SetAprilFoolsMode(bool enabled)
    {
        TrangaSettings.UpdateAprilFoolsMode(enabled);
        return Ok();
    }
}