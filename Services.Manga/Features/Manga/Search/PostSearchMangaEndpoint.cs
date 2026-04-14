using Services.Manga.Helpers;
using Common.Datatypes;
using Common.Helpers;
using Extensions;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Settings;

namespace Services.Manga.Features.Manga.Search;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class PostSearchMangaEndpoint
{
    /// <summary>
    /// Search Manga on Metadata-Extensions
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns>Search result</returns>
    /// <response code="200">Search result</response>
    public static async Task<Results<Ok<Entities.Metadata[]>, BadRequest, InternalServerError>> Handle(MangaContext mangaContext, [FromBody]PostSearchMangaRequest req, CancellationToken ct)
    {
        if (req.SearchQuery is { MangaDexSeriesId: null, MangaUpdatesSeriesId: null, Title: null })
            return TypedResults.BadRequest();
        
        IMetadataExtension[] extensions = req.MetadataExtensionIds is { Length: > 0 }
            ? MetadataExtensionsCollection.Extensions.Where(e => req.MetadataExtensionIds.Contains(e.Identifier))
                .ToArray()
            : MetadataExtensionsCollection.Extensions;
        
        List<SearchResult> searchResults = MetadataExtensionsCollection.Search(req.SearchQuery, extensions, ct);

        List<DbMetadata> db = [];
        
        foreach (SearchResult searchResult in searchResults)
        {
            if (await mangaContext.MetadataEntries
                    .Include(s => s.MangaMetadataEntries)
                    .Where(s =>
                        s.MetadataExtension == searchResult.MetadataExtensionIdentifier &&
                        s.Identifier == searchResult.Identifier || s.Series == searchResult.Series)
                    .FirstOrDefaultAsync(ct) is not { } existing)
            {
                DbMetadata metadata = await CreateMetadata(mangaContext, searchResult, ct);
                db.Add(metadata);
                
                await mangaContext.SaveChangesAsync(ct);
            }else db.Add(existing);
        }
                
        Entities.Metadata[] results = db.Distinct().Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(results);
    }

    /// <summary>
    /// Used in <see cref="PostSearchMangaEndpoint"/>
    /// </summary>
    /// <param name="SearchQuery">Search Query</param>
    /// <param name="MetadataExtensionIds">IDs of Metadata Extensions to Search on</param>
    public sealed record PostSearchMangaRequest(SearchQuery SearchQuery, Guid[]? MetadataExtensionIds);

    private static async Task<DbMetadata> CreateMetadata(MangaContext mangaContext, SearchResult searchResult, CancellationToken ct)
    {
        DbManga manga = new ()
        {
            Monitored = false,
            MetadataEntries = []
        };
        
        DbMetadata source = new()
        {
            MetadataExtension = searchResult.MetadataExtensionIdentifier,
            Identifier = searchResult.Identifier,
            Series = searchResult.Series,
            Summary = searchResult.Summary,
            Year = searchResult.Year,
            Url = searchResult.Url,
            Status = searchResult.Status,
            MangaMetadataEntries = [],
            NSFW = searchResult.NSFW
        };
        
        await SaveCover(mangaContext, searchResult, source, ct);

        DbMangaMetadataEntries mangaMetadataEntries = new()
        {
            Manga = manga,
            Metadata = source,
            Chosen = false
        };
        source.MangaMetadataEntries.Add(mangaMetadataEntries);
        manga.MetadataEntries.Add(mangaMetadataEntries);
        
        await mangaContext.AddAsync(manga, ct);

        if (searchResult.Genres is { Length: > 0 } genres)
        {
            source.Genres = await mangaContext.Genres.Where(dbGenre => genres.Any(g => dbGenre.Genre == g))
                .ToListAsync(ct);
            source.Genres = source.Genres.UnionBy(genres.Select(g => new DbGenre() { Genre = g }), g=> g.Genre).ToArray();
        }
        
        if (searchResult.Artists is { Length: > 0 } artists)
        {
            source.Artists = await mangaContext.Artists.Where(dbPerson => artists.Any(a => dbPerson.Name == a))
                .ToListAsync(ct);
            source.Artists = source.Artists.UnionBy(artists.Select(a => new DbPerson() { Name = a }), p=> p.Name).ToArray();
        }
        
        if (searchResult.Authors is { Length: > 0 } authors)
        {
            source.Authors = await mangaContext.Authors.Where(dbPerson => authors.Any(a => dbPerson.Name == a))
                .ToListAsync(ct);
            source.Authors = source.Authors.UnionBy(authors.Select(a => new DbPerson() { Name = a }), p=> p.Name).ToArray();
        }

        return source;
    }

    private static async Task SaveCover(MangaContext mangaContext, SearchResult searchResult, DbMetadata metadata, CancellationToken ct)
    {
        try
        {
            await searchResult.Cover.ToJpeg(ct);
            Guid coverId = Guid.CreateVersion7();
            DbFile file = new ()
            {
                FileId = coverId,
                Name = $"{coverId}.jpg",
                Path = Constants.CoverDirectory,
                MimeType = "image/jpeg"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(searchResult.Cover, ct);
            metadata.CoverId = file.FileId;
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
            await file.SaveFile(searchResult.Cover, ct);
            metadata.CoverId = file.FileId;
        }
    }
}