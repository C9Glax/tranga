using System.Text;
using API.APIEndpointRecords;
using API.Schema.NotificationsContext;
using API.Schema.NotificationsContext.NotificationConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class NotificationConnectorController(IServiceScope scope) : Controller
{
    /// <summary>
    /// Gets all configured <see cref="NotificationConnector"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<NotificationConnector[]>(Status200OK, "application/json")]
    public IActionResult GetAllConnectors()
    {
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        
        return Ok(context.NotificationConnectors.ToArray());
    }
    
    /// <summary>
    /// Returns <see cref="NotificationConnector"/> with requested Name
    /// </summary>
    /// <param name="Name"><see cref="NotificationConnector"/>.Name</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="NotificationConnector"/> with <paramref name="Name"/> not found</response>
    [HttpGet("{Name}")]
    [ProducesResponseType<NotificationConnector>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetConnector(string Name)
    {
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        if(context.NotificationConnectors.Find(Name) is not { } connector)
            return NotFound();
        
        return Ok(connector);
    }
    
    /// <summary>
    /// Creates a new <see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" can be placed in url, header-values and body and will be replaced when notifications are sent</remarks>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateConnector([FromBody]NotificationConnector notificationConnector)
    {
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        
        context.NotificationConnectors.Add(notificationConnector);
        
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);
        return Created();
    }
    
    /// <summary>
    /// Creates a new Gotify-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Priority needs to be between 0 and 10</remarks>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Gotify")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateGotifyConnector([FromBody]GotifyRecord gotifyData)
    {
        //TODO Validate Data
        
        NotificationConnector gotifyConnector = new (gotifyData.Name,
            gotifyData.Endpoint, 
            new Dictionary<string, string>() { { "X-Gotify-IDOnConnector", gotifyData.AppToken } }, 
            "POST", 
            $"{{\"message\": \"%text\", \"title\": \"%title\", \"Priority\": {gotifyData.Priority}}}");
        return CreateConnector(gotifyConnector);
    }
    
    /// <summary>
    /// Creates a new Ntfy-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Priority needs to be between 1 and 5</remarks>
    /// <response code="201"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Ntfy")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateNtfyConnector([FromBody]NtfyRecord ntfyRecord)
    {
        //TODO Validate Data
        
        string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ntfyRecord.Username}:{ntfyRecord.Password}"));
        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader)).Replace("=","");
        
        NotificationConnector ntfyConnector = new (ntfyRecord.Name,
            $"{ntfyRecord.Endpoint}/{ntfyRecord.Topic}?auth={auth}", 
            new Dictionary<string, string>()
            {
                {"Title", "%title"},
                {"Priority", ntfyRecord.Priority.ToString()},
            }, 
            "POST", 
            "%text");
        return CreateConnector(ntfyConnector);
    }
    
    /// <summary>
    /// Creates a new Pushover-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>https://pushover.net/api</remarks>
    /// <response code="201">ID of new connector</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Pushover")]
    [ProducesResponseType<string>(Status201Created, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreatePushoverConnector([FromBody]PushoverRecord pushoverRecord)
    {
        //TODO Validate Data
        
        NotificationConnector pushoverConnector = new  (pushoverRecord.Name,
            $"https://api.pushover.net/1/messages.json", 
            new Dictionary<string, string>(),
            "POST", 
            $"{{\"token\": \"{pushoverRecord.AppToken}\", \"user\": \"{pushoverRecord.User}\", \"message:\":\"%text\", \"%title\" }}");
        return CreateConnector(pushoverConnector);
    }
    
    /// <summary>
    /// Deletes the <see cref="NotificationConnector"/> with the requested Name
    /// </summary>
    /// <param name="Name"><see cref="NotificationConnector"/>.Name</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="NotificationConnector"/> with Name not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{Name}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteConnector(string Name)
    {
        NotificationsContext context = scope.ServiceProvider.GetRequiredService<NotificationsContext>();
        if(context.NotificationConnectors.Find(Name) is not { } connector)
            return NotFound();
        
        context.NotificationConnectors.Remove(connector);
        
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);
        return Created();
    }
}