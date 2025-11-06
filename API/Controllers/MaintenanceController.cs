using API.MangaConnectors;
using API.Schema.ActionsContext;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MaintenanceController(MangaContext mangaContext, ActionsContext actionContext) : ControllerBase
{
    
    /// <summary>
    /// Removes all <see cref="Manga"/> not marked for Download on any <see cref="MangaConnector"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("CleanupNoDownloadManga")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, InternalServerError<string>>> CleanupNoDownloadManga()
    {
        if (await mangaContext.Mangas
                .Include(m => m.MangaConnectorIds)
                .Where(m => !m.MangaConnectorIds.Any(id => id.UseForDownload))
                .ToListAsync(HttpContext.RequestAborted) is not { } remove)
            return TypedResults.InternalServerError("Database error");
        
        mangaContext.RemoveRange(remove);
        
        if(await mangaContext.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
    
    
    /// <summary>
    /// Removes all <see cref="ActionRecord"/>
    /// </summary>
    /// <response code="200">Number of deleted records</response>
    [HttpPost("CleanupActions")]
    [ProducesResponseType<int>(Status200OK, "text/plain")]
    public async Task<Ok<int>> CleanupActions()
    {
        int rows = await actionContext.Actions.ExecuteDeleteAsync(HttpContext.RequestAborted);
        return TypedResults.Ok(rows);
    }
    
}