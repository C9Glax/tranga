﻿using API.Schema;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}")]
public class MangaConnectorController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all available Connectors (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<MangaConnector[]>(Status200OK, "application/json")]
    public IActionResult GetConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Get all enabled Connectors (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("enabled")]
    [ProducesResponseType<MangaConnector[]>(Status200OK, "application/json")]
    public IActionResult GetEnabledConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.Where(c => c.Enabled == true).ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Get all disabled Connectors (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("disabled")]
    [ProducesResponseType<MangaConnector[]>(Status200OK, "application/json")]
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
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{id}/SetEnabled/{enabled}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
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