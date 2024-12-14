using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Soenneker.Utils.String.NeedlemanWunsch;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class ConnectorController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all available Connectors (Scanlation-Sites)
    /// </summary>
    /// <returns>Array of MangaConnector</returns>
    [HttpGet]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Initiate a search for a Manga on all Connectors
    /// </summary>
    /// <param name="name">Name/Title of the Manga</param>
    /// <returns>Array of Manga</returns>
    [HttpPost("SearchManga")]
    [ProducesResponseType<Manga[]>(Status500InternalServerError)]
    public IActionResult SearchMangaGlobal(string name)
    {
        List<Manga> allManga = new List<Manga>();
        foreach (MangaConnector contextMangaConnector in context.MangaConnectors)
        {
            allManga.AddRange(contextMangaConnector.GetManga(name));
        }
        return Ok(allManga.ToArray());
    }
    
    /// <summary>
    /// Initiate a search for a Manga on a specific Connector
    /// </summary>
    /// <param name="id">Manga-Connector-ID</param>
    /// <param name="name">Name/Title of the Manga</param>
    /// <returns>Manga</returns>
    [HttpPost("{id}/SearchManga")]
    [ProducesResponseType<Manga[]>(Status200OK)]
    [ProducesResponseType<ProblemResponse>(Status404NotFound)]
    public IActionResult SearchManga(string id, [FromBody]string name)
    {
        MangaConnector? connector = context.MangaConnectors.Find(id);
        if (connector is null)
            return NotFound(new ProblemResponse("Connector not found."));
        Manga[] manga = connector.GetManga(name);
        return Ok(manga);
    }
}