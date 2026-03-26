using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.File;

/// <summary>
/// Get a File
/// </summary>
public abstract class GetFileEndpoint
{
    /// <summary>
    /// Get a File
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="fileId">ID of the File</param>
    /// <param name="ct"></param>
    /// <returns>The File</returns>
    /// <response code="200">The file</response>
    /// <response code="404">File does not exist</response>
    /// <response code="500">File could not be loaded</response>
    public static async Task<Results<FileStreamHttpResult, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid fileId, CancellationToken ct)
    {
        if (await mangaContext.Files.FirstOrDefaultAsync(f => f.Id == fileId, ct) is not { } file)
            return TypedResults.NotFound();

        string path = Path.Join(file.Path, file.Name);
        try
        {
            await using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            MemoryStream memoryStream = new();
            await fs.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;
            return TypedResults.File(memoryStream, file.MimeType, file.Name);
        }
        catch
        {
            return TypedResults.InternalServerError();
        }
    }
}