using API.Schema.ActionsContext;
using API.Schema.ActionsContext.Actions;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;

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

    public sealed record Interval(DateTime Start, DateTime End);
    /// <summary>
    /// Returns <see cref="ActionRecord"/> performed in <see cref="Interval"/>
    /// </summary>
    /// <response code="200">List of performed actions</response>
    /// <response code="500">Database error</response>
    [HttpPost("Interval")]
    [ProducesResponseType<List<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<ActionRecord>>, InternalServerError>> GetActionsInterval([FromBody]Interval interval)
    {
        if (await context.Actions.Where(a => a.PerformedAt >= interval.Start && a.PerformedAt <= interval.End)
                .ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        return TypedResults.Ok(actions);
    }
    
    /// <summary>
    /// Returns <see cref="ActionRecord"/> with <paramref name="Type"/> <see cref="ActionsEnum"/>
    /// </summary>
    /// <response code="200">List of performed actions</response>
    /// <response code="500">Database error</response>
    [HttpGet("Type/{Type}")]
    [ProducesResponseType<List<ActionRecord>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<ActionRecord>>, InternalServerError>> GetActionsWithType(ActionsEnum Type)
    {
        if (await context.Actions.Where(a => a.Action == Type)
                .ToListAsync(HttpContext.RequestAborted) is not { } actions)
            return TypedResults.InternalServerError();
        return TypedResults.Ok(actions);
    }
}