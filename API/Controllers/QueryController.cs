using API.Controllers.DTOs;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soenneker.Utils.String.NeedlemanWunsch;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Author = API.Controllers.DTOs.Author;
using Chapter = API.Controllers.DTOs.Chapter;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/")]
public class QueryController(MangaContext mangaContext) : ControllerBase
{
    /// <summary>
    /// Returns the <see cref="Author"/> with <paramref name="AuthorId"/>
    /// </summary>
    /// <param name="AuthorId"><see cref="Author"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Author"/> with <paramref name="AuthorId"/> not found</response>
    [HttpGet("Author/{AuthorId}")]
    [ProducesResponseType<Author>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<Author>, NotFound<string>>> GetAuthor (string AuthorId)
    {
        if (await mangaContext.Authors.FirstOrDefaultAsync(a => a.Key == AuthorId, HttpContext.RequestAborted) is not { } author)
            return TypedResults.NotFound(nameof(AuthorId));
        
        return TypedResults.Ok(new Author(author.Key, author.AuthorName));
    }

    /// <summary>
    /// Returns the Server-Stats
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Stats")]
    [ProducesResponseType<Stats>(Status200OK, "application/json")]
    public async Task<Ok<Stats>> GetStats()
    {
        Stats stats = await mangaContext.Database.SqlQueryRaw<Stats>($"""
                                                                   SELECT * FROM
                                                                                (SELECT count("Key") "{nameof(Stats.NumberManga)}" FROM "Mangas") a CROSS JOIN
                                                                                    (SELECT count("Key") "{nameof(Stats.NumberChapters)}" FROM "Chapters") b CROSS JOIN
                                                                                    (SELECT count("Key") "{nameof(Stats.DownloadedChapters)}" FROM "Chapters" WHERE "Downloaded" = true) c CROSS JOIN
                                                                                    (SELECT count("Key") "{nameof(Stats.MissingChapters)}" FROM "Chapters" WHERE "Downloaded" = false) d CROSS JOIN
                                                                                    (SELECT count("Key") "{nameof(Stats.SentNotifications)}" FROM "Notifications" WHERE "IsSent" = true) e CROSS JOIN 
                                                                                    (SELECT count("Key") "{nameof(Stats.ActionsTaken)}" FROM "Actions") f CROSS JOIN 
                                                                                    (SELECT count("Key") "{nameof(Stats.NumberAuthors)}" FROM "Authors") g CROSS JOIN
                                                                                    (SELECT count("Tag") "{nameof(Stats.NumberTags)}" FROM "Tags") h
                                                                                    
                                                                   """).FirstAsync(HttpContext.RequestAborted);
        return TypedResults.Ok(stats);
    }
}