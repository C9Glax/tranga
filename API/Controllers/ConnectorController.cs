using API.Schema;
using API.Schema.Jobs;
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
        return StatusCode(500, "Not implemented"); //TODO
    }
    
    /// <summary>
    /// Initiate a search for a Manga on a specific Connector
    /// </summary>
    /// <param name="id">Manga-Connector-ID</param>
    /// <param name="name">Name/Title of the Manga</param>
    /// <returns>Manga</returns>
    [HttpPost("{id}/SearchManga")]
    [ProducesResponseType<Manga[]>(Status200OK)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult SearchManga(string id, [FromBody]string name)
    {
        try
        {
            SearchMangaJob searchMangaJob = new(name, id);
            context.Jobs.Add(searchMangaJob);
            context.SaveChanges();

            JobState? state = null;
            do
            {
                state = context.Jobs.Find(searchMangaJob.JobId)?.state;
                if (state is null)
                    return StatusCode(500, "Missing job state");
            } while (state is not JobState.Completed);

            Manga[] all = context.Manga.Where(m => m.MangaConnectorName == id).ToArray();
            
            return Ok(all.Where(m => NeedlemanWunschStringUtil.CalculateSimilarityPercentage(m.Name, name) >= 07f));
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}