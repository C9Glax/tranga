using API.Schema;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class ConnectorController(PgsqlContext context) : Controller
{
    [HttpGet]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.ToArray();
        return Ok(connectors);
    }
    
    [HttpGet("SearchManga")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SearchMangaGlobal(string name)
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("{id}/SearchManga")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult SearchManga(string id, string name)
    {
        return StatusCode(500, "Not implemented");
    }
    
    [HttpGet("{id}/GetManga")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult GetManga(string id, [FromBody]string mangaId)
    {
        return StatusCode(500, "Not implemented");
    }
}