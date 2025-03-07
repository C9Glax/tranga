using API.Schema;
using API.Schema.LibraryConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class LibraryConnectorController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Gets all configured Library-Connectors
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<LibraryConnector[]>(Status200OK)]
    public IActionResult GetAllConnectors()
    {
        LibraryConnector[] connectors = context.LibraryConnectors.ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Returns Library-Connector with requested ID
    /// </summary>
    /// <param name="id">Library-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Connector with ID not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<LibraryConnector>(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetConnector(string id)
    {
        LibraryConnector? ret = context.LibraryConnectors.Find(id);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }
    
    /// <summary>
    /// Creates a new Library-Connector
    /// </summary>
    /// <param name="libraryConnector">Library-Connector</param>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status500InternalServerError)]
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
            return StatusCode(500, e.Message);
        }
    }
    
    /// <summary>
    /// Deletes the Library-Connector with the requested ID
    /// </summary>
    /// <param name="id">Library-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Connector with ID not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult DeleteConnector(string id)
    {
        try
        {
            LibraryConnector? ret = context.LibraryConnectors.Find(id);
            switch (ret is not null)
            {
                case true:
                    context.Remove(ret);
                    context.SaveChanges();
                    return Ok();
                case false: return NotFound();
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}