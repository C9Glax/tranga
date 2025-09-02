using System.Text;
using API.APIEndpointRecords;
using API.Schema.NotificationsContext;
using API.Schema.NotificationsContext.NotificationConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class NotificationConnectorController(NotificationsContext context) : Controller
{
    /// <summary>
    /// Gets all configured <see cref="NotificationConnector"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<List<NotificationConnector>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<NotificationConnector>>, InternalServerError>> GetAllConnectors ()
    {
        if(await context.NotificationConnectors.ToListAsync(HttpContext.RequestAborted) is not { } result)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(result);
    }
    
    /// <summary>
    /// Returns <see cref="NotificationConnector"/> with requested Name
    /// </summary>
    /// <param name="Name"><see cref="NotificationConnector"/>.Name</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="NotificationConnector"/> with <paramref name="Name"/> not found</response>
    [HttpGet("{Name}")]
    [ProducesResponseType<NotificationConnector>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<NotificationConnector>, NotFound<string>>> GetConnector (string Name)
    {
        if (await context.NotificationConnectors.FirstOrDefaultAsync(c => c.Name == Name, HttpContext.RequestAborted) is not { } connector)
            return TypedResults.NotFound(nameof(Name));
        
        return TypedResults.Ok(connector);
    }
    
    /// <summary>
    /// Creates a new <see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" can be placed in url, header-values and body and will be replaced when notifications are sent</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok<string>, InternalServerError<string>>> CreateConnector ([FromBody]NotificationConnector notificationConnector)
    {
        context.NotificationConnectors.Add(notificationConnector);
        context.Notifications.Add(new ("Added new Notification Connector!", notificationConnector.Name, NotificationUrgency.High));
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok(notificationConnector.Name);
    }
    
    /// <summary>
    /// Creates a new Gotify-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Priority needs to be between 0 and 10</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Gotify")]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok<string>, InternalServerError<string>>> CreateGotifyConnector ([FromBody]GotifyRecord gotifyData)
    {
        //TODO Validate Data
        
        NotificationConnector gotifyConnector = new (gotifyData.Name,
            gotifyData.Endpoint, 
            new Dictionary<string, string>() { { "X-Gotify-Key", gotifyData.AppToken } }, 
            "POST", 
            $"{{\"message\": \"%text\", \"title\": \"%title\", \"Priority\": {gotifyData.Priority}}}");
        return await CreateConnector(gotifyConnector);
    }
    
    /// <summary>
    /// Creates a new Ntfy-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Priority needs to be between 1 and 5</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Ntfy")]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok<string>, InternalServerError<string>>> CreateNtfyConnector ([FromBody]NtfyRecord ntfyRecord)
    {
        //TODO Validate Data
        
        string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ntfyRecord.Username}:{ntfyRecord.Password}"));
        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader)).Replace("=","");
        
        NotificationConnector ntfyConnector = new (ntfyRecord.Name,
            $"{ntfyRecord.Endpoint}?auth={auth}", 
            new Dictionary<string, string>()
            {
                {"Authorization", auth}
            }, 
            "POST", 
            $"{{\"message\": \"%text\", \"title\": \"%title\", \"Priority\": {ntfyRecord.Priority} \"Topic\": \"{ntfyRecord.Topic}\"}}");
        return await CreateConnector(ntfyConnector);
    }
    
    /// <summary>
    /// Creates a new Pushover-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>https://pushover.net/api</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Pushover")]
    [ProducesResponseType<string>(Status200OK, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok<string>, InternalServerError<string>>> CreatePushoverConnector ([FromBody]PushoverRecord pushoverRecord)
    {
        //TODO Validate Data
        
        NotificationConnector pushoverConnector = new  (pushoverRecord.Name,
            $"https://api.pushover.net/1/messages.json", 
            new Dictionary<string, string>(),
            "POST", 
            $"{{\"token\": \"{pushoverRecord.AppToken}\", \"user\": \"{pushoverRecord.User}\", \"message:\":\"%text\", \"%title\" }}");
        return await CreateConnector(pushoverConnector);
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
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> DeleteConnector (string Name)
    {
        if (await context.NotificationConnectors.FirstOrDefaultAsync(c => c.Name == Name, HttpContext.RequestAborted) is not { } connector)
            return TypedResults.NotFound(nameof(Name));
        
        context.NotificationConnectors.Remove(connector);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
}