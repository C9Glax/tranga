using API.MangaConnectors;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MangaConnectorController(MangaContext context) : Controller
{
    /// <summary>
    /// Get all <see cref="MangaConnector"/> (Scanlation-Sites)
    /// </summary>
    /// <response code="200">Names of <see cref="MangaConnector"/> (Scanlation-Sites)</response>
    [HttpGet]
    [ProducesResponseType<List<MangaConnector>>(Status200OK, "application/json")]
    public Ok<List<MangaConnector>> GetConnectors()
    {
        return TypedResults.Ok(Tranga.MangaConnectors.ToList());
    }

    /// <summary>
    /// Returns the <see cref="MangaConnector"/> (Scanlation-Sites) with the requested Name
    /// </summary>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnector"/> (Scanlation-Sites) with Name not found.</response>
    [HttpGet("{MangaConnectorName}")]
    [ProducesResponseType<MangaConnector>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public Results<Ok<MangaConnector>, NotFound<string>> GetConnector(string MangaConnectorName)
    {
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals(MangaConnectorName, StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return TypedResults.NotFound(nameof(MangaConnectorName));
        
        return TypedResults.Ok(connector);
    }
    
    /// <summary>
    /// Get all enabled <see cref="MangaConnector"/> (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Enabled")]
    [ProducesResponseType<List<MangaConnector>>(Status200OK, "application/json")]
    public Ok<List<MangaConnector>> GetEnabledConnectors()
    {
        return TypedResults.Ok(Tranga.MangaConnectors.Where(c => c.Enabled).ToList());
    }
    
    /// <summary>
    /// Get all disabled <see cref="MangaConnector"/> (Scanlation-Sites)
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Disabled")]
    [ProducesResponseType<List<MangaConnector>>(Status200OK, "application/json")]
    public Ok<List<MangaConnector>> GetDisabledConnectors()
    {
        
        return TypedResults.Ok(Tranga.MangaConnectors.Where(c => c.Enabled == false).ToList());
    }

    /// <summary>
    /// Enabled or disables <see cref="MangaConnector"/> (Scanlation-Sites) with Name
    /// </summary>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <param name="Enabled">Set true to enable, false to disable</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnector"/> (Scanlation-Sites) with Name not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{MangaConnectorName}/SetEnabled/{Enabled}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> SetEnabled(string MangaConnectorName, bool Enabled)
    {
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals(MangaConnectorName, StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return TypedResults.NotFound(nameof(MangaConnectorName));
        
        connector.Enabled = Enabled;
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
}