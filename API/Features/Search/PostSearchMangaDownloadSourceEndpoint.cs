using API.Entities;
using API.Helpers;
using Common.Helpers;
using Database.Helpers;
using Database.MangaContext;
using DownloadExtensions;
using DownloadExtensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Settings;

namespace API.Features.Search;

public abstract class PostSearchMangaDownloadSourceEndpoint
{
    public static async Task<Results<Ok<DownloadLink[]>, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } source)
            return TypedResults.NotFound();

        List<MangaInfo> searchResult = DownloadExtensionsCollection.SearchAll(source.ToSearchQuery(), ct);

        if (await mangaContext.MangaDownloadSources.Where(m => m.MangaId == mangaId).ToListAsync(ct) is not { } existingSources)
            return TypedResults.InternalServerError();

        List<DbMangaDownloadSource> result = [];
        foreach (MangaInfo mangaInfo in searchResult)
        {
            if (existingSources.FirstOrDefault(d =>
                    d.DownloadSource.DownloadExtension == mangaInfo.ExtensionIdentifier && d.DownloadSource.Identifier == mangaInfo.Identifier)
                is not { } existing)
            {
                DbDownloadSource downloadSource = new()
                {
                    DownloadExtension = mangaInfo.ExtensionIdentifier,
                    Identifier = mangaInfo.Identifier,
                    Series = mangaInfo.Title,
                    Summary = mangaInfo.Description,
                    Url = mangaInfo.Url,
                };

                DbMangaDownloadSource mangaDownloadSource = new()
                {
                    DownloadSource = downloadSource,
                    Manga = source.Manga,
                    Matched = false,
                    Priority = int.MaxValue
                };

                await mangaContext.AddAsync(mangaDownloadSource, ct);
                await SaveCover(mangaContext, mangaInfo, downloadSource, ct);
                
                result.Add(mangaDownloadSource);

            }else result.Add(existing);
        }

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok(result.Select(r => r.ToDTO()).ToArray());
    }
    
    

    private static async Task SaveCover(MangaContext mangaContext, MangaInfo mangaInfo, DbDownloadSource downloadSource, CancellationToken ct)
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
            downloadSource.CoverId = file.FileId;
            downloadSource.Cover = file;
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
            downloadSource.CoverId = file.FileId;
            downloadSource.Cover = file;
        }
    }
}