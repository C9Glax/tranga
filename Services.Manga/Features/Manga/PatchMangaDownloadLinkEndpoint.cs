using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PatchMangaDownloadLinkEndpoint
{
    /// <summary>
    /// Set Priority for Download-Link
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="tasksService"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="downloadId">ID of Download-Link</param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Download-Link priority has been changed</response>
    /// <response code="404">Manga or Download-Link with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle( [FromServices]MangaContext mangaContext, [FromServices]Service.MyTasksServiceApiClient tasksService, [FromRoute]Guid mangaId, [FromRoute]Guid downloadId, [FromBody]PatchMangaDownloadLinkRequest req, CancellationToken ct)
    {
        if (await mangaContext.MangaDownloadLinks.FirstOrDefaultAsync(s => s.DownloadLinkId == downloadId && s.MangaId == mangaId, ct) is not { } entry)
            return TypedResults.NotFound();
        
        //TODO Priority
        
        entry.Matched = req.Matched;
        await mangaContext.SaveChangesAsync(ct);

        await tasksService.GetMangaChaptersAsync(mangaId, ct);

        return TypedResults.Ok();
    }

    /// <summary>
    /// Used in <see cref="PatchMangaDownloadLinkEndpoint"/>
    /// </summary>
    /// <param name="Matched">Use Download-Link for Downloads</param>
    /// <param name="Priority">Priority with which to Download Chapters from this Source</param>
    public sealed record PatchMangaDownloadLinkRequest(bool Matched, int Priority);
}