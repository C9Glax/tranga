using API.Controllers.DTOs;
using API.Schema.ActionsContext;
using API.Schema.ActionsContext.Actions;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
using ActionRecord = API.Controllers.DTOs.ActionRecord;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class ActionsController(ActionsContext context) : Controller
{
    /// <summary>
    /// Returns the available Action Types (<see cref="ActionsEnum"/>)
    /// </summary>
    /// <response code="200">List of action-types</response>
    [HttpGet("Types")]
    [ProducesResponseType<ActionsEnum[]>(Status200OK, "application/json")]
    public Ok<ActionsEnum[]> GetAvailableActions()
    {
        return TypedResults.Ok(Enum.GetValues<ActionsEnum>());
    }

    public sealed record Filter(DateTime? Start, DateTime? End, string? MangaId, string? ChapterId, ActionsEnum? Action);
    /// <summary>
    /// Returns <see cref="Schema.ActionsContext.ActionRecord"/> performed in <see cref="Interval"/>
    /// </summary>
    /// <response code="200">List of performed actions</response>
    /// <response code="500">Database error</response>
    [HttpPost("Filter")]
    [ProducesResponseType<IEnumerable<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<IEnumerable<ActionRecord>>, InternalServerError>> GetActionsInterval([FromBody]Filter filter)
    {
        if (await context.Filter(filter.MangaId, filter.ChapterId)
                .Where(a => filter.Start == null || a.PerformedAt >= filter.Start.Value.ToUniversalTime())
                .Where(a => filter.End == null || a.PerformedAt >= filter.End.Value.ToUniversalTime())
                .Where(a => filter.Action == null || a.Action == filter.Action)
                .ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(actions.Select(a => new ActionRecord(a)));
    }
    
    /// <summary>
    /// Returns <see cref="Schema.ActionsContext.ActionRecord"/> with <paramref name="Type"/> <see cref="ActionsEnum"/>
    /// </summary>
    /// <response code="200">List of performed actions</response>
    /// <response code="500">Database error</response>
    [HttpGet("Type/{Type}")]
    [ProducesResponseType<IEnumerable<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<IEnumerable<ActionRecord>>, InternalServerError>> GetActionsWithType(ActionsEnum Type)
    {
        if (await context.Actions.Where(a => a.Action == Type)
                .ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        return TypedResults.Ok(actions.Select(a => new ActionRecord(a)));
    }
    
    /// <summary>
    /// Returns <see cref="Schema.ActionsContext.ActionRecord"/> related to <see cref="Manga"/>
    /// </summary>
    /// <response code="200">List of performed actions</response>
    /// <response code="500">Database error</response>
    [HttpGet("RelatedTo/Manga/{MangaId}")]
    [ProducesResponseType<IEnumerable<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<IEnumerable<ActionRecord>>, InternalServerError>> GetActionsRelatedToManga(string MangaId)
    {
        if(await context.FilterManga(MangaId).ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(actions.Select(a => new ActionRecord(a)));
    }
    
    /// <summary>
    /// Returns <see cref="Schema.ActionsContext.ActionRecord"/> related to <see cref="Chapter"/>
    /// </summary>
    /// <response code="200">List of performed actions</response>
    /// <response code="500">Database error</response>
    [HttpGet("RelatedTo/Chapter/{ChapterId}")]
    [ProducesResponseType<IEnumerable<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<IEnumerable<ActionRecord>>, InternalServerError>> GetActionsRelatedToChapter(string ChapterId)
    {
        if(await context.FilterChapter(ChapterId).ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(actions.Select(a => new ActionRecord(a)));
    }
}