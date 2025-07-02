using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class LibraryConnectorController(IServiceScope scope) : Controller
{
    /// <summary>
    /// Gets all configured <see cref="LibraryConnector"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<LibraryConnector[]>(Status200OK, "application/json")]
    public IActionResult GetAllConnectors()
    {
        LibraryContext context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        
        LibraryConnector[] connectors = context.LibraryConnectors.ToArray();
        
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
    public IActionResult GetConnector(string LibraryConnectorId)
    {
        LibraryContext context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        if (context.LibraryConnectors.Find(LibraryConnectorId) is not { } connector)
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
    public IActionResult CreateConnector([FromBody]LibraryConnector libraryConnector)
    {
        LibraryContext context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        
        context.LibraryConnectors.Add(libraryConnector);
        
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);
        return Created();
    }
    
    /// <summary>
    /// Deletes <see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/>
    /// </summary>
    /// <param name="LibraryConnectorId">ToFileLibrary-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="LibraryConnector"/> with <<paramref name="LibraryConnectorId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{LibraryConnectorId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteConnector(string LibraryConnectorId)
    {
        LibraryContext context = scope.ServiceProvider.GetRequiredService<LibraryContext>();
        if (context.LibraryConnectors.Find(LibraryConnectorId) is not { } connector)
            return NotFound();
        
        context.LibraryConnectors.Remove(connector);
        
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);
        return Ok();
    }
}