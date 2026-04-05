using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

public abstract class GetMangaCoverEndpoint
{
    public static async Task<Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } source)
            return TypedResults.NotFound();

        if (source.MetadataSource.CoverId is null)
            return TypedResults.NoContent();

        if (await mangaContext.Files.FirstOrDefaultAsync(f => f.FileId == source.MetadataSource.CoverId, cancellationToken: ct) is not { } file)
            return TypedResults.InternalServerError();

        MemoryStream fileStream = await file.LoadFile(ct);
        return TypedResults.Stream(fileStream, file.MimeType, file.Name);
    }
}