using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MangaController(PgsqlContext context) : Controller
{
    [HttpPost]
    public IActionResult GetManga([FromBody]string[] ids)
    {
        
    }

    [HttpGet("{id}")]
    public IActionResult GetManga(string id)
    {
        
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteManga(string id)
    {
        
    }

    [HttpGet("{id}/Cover")]
    public IActionResult GetCover(string id)
    {
        
    }

    [HttpGet("{id}/Chapters")]
    public IActionResult GetChapters(string id)
    {
        
    }
    
    [HttpGet("{id}/Chapter/Latest")]
    public IActionResult GetLatestChapter(string id)
    {
        
    }
    
    [HttpPatch("{id}/IgnoreChaptersBefore")]
    public IActionResult IgnoreChaptersBefore(string id)
    {
        
    }
    
    [HttpPost("{id}/MoveFolder")]
    public IActionResult MoveFolder(string id, [FromBody]string folder)
    {
        
    }
}