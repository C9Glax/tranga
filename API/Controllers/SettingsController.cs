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
    [HttpGet]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetSettings()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("UserAgent")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetUserAgent()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("UserAgent")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetUserAgent()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpDelete("UserAgent")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult ResetUserAgent()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("RequestLimits")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetRequestLimits()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("RequestLimits")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetRequestLimits()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpDelete("RequestLimits")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult ResetRequestLimits()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("ImageCompression")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetImageCompression()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("ImageCompression")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetImageCompression()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("BWImages")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetBWImagesToggle()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("BWImages")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetBWImagesToggle()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("LibraryUpdateBuffer")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetLibraryUpdateBuffer()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("LibraryUpdateBuffer")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetLibraryUpdateBuffer()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("NotificationUpdateBuffer")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetNotificationUpdateBuffer()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("NotificationUpdateBuffer")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetNotificationUpdateBuffer()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("AprilFoolsMode")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetAprilFoolsMode()
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpPatch("AprilFoolsMode")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SetAprilFoolsMode()
    {
        return StatusCode(500, "Not implemented");
    }
}