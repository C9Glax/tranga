using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostMangaRemoveEndpoint
{
    /// <summary>
    /// Stops monitoring a Manga and un-chooses its metadata entry, removing it from the Manga list.
    /// Downloaded chapters and files are kept.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <response code="200">Manga has been removed</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.Mangas.FirstOrDefaultAsync(m => m.MangaId == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        manga.Monitored = false;

        await mangaContext.MangaMetadataEntries.Where(e => e.MangaId == mangaId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Chosen, false), cancellationToken: ct);

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
