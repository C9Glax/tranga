using API.Schema;
using API.Schema.NotificationConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class NotificationConnectorController(PgsqlContext context) : Controller
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
    public IActionResult CreateConnector([FromBody]NotificationConnector notificationConnector)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpDelete("{id}")]
    public IActionResult DeleteConnector(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
}