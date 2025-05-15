using System.Text;
using API.APIEndpointRecords;
using API.Schema.Contexts;
using API.Schema.NotificationConnectors;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class NotificationConnectorController(NotificationsContext context, ILog Log) : Controller
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
    /// <param name="NotificationConnectorId">Notification-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">NotificationConnector with ID not found</response>
    [HttpGet("{NotificationConnectorId}")]
    [ProducesResponseType<NotificationConnector>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetConnector(string NotificationConnectorId)
    {
        NotificationConnector? ret = context.NotificationConnectors.Find(NotificationConnectorId);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }
    
    /// <summary>
    /// Creates a new REST-Notification-Connector
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" can be placed in url, header-values and body and will be replaced when notifications are sent</remarks>
    /// <param name="notificationConnector">Notification-Connector</param>
    /// <response code="201">ID of new connector</response>
    /// <response code="409">A NotificationConnector with name already exists</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateConnector([FromBody]NotificationConnector notificationConnector)
    {
        if (context.NotificationConnectors.Find(notificationConnector.Name) is not null)
            return Conflict();
        try
        {
            context.NotificationConnectors.Add(notificationConnector);
            context.SaveChanges();
            return Created(notificationConnector.Name, notificationConnector);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
    
    /// <summary>
    /// Creates a new Gotify-Notification-Connector
    /// </summary>
    /// <remarks>Priority needs to be between 0 and 10</remarks>
    /// <response code="201">ID of new connector</response>
    /// <response code="400"></response>
    /// <response code="409">A NotificationConnector with name already exists</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Gotify")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateGotifyConnector([FromBody]GotifyRecord gotifyData)
    {
        if(!gotifyData.Validate())
            return BadRequest();
        
        NotificationConnector gotifyConnector = new NotificationConnector(TokenGen.CreateToken("Gotify"),
            gotifyData.endpoint, 
            new Dictionary<string, string>() { { "X-Gotify-Key", gotifyData.appToken } }, 
            "POST", 
            $"{{\"message\": \"%text\", \"title\": \"%title\", \"priority\": {gotifyData.priority}}}");
        return CreateConnector(gotifyConnector);
    }
    
    /// <summary>
    /// Creates a new Ntfy-Notification-Connector
    /// </summary>
    /// <remarks>Priority needs to be between 1 and 5</remarks>
    /// <response code="201">ID of new connector</response>
    /// <response code="400"></response>
    /// <response code="409">A NotificationConnector with name already exists</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Ntfy")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateNtfyConnector([FromBody]NtfyRecord ntfyRecord)
    {
        if(!ntfyRecord.Validate())
            return BadRequest();
        
        string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ntfyRecord.username}:{ntfyRecord.password}"));
        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader)).Replace("=","");
        
        NotificationConnector ntfyConnector = new (TokenGen.CreateToken("Ntfy"),
            $"{ntfyRecord.endpoint}?auth={auth}", 
            new Dictionary<string, string>()
            {
                {"Title", "%title"},
                {"Priority", ntfyRecord.priority.ToString()},
            }, 
            "POST", 
            "%text");
        return CreateConnector(ntfyConnector);
    }
    
    /// <summary>
    /// Creates a new Lunasea-Notification-Connector
    /// </summary>
    /// <remarks>https://docs.lunasea.app/lunasea/notifications/custom-notifications for id. Either device/:device_id or user/:user_id</remarks>
    /// <response code="201">ID of new connector</response>
    /// <response code="400"></response>
    /// <response code="409">A NotificationConnector with name already exists</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Lunasea")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateLunaseaConnector([FromBody]LunaseaRecord lunaseaRecord)
    {
        if(!lunaseaRecord.Validate())
            return BadRequest();
        
        NotificationConnector lunaseaConnector = new (TokenGen.CreateToken("Lunasea"),
            $"https://notify.lunasea.app/v1/custom/{lunaseaRecord.id}", 
            new Dictionary<string, string>(),
            "POST", 
            "{\"title\": \"%title\", \"body\": \"%text\"}");
        return CreateConnector(lunaseaConnector);
    }
    
    /// <summary>
    /// Creates a new Pushover-Notification-Connector
    /// </summary>
    /// <remarks>https://pushover.net/api</remarks>
    /// <response code="201">ID of new connector</response>
    /// <response code="400"></response>
    /// <response code="409">A NotificationConnector with name already exists</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Pushover")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreatePushoverConnector([FromBody]PushoverRecord pushoverRecord)
    {
        if(!pushoverRecord.Validate())
            return BadRequest();
        
        NotificationConnector pushoverConnector = new  (TokenGen.CreateToken("Pushover"),
            $"https://api.pushover.net/1/messages.json", 
            new Dictionary<string, string>(),
            "POST", 
            $"{{\"token\": \"{pushoverRecord.apptoken}\", \"user\": \"{pushoverRecord.user}\", \"message:\":\"%text\", \"%title\" }}");
        return CreateConnector(pushoverConnector);
    }
    
    /// <summary>
    /// Deletes the Notification-Connector with the requested ID
    /// </summary>
    /// <param name="NotificationConnectorId">Notification-Connector-ID</param>
    /// <response code="200"></response>
    /// <response code="404">NotificationConnector with ID not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{NotificationConnectorId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteConnector(string NotificationConnectorId)
    {
        try
        {
            NotificationConnector? ret = context.NotificationConnectors.Find(NotificationConnectorId);
            if(ret is null)
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