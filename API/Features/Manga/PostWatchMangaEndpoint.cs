using Database.DownloadContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// Mark a Manga to be (un-)watched for downloads 
/// </summary>
public abstract class PostWatchMangaEndpoint
{
    /// <summary>
    /// Mark a Manga to be (un-)watched for downloads 
    /// </summary>
    /// <param name="downloadContext"></param>
    /// <param name="mangaId">Id of the Manga</param>
    /// <param name="download">true to watch the Manga</param>
    /// <param name="ct"></param>
    /// <response code="200">Manga is marked</response>
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok, NotFound>> Handle(DownloadContext downloadContext, [FromRoute]Guid mangaId, [FromQuery]bool download, CancellationToken ct)
    {
        if (await downloadContext.Mangas.FirstOrDefaultAsync(m => m.MangaId == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        manga.Download = download;

        await downloadContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}