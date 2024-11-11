using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class ConnectorController(PgsqlContext context) : Controller
{
    [HttpGet]
    public IActionResult GetConnectors()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("SearchManga")]
    public IActionResult SearchMangaGlobal(string name)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("{id}/SearchManga")]
    public IActionResult SearchManga(string id, string name)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("{id}/GetManga")]
    public IActionResult GetManga(string id, [FromBody]string mangaId)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
}