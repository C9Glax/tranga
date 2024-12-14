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
    /// <returns>Array of configured Notification-Connectors</returns>
    [HttpGet]
    [ProducesResponseType<NotificationConnector[]>(Status200OK)]
    public IActionResult GetAllConnectors()
    {
        NotificationConnector[] ret = context.NotificationConnectors.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns Notification-Connector with requested ID
    /// </summary>
    /// <param name="id">Notification-Connector-ID</param>
    /// <returns>Notification-Connector</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<NotificationConnector>(Status200OK)]
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
    /// <returns>Nothing</returns>
    [HttpPut]
    [ProducesResponseType<NotificationConnector[]>(Status200OK)]
    [ProducesResponseType<string>(Status500InternalServerError)]
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
    /// <returns>Nothing</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult DeleteConnector(string id)
    {
        try
        {
            NotificationConnector? ret = context.NotificationConnectors.Find(id);
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