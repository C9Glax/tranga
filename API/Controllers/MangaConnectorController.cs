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
public class MangaConnectorController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all available Connectors (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("GetConnectors")]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Get all enabled Connectors (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("GetConnectors/enabled")]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetEnabledConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.Where(c => c.Enabled == true).ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Get all disabled Connectors (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("GetConnectors/disabled")]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetDisabledConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.Where(c => c.Enabled == false).ToArray();
        return Ok(connectors);
    }

    /// <summary>
    /// Enabled or disables a Connector
    /// </summary>
    /// <param name="id">ID of the connector</param>
    /// <param name="enabled">Set true to enable</param>
    /// <response code="200"></response>
    /// <response code="404">Connector with ID not found.</response>
    [HttpPatch("{id}/SetEnabled/{enabled}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult SetEnabled(string id, bool enabled)
    {
        try
        {
            MangaConnector? connector = context.MangaConnectors.Find(id);
            if (connector is null)
                return NotFound();
            
            connector.Enabled = enabled;
            context.SaveChanges();

            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}