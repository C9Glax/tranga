using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
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
    [ProducesResponseType<List<LibraryConnector>>(Status200OK, "application/json")]
    public async Task<Results<Ok<List<LibraryConnector>>, InternalServerError>> GetAllConnectors ()
    {
        if (await context.LibraryConnectors.ToListAsync(HttpContext.RequestAborted) is not { } connectors)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(connectors);
    }
    
    /// <summary>
    /// Returns <see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/>
    /// </summary>
    /// <param name="LibraryConnectorId"><see cref="LibraryConnector"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="LibraryConnector"/> with <paramref name="LibraryConnectorId"/> not found.</response>
    [HttpGet("{LibraryConnectorId}")]
    [ProducesResponseType<LibraryConnector>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<LibraryConnector>, NotFound<string>>> GetConnector (string LibraryConnectorId)
    {
        if (await context.LibraryConnectors.FirstOrDefaultAsync(l => l.Key == LibraryConnectorId) is not { } connector)
            return TypedResults.NotFound(nameof(LibraryConnectorId));
        
        return TypedResults.Ok(connector);
    }
    
    /// <summary>
    /// Creates a new <see cref="LibraryConnector"/>
    /// </summary>
    /// <param name="libraryConnector"></param>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, InternalServerError<string>>> CreateConnector ([FromBody]LibraryConnector libraryConnector)
    {
        context.LibraryConnectors.Add(libraryConnector);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Created(string.Empty, libraryConnector.Key);
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
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> DeleteConnector (string LibraryConnectorId)
    {
        if (await context.LibraryConnectors.FirstOrDefaultAsync(l => l.Key == LibraryConnectorId) is not { } connector)
            return TypedResults.NotFound(nameof(LibraryConnectorId));
        
        context.LibraryConnectors.Remove(connector);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
}