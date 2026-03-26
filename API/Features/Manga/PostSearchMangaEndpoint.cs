using API.DTOs;
using Common.Datatypes;
using Common.Helpers;
using Database;
using Database.MangaContext;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Settings;

namespace API.Features.Manga;

/// <summary>
/// Searches for a Manga
/// </summary>
public abstract class PostSearchMangaEndpoint
{
    /// <summary>
    /// Searches for a Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="query"></param>
    /// <param name="ct"></param>
    /// <returns>The Search-result</returns>
    /// <response code="200">The Search-result</response>
    /// <response code="500">Error while searching for Manga</response>
    public static async Task<Results<Ok<MangaSearchResultDTO[]>, InternalServerError>> Handle(MangaContext mangaContext, [FromBody]SearchQuery query, CancellationToken ct)
    {
        List<SearchResult> searchResult = MetadataExtensionsCollection.SearchAll(query, ct);
        
        MangaSearchResultDTO[] result = await InsertNewDataIntoMangaContext(mangaContext, searchResult, ct);

        await mangaContext.SaveChangesAsync(ct);
        
        return TypedResults.Ok(result);
    }

    private static async Task<MangaSearchResultDTO[]> InsertNewDataIntoMangaContext(MangaContext mangaContext, List<SearchResult> searchResult, CancellationToken ct)
    {
        Dictionary<DbManga, DbMetadataLink> ret = new();
        foreach (SearchResult result in searchResult)
        {
            if (await mangaContext.Mangas
                    .Include(m => m.MetadataLinks)
                    .Select(m => new { manga = m, metadataLinks = m.MetadataLinks! })
                    .FirstOrDefaultAsync(pair => pair.metadataLinks.Any(l=> l.MetadataExtensionId == result.MetadataExtensionIdentifier && l.Identifier == result.Identifier)
                        , ct) is { } existing)
            {
                ret.Add(existing.manga, existing.metadataLinks.First(l => l.MetadataExtensionId == result.MetadataExtensionIdentifier && l.Identifier == result.Identifier));
            }
            else if (await mangaContext.Mangas.FirstOrDefaultAsync(m => m.Series == result.Series, ct) is { } manga)
            {
                DbMetadataLink link = new ()
                {
                    MangaId = manga.Id,
                    MetadataExtensionId = result.MetadataExtensionIdentifier,
                    Identifier = result.Identifier,
                };
                await mangaContext.AddAsync(link, ct);
                await SaveCover(link, result.Cover, ct);
                manga.MetadataLinks!.Add(link);
                ret.Add(manga, link);
            }
            else
            {
                DbManga newManga = new ()
                {
                    Series = result.Series,
                    Monitor = false
                };
                await mangaContext.AddAsync(newManga, ct);
                DbMetadataLink link = new ()
                {
                    MangaId = newManga.Id,
                    MetadataExtensionId = result.MetadataExtensionIdentifier,
                    Identifier = result.Identifier,
                };
                await mangaContext.AddAsync(link, ct);
                await SaveCover(link, result.Cover, ct);
                newManga.MetadataLinks!.Add(link);
                ret.Add(newManga, link);
            }
        }

        return ret.Select(kv => new MangaSearchResultDTO()
        {
            MangaId = kv.Key.Id,
            Description = kv.Value.Summary,
            MetadataId = kv.Value.Id,
            Title = kv.Key.Series,
            Url = kv.Value.Url
        }).ToArray();
    }

    private static async Task SaveCover(DbMetadataLink metadataLink, MemoryStream memoryStream, CancellationToken ct)
    {
        try
        {
            await memoryStream.ToJpeg(ct);
            metadataLink.Cover = new DbFile()
            {
                Name = $"{metadataLink.Id}.jpg",
                Path = Constants.CoverDirectory,
                MimeType = "image/jpeg"
            };
            await metadataLink.Cover.SaveFile(memoryStream, ct);
        }
        catch
        {
            metadataLink.Cover = new DbFile()
            {
                Name = $"{metadataLink.Id}",
                Path = Constants.CoverDirectory,
                MimeType = "image/png"
            };
            await metadataLink.Cover.SaveFile(memoryStream, ct);
        }
    }
}