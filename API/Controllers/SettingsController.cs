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
        
    }
    
    [HttpGet("UserAgent")]
    public IActionResult GetUserAgent()
    {
        
    }
    
    [HttpPatch("UserAgent")]
    public IActionResult SetUserAgent()
    {
        
    }
    
    [HttpDelete("UserAgent")]
    public IActionResult ResetUserAgent()
    {
        
    }
    
    [HttpGet("RequestLimits")]
    public IActionResult GetRequestLimits()
    {
        
    }
    
    [HttpPatch("RequestLimits")]
    public IActionResult SetRequestLimits()
    {
        
    }
    
    [HttpDelete("RequestLimits")]
    public IActionResult ResetRequestLimits()
    {
        
    }
    
    [HttpGet("ImageCompression")]
    public IActionResult GetImageCompression()
    {
        
    }
    
    [HttpPatch("ImageCompression")]
    public IActionResult SetImageCompression()
    {
        
    }
    
    [HttpGet("BWImages")]
    public IActionResult GetBWImagesToggle()
    {
        
    }
    
    [HttpPatch("BWImages")]
    public IActionResult SetBWImagesToggle()
    {
        
    }
    
    [HttpGet("LibraryUpdateBuffer")]
    public IActionResult GetLibraryUpdateBuffer()
    {
        
    }
    
    [HttpPatch("LibraryUpdateBuffer")]
    public IActionResult SetLibraryUpdateBuffer()
    {
        
    }
    
    [HttpGet("NotificationUpdateBuffer")]
    public IActionResult GetNotificationUpdateBuffer()
    {
        
    }
    
    [HttpPatch("NotificationUpdateBuffer")]
    public IActionResult SetNotificationUpdateBuffer()
    {
        
    }
    
    [HttpGet("AprilFoolsMode")]
    public IActionResult GetAprilFoolsMode()
    {
        
    }
    
    [HttpPatch("AprilFoolsMode")]
    public IActionResult SetAprilFoolsMode()
    {
        
    }
}