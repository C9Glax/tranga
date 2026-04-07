using API.Entities;
using API.Helpers;
using Common.Helpers;
using Database.Helpers;
using Database.MangaContext;
using Extensions;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Settings;

namespace API.Features.Manga.Search;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class PostSearchMangaDownloadLinksEndpoint
{
    /// <summary>
    /// Search Manga on Download-Extensions
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga to Search</param>
    /// <param name="ct"></param>
    /// <returns>Search result</returns>
    /// <response code="200">Search result</response>
    /// <response code="404">Manga with ID does not exist</response>
    public static async Task<Results<Ok<MangaDownloadLink[]>, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } source)
            return TypedResults.NotFound();

        List<MangaInfo> searchResult = DownloadExtensionsCollection.SearchAll(source.ToSearchQuery(), ct);

        if (await mangaContext.MangaDownloadLinks.Where(m => m.MangaId == mangaId).ToListAsync(ct) is not { } existingSources)
            return TypedResults.InternalServerError();

        List<DbMangaDownloadLinks> result = [];
        foreach (MangaInfo mangaInfo in searchResult)
        {
            if (existingSources.FirstOrDefault(d =>
                    d.DownloadLink.DownloadExtension == mangaInfo.ExtensionIdentifier && d.DownloadLink.Identifier == mangaInfo.Identifier)
                is not { } existing)
            {
                DbDownloadLink downloadLink = new()
                {
                    DownloadExtension = mangaInfo.ExtensionIdentifier,
                    Identifier = mangaInfo.Identifier,
                    Series = mangaInfo.Title,
                    Summary = mangaInfo.Description,
                    Url = mangaInfo.Url,
                    NSFW = mangaInfo.NSFW
                };

                DbMangaDownloadLinks mangaDownloadLinks = new()
                {
                    DownloadLink = downloadLink,
                    Manga = source.Manga,
                    Matched = false,
                    Priority = 0
                };

                await mangaContext.AddAsync(mangaDownloadLinks, ct);
                await SaveCover(mangaContext, mangaInfo, downloadLink, ct);
                
                result.Add(mangaDownloadLinks);

            }else result.Add(existing);
        }

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok(result.Select(r => r.ToDTO()).ToArray());
    }
    
    

    private static async Task SaveCover(MangaContext mangaContext, MangaInfo mangaInfo, DbDownloadLink downloadLink, CancellationToken ct)
    {
        try
        {
            await mangaInfo.Cover.ToJpeg(ct);
            Guid coverId = Guid.CreateVersion7();
            DbFile file = new ()
            {
                FileId = coverId,
                Name = $"{coverId}.jpg",
                Path = Constants.CoverDirectory,
                MimeType = "image/jpeg"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(mangaInfo.Cover, ct);
            downloadLink.CoverId = file.FileId;
            downloadLink.Cover = file;
        }
        catch
        {
            Guid coverId = Guid.CreateVersion7();
            DbFile file = new ()
            {
                FileId = coverId,
                Name = $"{coverId}",
                Path = Constants.CoverDirectory,
                MimeType = "image/png"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(mangaInfo.Cover, ct);
            downloadLink.CoverId = file.FileId;
            downloadLink.Cover = file;
        }
    }
}