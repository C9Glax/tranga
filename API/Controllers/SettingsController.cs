using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SettingsController(PgsqlContext context) : Controller
{
    [HttpGet]
    public IActionResult GetSettings()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("UserAgent")]
    public IActionResult GetUserAgent()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("UserAgent")]
    public IActionResult SetUserAgent()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpDelete("UserAgent")]
    public IActionResult ResetUserAgent()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("RequestLimits")]
    public IActionResult GetRequestLimits()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("RequestLimits")]
    public IActionResult SetRequestLimits()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpDelete("RequestLimits")]
    public IActionResult ResetRequestLimits()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("ImageCompression")]
    public IActionResult GetImageCompression()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("ImageCompression")]
    public IActionResult SetImageCompression()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("BWImages")]
    public IActionResult GetBWImagesToggle()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("BWImages")]
    public IActionResult SetBWImagesToggle()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("LibraryUpdateBuffer")]
    public IActionResult GetLibraryUpdateBuffer()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("LibraryUpdateBuffer")]
    public IActionResult SetLibraryUpdateBuffer()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("NotificationUpdateBuffer")]
    public IActionResult GetNotificationUpdateBuffer()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("NotificationUpdateBuffer")]
    public IActionResult SetNotificationUpdateBuffer()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("AprilFoolsMode")]
    public IActionResult GetAprilFoolsMode()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("AprilFoolsMode")]
    public IActionResult SetAprilFoolsMode()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
}