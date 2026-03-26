using Database.MangaContext;
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
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of the Manga</param>
    /// <param name="download">true to watch the Manga</param>
    /// <param name="ct"></param>
    /// <response code="200">Manga is marked</response>
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, [FromQuery]bool download, CancellationToken ct)
    {
        if (await mangaContext.Mangas.FirstOrDefaultAsync(m => m.Id == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        manga.Monitor = download;

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}