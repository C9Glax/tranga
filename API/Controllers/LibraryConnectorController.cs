using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class LibraryConnectorController(LibraryContext context) : Controller
{
    /// <summary>
    /// Gets all configured <see cref="LibraryConnector"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<LibraryConnector[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetAllConnectors ()
    {
        if (await context.LibraryConnectors.ToArrayAsync(HttpContext.RequestAborted) is not { } connectors)
            return StatusCode(Status500InternalServerError);
        
        return Ok(connectors);
    }
    
    /// <summary>
    /// Returns <see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/>
    /// </summary>
    /// <param name="LibraryConnectorId"><see cref="LibraryConnector"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/> not found.</response>
    [HttpGet("{LibraryConnectorId}")]
    [ProducesResponseType<LibraryConnector>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetConnector (string LibraryConnectorId)
    {
        if (await context.LibraryConnectors.FirstOrDefaultAsync(l => l.Key == LibraryConnectorId) is not { } connector)
            return NotFound();
        
        return Ok(connector);
    }
    
    /// <summary>
    /// Creates a new <see cref="LibraryConnector"/>
    /// </summary>
    /// <param name="libraryConnector"></param>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> CreateConnector ([FromBody]LibraryConnector libraryConnector)
    {
        
        context.LibraryConnectors.Add(libraryConnector);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Created();
    }
    
    /// <summary>
    /// Deletes <see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/>
    /// </summary>
    /// <param name="LibraryConnectorId">ToFileLibrary-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{LibraryConnectorId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> DeleteConnector (string LibraryConnectorId)
    {
        if (await context.LibraryConnectors.FirstOrDefaultAsync(l => l.Key == LibraryConnectorId) is not { } connector)
            return NotFound();
        
        context.LibraryConnectors.Remove(connector);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Ok();
    }
}