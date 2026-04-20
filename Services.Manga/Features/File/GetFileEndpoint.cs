using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

namespace Services.Manga.Features.File;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetFileEndpoint
{
    /// <summary>
    /// Get File
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="fileId">ID of File</param>
    /// <param name="ct"></param>
    /// <returns>File</returns>
    /// <response code="200">File</response>
    /// <response code="404">File with ID does not exist</response>
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