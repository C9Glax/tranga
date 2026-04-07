using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMangaCoverEndpoint
{
    /// <summary>
    /// Cover of Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <returns>Cover-File</returns>
    /// <response code="200">Cover-File</response>
    /// <response code="204">Cover does not exist</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    public static async Task<Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } source)
            return TypedResults.NotFound();

        if (source.Metadata.CoverId is null)
            return TypedResults.NoContent();

        if (await mangaContext.Files.FirstOrDefaultAsync(f => f.FileId == source.Metadata.CoverId, cancellationToken: ct) is not { } file)
            return TypedResults.InternalServerError();

        MemoryStream fileStream = await file.LoadFile(ct);
        return TypedResults.Stream(fileStream, file.MimeType, file.Name);
    }
}