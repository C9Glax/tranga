using API.Controllers.Requests;
using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using static Microsoft.AspNetCore.Http.StatusCodes;
using LibraryConnector = API.Controllers.DTOs.LibraryConnector;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class LibraryConnectorController(LibraryContext context) : ControllerBase
{
    /// <summary>
    /// Gets all configured <see cref="DTOs.LibraryConnector"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<List<LibraryConnector>>(Status200OK, "application/json")]
    public async Task<Results<Ok<List<LibraryConnector>>, InternalServerError>> GetAllConnectors ()
    {
        if (await context.LibraryConnectors.ToListAsync(HttpContext.RequestAborted) is not { } connectors)
            return TypedResults.InternalServerError();

        List<LibraryConnector> libraryConnectors = connectors.Select(c => new LibraryConnector(c.Key, c.BaseUrl, c.LibraryType)).ToList();

        return TypedResults.Ok(libraryConnectors);
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
        
        return TypedResults.Ok(new LibraryConnector(connector.Key, connector.BaseUrl, connector.LibraryType));
    }
    
    /// <summary>
    /// Creates a new <see cref="Kavita"/> <see cref="LibraryConnector"/> (<seealso cref="Schema.LibraryContext.LibraryConnectors.LibraryConnector"/>)
    /// </summary>
    /// <param name="requestData"></param>
    /// <response code="201"></response>
    /// <response code="401">Unable to log into account</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Kavita")]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType(Status401Unauthorized)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, UnauthorizedHttpResult, InternalServerError<string>>> CreateKavita ([FromBody]CreateKavitaRecord requestData)
    {
        EntityEntry<Schema.LibraryContext.LibraryConnectors.LibraryConnector> entityEntry
            = await context.LibraryConnectors.AddAsync(new Kavita(requestData.Url, requestData.ApiKey), HttpContext.RequestAborted);

        if (!await entityEntry.Entity.Test(HttpContext.RequestAborted))
            return TypedResults.Unauthorized();

        if(await context.Sync(HttpContext.RequestAborted, GetType(), "Adding Komga Connector") is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Created(string.Empty, entityEntry.Entity.Key);
    }
    
    /// <summary>
    /// Creates a new <see cref="Komga"/> <see cref="LibraryConnector"/> (<seealso cref="Schema.LibraryContext.LibraryConnectors.LibraryConnector"/>)
    /// </summary>
    /// <param name="requestData"></param>
    /// <response code="201"></response>
    /// <response code="401">Unable to log into account</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Komga")]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType(Status401Unauthorized)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, UnauthorizedHttpResult ,InternalServerError<string>>> CreateKomga ([FromBody]CreateKomgaRecord requestData)
    {
        EntityEntry<Schema.LibraryContext.LibraryConnectors.LibraryConnector> entityEntry
            = await context.LibraryConnectors.AddAsync(new Komga(requestData.Url, requestData.ApiKey), HttpContext.RequestAborted);
        
        if (!await entityEntry.Entity.Test(HttpContext.RequestAborted))
            return TypedResults.Unauthorized();

        if(await context.Sync(HttpContext.RequestAborted, GetType(), "Adding Komga Connector") is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Created(string.Empty, entityEntry.Entity.Key);
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
        if (await context.LibraryConnectors.Where(l => l.Key == LibraryConnectorId).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(LibraryConnectorId));
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
}