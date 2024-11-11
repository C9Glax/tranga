using API.Schema;
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
        
    }
    
    [HttpGet("Types")]
    public IActionResult GetConnectorTypes()
    {
        
    }
    
    [HttpPost("Create")]
    public IActionResult CreateConnector([FromBody] details)
    {
        
    }
    
    [HttpDelete("{id}")]
    public IActionResult DeleteConnector(string id)
    {
        
    }
    
}