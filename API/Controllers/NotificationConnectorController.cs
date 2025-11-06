using System.Text;
using API.Controllers.DTOs;
using API.Controllers.Requests;
using API.Schema.NotificationsContext;
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
public class NotificationConnectorController(NotificationsContext context) : ControllerBase
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

        List<NotificationConnector> notificationConnectors = result.Select(n => new NotificationConnector(n.Name, n.Url, n.HttpMethod, n.Body, n.Headers)).ToList();

        return TypedResults.Ok(notificationConnectors);
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

        NotificationConnector notificationConnector = new NotificationConnector(connector.Name, connector.Url, connector.HttpMethod, connector.Body, connector.Headers);

        return TypedResults.Ok(notificationConnector);
    }
    
    /// <summary>
    /// Creates a new <see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Formatting placeholders: "%title" and "%text" can be placed in url, header-values and body and will be replaced when notifications are sent</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, InternalServerError<string>>> CreateConnector ([FromBody]CreateNotificationConnectorRecord requestData)
    {
        // TODO validate data
        API.Schema.NotificationsContext.NotificationConnectors.NotificationConnector newConnector =
            new(requestData.Name, requestData.Url, requestData.Headers, requestData.HttpMethod, requestData.Body);
        
        context.NotificationConnectors.Add(newConnector);
        context.Notifications.Add(new ("Added new Notification Connector!", newConnector.Name, NotificationUrgency.High));
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Created(string.Empty, newConnector.Name);
    }
    
    /// <summary>
    /// Creates a new Gotify-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Priority needs to be between 0 and 10</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Gotify")]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, InternalServerError<string>>> CreateGotifyConnector ([FromBody]CreateGotifyConnectorRecord createGotifyConnectorData)
    {
        //TODO Validate Data

        Uri uri = new Uri(createGotifyConnectorData.Url);
        string url = $"{uri.Scheme}://{uri.DnsSafeHost}{uri.AbsolutePath}/message";
        
        CreateNotificationConnectorRecord gotifyConnector = new ()
        {
            Name = createGotifyConnectorData.Name,
            Url = url,
            HttpMethod = "POST",
            Body =
                $"{{\"message\": \"%text\", \"title\": \"%title\", \"Priority\": {createGotifyConnectorData.Priority}}}",
            Headers = new() { { "X-Gotify-Key", createGotifyConnectorData.AppToken } }
        };
        return await CreateConnector(gotifyConnector);
    }
    
    /// <summary>
    /// Creates a new Ntfy-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>Priority needs to be between 1 and 5</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Ntfy")]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, InternalServerError<string>>> CreateNtfyConnector ([FromBody]CreateNtfyConnectorRecord createNtfyConnectorRecord)
    {
        //TODO Validate Data
        string authHeader = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{createNtfyConnectorRecord.Username}:{createNtfyConnectorRecord.Password}"));
        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader)).Replace("=","");
        
        CreateNotificationConnectorRecord ntfyConnector = new ()
        {
            Name = createNtfyConnectorRecord.Name,
            Url = $"{createNtfyConnectorRecord.Url}?auth={auth}",
            HttpMethod = "POST",
            Body = $"{{\"message\": \"%text\", \"title\": \"%title\", \"Priority\": {createNtfyConnectorRecord.Priority} \"Topic\": \"{createNtfyConnectorRecord.Topic}\"}}",
            Headers = new () {{"Authorization", auth}}
        };
        return await CreateConnector(ntfyConnector);
    }
    
    /// <summary>
    /// Creates a new Pushover-<see cref="NotificationConnector"/>
    /// </summary>
    /// <remarks>https://pushover.net/api</remarks>
    /// <response code="200">ID of the new <see cref="NotificationConnector"/></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("Pushover")]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, InternalServerError<string>>> CreatePushoverConnector ([FromBody]CreatePushoverConnectorRecord createPushoverConnectorRecord)
    {
        //TODO Validate Data
        CreateNotificationConnectorRecord pushoverConnector = new ()
        {
            Name = createPushoverConnectorRecord.Name,
            Url = "https://api.pushover.net/1/messages.json",
            HttpMethod = "POST",
            Body = $"{{\"token\": \"{createPushoverConnectorRecord.AppToken}\", \"user\": \"{createPushoverConnectorRecord.Username}\", \"message:\":\"%text\", \"%title\" }}",
            Headers = new ()
        };
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
        if (await context.NotificationConnectors.Where(c => c.Name == Name).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(Name));
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
}