using API.Schema;
using API.Schema.NotificationConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class NotificationConnectorController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Gets all configured Notification-Connectors
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<NotificationConnector[]>(Status200OK, "application/json")]
    public IActionResult GetAllConnectors()
    {
        NotificationConnector[] ret = context.NotificationConnectors.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns Notification-Connector with requested ID
    /// </summary>
    /// <param name="id">Notification-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">NotificationConnector with ID not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType<NotificationConnector>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetConnector(string id)
    {
        NotificationConnector? ret = context.NotificationConnectors.Find(id);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }
    
    /// <summary>
    /// Creates a new Notification-Connector
    /// </summary>
    /// <param name="notificationConnector">Notification-Connector</param>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType<NotificationConnector[]>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateConnector([FromBody]NotificationConnector notificationConnector)
    {
        try
        {
            context.NotificationConnectors.Add(notificationConnector);
            context.SaveChanges();
            return Created();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    /// <summary>
    /// Deletes the Notification-Connector with the requested ID
    /// </summary>
    /// <param name="id">Notification-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">NotificationConnector with ID not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteConnector(string id)
    {
        try
        {
            NotificationConnector? ret = context.NotificationConnectors.Find(id);
            if(ret is null)
                return NotFound();
            
            context.Remove(ret);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}