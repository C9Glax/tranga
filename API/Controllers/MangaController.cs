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
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpGet("{id}")]
    public IActionResult GetManga(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteManga(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpGet("{id}/Cover")]
    public IActionResult GetCover(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpGet("{id}/Chapters")]
    public IActionResult GetChapters(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("{id}/Chapter/Latest")]
    public IActionResult GetLatestChapter(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPatch("{id}/IgnoreChaptersBefore")]
    public IActionResult IgnoreChaptersBefore(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPost("{id}/MoveFolder")]
    public IActionResult MoveFolder(string id, [FromBody]string folder)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
}