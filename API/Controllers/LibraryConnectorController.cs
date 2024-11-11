using API.Schema;
using API.Schema.LibraryConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class LibraryConnectorController(PgsqlContext context) : Controller
{
    [HttpGet]
    public IActionResult GetAllConnectors()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("Types")]
    public IActionResult GetConnectorTypes()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpPost("Create")]
    public IActionResult CreateConnector([FromBody]LibraryConnector libraryConnector)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpDelete("{id}")]
    public IActionResult DeleteConnector(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
}