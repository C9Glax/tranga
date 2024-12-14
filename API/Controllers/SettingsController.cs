using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class SettingsController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all Settings
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetSettings()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Get the current UserAgent used by Tranga
    /// </summary>
    /// <returns>UserAgent as string</returns>
    [HttpGet("UserAgent")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetUserAgent()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Set a new UserAgent
    /// </summary>
    /// <returns>Nothing</returns>
    [HttpPatch("UserAgent")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetUserAgent()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Reset the UserAgent to default
    /// </summary>
    /// <returns>Nothing</returns>
    [HttpDelete("UserAgent")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult ResetUserAgent()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Get all Request-Limits
    /// </summary>
    /// <returns></returns>
    [HttpGet("RequestLimits")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetRequestLimits()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Update all Request-Limits to new values
    /// </summary>
    /// <returns>Nothing</returns>
    [HttpPatch("RequestLimits")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetRequestLimits()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Reset all Request-Limits
    /// </summary>
    /// <returns>Nothing</returns>
    [HttpDelete("RequestLimits")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult ResetRequestLimits()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Returns Level of Image-Compression for Images
    /// </summary>
    /// <returns></returns>
    [HttpGet("ImageCompression")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetImageCompression()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Set the Image-Compression-Level for Images
    /// </summary>
    /// <param name="percentage">100 to disable, 0-99 for JPEG compression-Level</param>
    /// <returns>Nothing</returns>
    [HttpPatch("ImageCompression")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetImageCompression(int percentage)
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Get state of Black/White-Image setting
    /// </summary>
    /// <returns>True if enabled</returns>
    [HttpGet("BWImages")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetBwImagesToggle()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Enable/Disable conversion of Images to Black and White
    /// </summary>
    /// <param name="enabled">true to enable</param>
    /// <returns>Nothing</returns>
    [HttpPatch("BWImages")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetBwImagesToggle(bool enabled)
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Get state of April Fools Mode
    /// </summary>
    /// <remarks>April Fools Mode disables all downloads on April 1st</remarks>
    /// <returns>True if enabled</returns>
    [HttpGet("AprilFoolsMode")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetAprilFoolsMode()
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Enable/Disable April Fools Mode
    /// </summary>
    /// <remarks>April Fools Mode disables all downloads on April 1st</remarks>
    /// <param name="enabled">true to enable</param>
    /// <returns>Nothing</returns>
    [HttpPatch("AprilFoolsMode")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetAprilFoolsMode(bool enabled)
    {
        return StatusCode(500, "Not implemented"); //TODO
    }
}