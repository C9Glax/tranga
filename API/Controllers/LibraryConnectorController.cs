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
    [HttpGet]
    [ProducesResponseType<LibraryConnector[]>(Status200OK)]
    public IActionResult GetAllConnectors()
    {
        LibraryConnector[] connectors = context.LibraryConnectors.ToArray();
        return Ok(connectors);
    }
    
    [HttpGet("Types")]
    [ProducesResponseType<string[]>(Status200OK)]
    public IActionResult GetConnectorTypes()
    {
        string[] ret = Enum.GetNames<LibraryType>();
        return Ok(ret);
    }
    
    [HttpPost("Create")]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult CreateConnector([FromBody]LibraryConnector libraryConnector)
    {
        return StatusCode(500, "Not implemented");
    }
    
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