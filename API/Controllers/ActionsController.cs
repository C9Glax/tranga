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
    /// <param name="filter"></param>
    /// <param name="page">Page to request (default 1)</param>
    /// <param name="pageSize">Size of Page (default 10)</param>
    /// <response code="200">List of performed actions</response>
    /// <response code="400">Page data wrong</response>
    /// <response code="500">Database error</response>
    [HttpPost("Filter")]
    [ProducesResponseType<PagedResponse<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    [ProducesResponseType(Status400BadRequest)]
    public async Task<Results<Ok<PagedResponse<ActionRecord>>, BadRequest, InternalServerError>> GetActionsInterval([FromBody]Filter filter, [FromQuery]int page = 1, [FromQuery]int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
            return TypedResults.BadRequest();
        if (await context.FilterActions(filter.MangaId, filter.ChapterId)
                .Where(a => filter.Start == null || a.PerformedAt >= filter.Start.Value.ToUniversalTime())
                .Where(a => filter.End == null || a.PerformedAt <= filter.End.Value.ToUniversalTime())
                .Where(a => filter.Action == null || a.Action == filter.Action) 
                .OrderByDescending(a => a.PerformedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        int totalResults = await context.FilterActions(filter.MangaId, filter.ChapterId)
            .Where(a => filter.Start == null || a.PerformedAt >= filter.Start.Value.ToUniversalTime())
            .Where(a => filter.End == null || a.PerformedAt <= filter.End.Value.ToUniversalTime())
            .Where(a => filter.Action == null || a.Action == filter.Action)
            .CountAsync(HttpContext.RequestAborted);
        
        return TypedResults.Ok(new PagedResponse<ActionRecord>(actions.Select(a => new ActionRecord(a)), page, totalResults / pageSize, totalResults));
    }
}