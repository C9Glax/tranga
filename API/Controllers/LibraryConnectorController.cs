using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class LibraryConnectorController(LibraryContext context, ILog Log) : Controller
{
    /// <summary>
    /// Gets all configured ToFileLibrary-Connectors
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<LibraryConnector[]>(Status200OK, "application/json")]
    public IActionResult GetAllConnectors()
    {
        LibraryConnector[] connectors = context.LibraryConnectors.ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Returns ToFileLibrary-Connector with requested ID
    /// </summary>
    /// <param name="LibraryControllerId">ToFileLibrary-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Connector with ID not found.</response>
    [HttpGet("{LibraryControllerId}")]
    [ProducesResponseType<LibraryConnector>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetConnector(string LibraryControllerId)
    {
        LibraryConnector? ret = context.LibraryConnectors.Find(LibraryControllerId);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }
    
    /// <summary>
    /// Creates a new ToFileLibrary-Connector
    /// </summary>
    /// <param name="libraryConnector">ToFileLibrary-Connector</param>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateConnector([FromBody]LibraryConnector libraryConnector)
    {
        try
        {
            context.LibraryConnectors.Add(libraryConnector);
            context.SaveChanges();
            return Created();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
    
    /// <summary>
    /// Deletes the ToFileLibrary-Connector with the requested ID
    /// </summary>
    /// <param name="LibraryControllerId">ToFileLibrary-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Connector with ID not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{LibraryControllerId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteConnector(string LibraryControllerId)
    {
        try
        {
            LibraryConnector? ret = context.LibraryConnectors.Find(LibraryControllerId);
            if (ret is null)
                return NotFound();
            
            context.Remove(ret);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
}