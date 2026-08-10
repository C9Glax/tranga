using Common;
using Services.Manga.Helpers;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

namespace Services.Manga.Features.Manga.Search;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostSearchMangaEndpoint
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

        List<DbMetadata> metadataList = [];
        
        foreach (SearchResult searchResult in searchResults)
        {
            if (await FindExistingMetadata(mangaContext, searchResult, ct) is not { } existing)
            {
                DbMetadata metadata = await CreateMetadata(mangaContext, searchResult, ct);
                metadataList.Add(metadata);

                await mangaContext.SaveChangesAsync(ct);
            }
            else
            {
                await MergeMetadata(mangaContext, searchResult, existing, ct);
                metadataList.Add(existing);

                await mangaContext.SaveChangesAsync(ct);
            }
        }
                
        Entities.Metadata[] results = metadataList.Distinct().Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(results);
    }

    /// <summary>
    /// Used in <see cref="PostSearchMangaEndpoint"/>
    /// </summary>
    /// <param name="SearchQuery">Search Query</param>
    /// <param name="MetadataExtensionIds">IDs of Metadata Extensions to Search on</param>
    public sealed record PostSearchMangaRequest(SearchQuery SearchQuery, Guid[]? MetadataExtensionIds);

    /// <summary>
    /// Looks up a previously-stored <see cref="DbMetadata"/> matching a search result. Matching is scoped to the
    /// same extension (<see cref="SearchResult.MetadataExtensionIdentifier"/>): a result is only considered the
    /// same entry as an existing row if it came from the same extension, and either shares the extension's own
    /// identifier or the same series title. Without the extension scope, two different extensions' results for a
    /// series with the same title would collapse into one row, silently overwriting fields (e.g. Url) from one
    /// extension with another's.
    /// </summary>
    internal static async Task<DbMetadata?> FindExistingMetadata(MangaContext mangaContext, SearchResult searchResult, CancellationToken ct) =>
        await mangaContext.MetadataEntries
            .Include(s => s.MangaMetadataEntries)
            .Include(s => s.Genres)
            .Include(s => s.Artists)
            .Include(s => s.Authors)
            .Where(s =>
                s.MetadataExtension == searchResult.MetadataExtensionIdentifier &&
                (s.Identifier == searchResult.Identifier || s.Series == searchResult.Series))
            .FirstOrDefaultAsync(ct);

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

    /// <summary>
    /// Refreshes an already-known <see cref="DbMetadata"/> entry with freshly-fetched search data,
    /// filling in missing fields and merging Genres/Artists/Authors rather than discarding the existing record.
    /// </summary>
    internal static async Task MergeMetadata(MangaContext mangaContext, SearchResult searchResult, DbMetadata existing, CancellationToken ct)
    {
        existing.Series = searchResult.Series;

        if (searchResult.Summary is { } summary)
            existing.Summary = summary;

        if (searchResult.Year is { } year)
            existing.Year = year;

        if (searchResult.Url is { } url)
            existing.Url = url;

        if (searchResult.Status is { } status)
            existing.Status = status;

        if (searchResult.NSFW is { } nsfw)
            existing.NSFW = nsfw;

        if (existing.CoverId is null)
            await SaveCover(mangaContext, searchResult, existing, ct);

        if (searchResult.Genres is { Length: > 0 } genres)
        {
            List<DbGenre> genreEntities = await mangaContext.Genres.Where(dbGenre => genres.Any(g => dbGenre.Genre == g))
                .ToListAsync(ct);
            genreEntities = genreEntities.UnionBy(genres.Select(g => new DbGenre() { Genre = g }), g => g.Genre).ToList();
            existing.Genres = (existing.Genres ?? []).UnionBy(genreEntities, g => g.Genre).ToArray();
        }

        if (searchResult.Artists is { Length: > 0 } artists)
        {
            List<DbPerson> artistEntities = await mangaContext.Artists.Where(dbPerson => artists.Any(a => dbPerson.Name == a))
                .ToListAsync(ct);
            artistEntities = artistEntities.UnionBy(artists.Select(a => new DbPerson() { Name = a }), p => p.Name).ToList();
            existing.Artists = (existing.Artists ?? []).UnionBy(artistEntities, p => p.Name).ToArray();
        }

        if (searchResult.Authors is { Length: > 0 } authors)
        {
            List<DbPerson> authorEntities = await mangaContext.Authors.Where(dbPerson => authors.Any(a => dbPerson.Name == a))
                .ToListAsync(ct);
            authorEntities = authorEntities.UnionBy(authors.Select(a => new DbPerson() { Name = a }), p => p.Name).ToList();
            existing.Authors = (existing.Authors ?? []).UnionBy(authorEntities, p => p.Name).ToArray();
        }
    }

    private static async Task SaveCover(MangaContext mangaContext, SearchResult searchResult, DbMetadata metadata, CancellationToken ct)
    {
        try
        {
            await searchResult.Cover.ToJpeg(ct);
        }
        catch
        {
            // Cover data isn't a valid/supported image - skip saving it rather than storing the raw,
            // undecoded bytes under a made-up MIME type (consumers like Komga reject/fail to decode those).
            return;
        }

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
}