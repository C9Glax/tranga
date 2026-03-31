using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// Marks a Manga as Monitored
/// </summary>
public abstract class PatchMangaMonitoredEndpoint
{
    /// <summary>
    /// Marks a Manga as Monitored
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of the Manga</param>
    /// <param name="monitored"></param>
    /// <param name="ct"></param>
    /// <response code="200">Monitored changed</response>
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, [FromQuery]bool monitored, CancellationToken ct)
    {
        
        if(await mangaContext.Mangas.GetManga(mangaId)
               .ExecuteUpdateAsync(s =>
                   s.SetProperty(m => m.Monitor, monitored), ct) != 1)
            return TypedResults.NotFound();

        return TypedResults.Ok();
    }
}