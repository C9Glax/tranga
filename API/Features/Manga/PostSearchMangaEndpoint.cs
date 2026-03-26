using API.DTOs;
using Common.Datatypes;
using Common.Helpers;
using Database.MangaContext;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        // Crimes have been committed.
        Dictionary<DbMetadataLink, SearchResult> metadataEntries = (await mangaContext.Mangas.Include(m => m.MetadataLinks)
            .SelectMany(m => m.MetadataLinks!)
            .Where(l => searchResult.Any(s =>
                s.MetadataExtensionIdentifier == l.MetadataExtensionId && s.Identifier == l.Identifier))
            .ToListAsync(ct))
            .ToDictionary(l => l,
                l => searchResult.First(s =>
                    s.MetadataExtensionIdentifier == l.MetadataExtensionId && s.Identifier == l.Identifier));
        
        // So many crimes.
        List<SearchResult> missing = searchResult.Where(s => !metadataEntries.ContainsValue(s)).ToList();
        Dictionary<DbManga, SearchResult> mangaEntries = (await mangaContext.Mangas
                .Include(m => m.MetadataLinks)
                .Where(m => missing.Any(sr => sr.Series == m.Series))
                .ToListAsync(ct))
            .ToDictionary(m => m, m => missing.First(sr => sr.Series == m.Series));
        
        missing = missing.Where(s => !mangaEntries.ContainsValue(s)).ToList();
        
        // Create new Manga in DB
        foreach (SearchResult result in missing)
        {
            DbManga manga = new ()
            {
                Series = result.Series,
                Monitor = false
            };
            await mangaContext.AddAsync(manga, ct);
            mangaEntries.Add(manga, result);
        }
        
        // Create new MetadataLinks in DB
        foreach ((DbManga manga, SearchResult result) in mangaEntries)
        {
            DbMetadataLink link = new ()
            {
                MangaId = manga.Id,
                MetadataExtensionId = result.MetadataExtensionIdentifier,
                Identifier = result.Identifier,
            };
            await SaveCover(link, result.Cover, ct);
            manga.MetadataLinks!.Add(link);
            metadataEntries.Add(link, result);
        }

        return metadataEntries.Select(kv => new MangaSearchResultDTO()
        {
            MangaId = kv.Key.MangaId,
            Description = kv.Key.Summary,
            MetadataId = kv.Key.Id,
            Title = kv.Key.Manga!.Series,
            Url = kv.Key.Url ?? string.Empty
        }).ToArray();
    }

    private static async Task SaveCover(DbMetadataLink metadataLink, MemoryStream memoryStream, CancellationToken ct)
    {
        (string fileName, string path) = await MangaCover.SaveCover(metadataLink.Id.ToString(), memoryStream, ct);
        metadataLink.Cover = new DbFile()
        {
            Name = fileName,
            Path = path
        };
    }
}