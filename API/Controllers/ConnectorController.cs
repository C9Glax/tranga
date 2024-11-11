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
        
    }
    
    [HttpGet("SearchManga")]
    public IActionResult SearchMangaGlobal(string name)
    {
        
    }
    
    [HttpGet("{id}/SearchManga")]
    public IActionResult SearchManga(string id, string name)
    {
        
    }
    
    [HttpGet("{id}/GetManga")]
    public IActionResult GetManga(string id, [FromBody]string mangaId)
    {
        
    }
}