using API.Entities;
using Common.Datatypes;
using Common.Helpers;
using Database.Helpers;
using Database.MangaContext;
using DownloadExtensions;
using DownloadExtensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Settings;

namespace API.Features.Search;

public abstract class PostMatchMangaEndpoint
{
    public static async Task<Results<Ok<MatchResult[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        List<MangaInfo> mangaInfos = DownloadExtensionsCollection.SearchAll(GetSearchQuery(manga), ct);

        List<MatchResult> res = [];
        foreach (MangaInfo mangaInfo in mangaInfos)
        {
            res.Add(await GetMatchResult(mangaContext, manga, mangaInfo, ct));
        }
        
        await mangaContext.SaveChangesAsync(ct);
        
        return TypedResults.Ok(res.ToArray());
    }

    private static SearchQuery GetSearchQuery(DbMetadataSource metadataSource) => new()
    {
        Title = metadataSource.Series,
        MangaUpdatesSeriesId = metadataSource.MetadataExtension == Guid.Parse("019cf2cb-3aac-7c9c-9580-7091471b6788")
            ? long.Parse(metadataSource.Identifier)
            : null
    };

    private static async Task<MatchResult> GetMatchResult(MangaContext mangaContext, DbMetadataSource metadataSource, MangaInfo mangaInfo, CancellationToken ct)
    {
        Guid fileId = await SaveCover(mangaContext, mangaInfo, ct);
        
        
        
        MatchResult m = new ()
        {
            MangaId = metadataSource.MangaId,
            DownloadExtensionId = mangaInfo.ExtensionIdentifier,
            Identifier = mangaInfo.Identifier,
            Series = mangaInfo.Title,
            Summary = mangaInfo.Description,
            Url = mangaInfo.Url,
            CoverId = fileId
        };

        return m;
    }

    private static async Task<Guid> SaveCover(MangaContext mangaContext, MangaInfo mangaInfo, CancellationToken ct)
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
            return file.FileId;
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
            return file.FileId;
        }
    }
}