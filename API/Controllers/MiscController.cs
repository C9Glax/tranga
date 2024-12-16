using API.Schema;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}")]
public class MiscController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all available Connectors (Scanlation-Sites)
    /// </summary>
    /// <returns>Array of MangaConnector</returns>
    [HttpGet("GetConnectors")]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.ToArray();
        return Ok(connectors);
    }
}