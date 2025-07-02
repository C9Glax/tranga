using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MangaConnectorController(IServiceScope scope) : Controller
{
    /// <summary>
    /// Get all <see cref="MangaConnector"/> (Scanlation-Sites)
    /// </summary>
    /// <response code="200">Names of <see cref="MangaConnector"/> (Scanlation-Sites)</response>
    [HttpGet]
    [ProducesResponseType<MangaConnector[]>(Status200OK, "application/json")]
    public IActionResult GetConnectors()
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        return Ok(context.MangaConnectors.Select(c => c.Name).ToArray());
    }

    /// <summary>
    /// Returns the <see cref="MangaConnector"/> (Scanlation-Sites) with the requested Name
    /// </summary>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnector"/> (Scanlation-Sites) with Name not found.</response>
    [HttpGet("{MangaConnectorName}")]
    [ProducesResponseType<MangaConnector>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetConnector(string MangaConnectorName)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if(context.MangaConnectors.Find(MangaConnectorName) is not { } connector)
            return NotFound();
        
        return Ok(connector);
    }
    
    /// <summary>
    /// Get all enabled <see cref="MangaConnector"/> (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Enabled")]
    [ProducesResponseType<MangaConnector[]>(Status200OK, "application/json")]
    public IActionResult GetEnabledConnectors()
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        
        return Ok(context.MangaConnectors.Where(c => c.Enabled).ToArray());
    }
    
    /// <summary>
    /// Get all disabled <see cref="MangaConnector"/> (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Disabled")]
    [ProducesResponseType<MangaConnector[]>(Status200OK, "application/json")]
    public IActionResult GetDisabledConnectors()
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        
        return Ok(context.MangaConnectors.Where(c => c.Enabled == false).ToArray());
    }

    /// <summary>
    /// Enabled or disables <see cref="MangaConnector"/> (Scanlation-Sites) with Name
    /// </summary>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <param name="Enabled">Set true to enable, false to disable</param>
    /// <response code="202"></response>
    /// <response code="404"><see cref="MangaConnector"/> (Scanlation-Sites) with Name not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{MangaConnectorName}/SetEnabled/{Enabled}")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult SetEnabled(string MangaConnectorName, bool Enabled)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if(context.MangaConnectors.Find(MangaConnectorName) is not { } connector)
            return NotFound();
        
        connector.Enabled = Enabled;
        
        if(context.Sync().Result is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Accepted();
    }
}