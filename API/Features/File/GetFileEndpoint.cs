using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.File;

public abstract class GetFileEndpoint
{
    public static async Task<Results<FileStreamHttpResult, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid fileId, CancellationToken ct)
    {
        if (await mangaContext.Files.FirstOrDefaultAsync(f => f.FileId == fileId, ct) is not { } file)
            return TypedResults.NotFound();

        try
        {
            MemoryStream memoryStream = await file.LoadFile(ct);
            return TypedResults.File(memoryStream, file.MimeType, file.Name);
        }
        catch (FileLoadException)
        {
            return TypedResults.InternalServerError();
        }
    }
}