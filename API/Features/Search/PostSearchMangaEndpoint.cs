using API.Helpers;
using Common.Datatypes;
using Common.Helpers;
using Database.Helpers;
using Database.MangaContext;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Settings;

namespace API.Features.Search;

public abstract class PostSearchMangaEndpoint
{

    public static async Task<Results<Ok<Entities.MangaMetadata[]>, InternalServerError>> Handle(MangaContext mangaContext, [FromBody]SearchQuery query, CancellationToken ct)
    {
        List<SearchResult> searchResults = MetadataExtensionsCollection.SearchAll(query, ct);

        List<DbMetadataSource> db = [];
        
        foreach (SearchResult searchResult in searchResults)
        {

            if (await mangaContext.MetadataSources.Where(s =>
                        s.MetadataExtension == searchResult.MetadataExtensionIdentifier &&
                        s.Identifier == searchResult.Identifier || s.Series == searchResult.Series)
                    .FirstOrDefaultAsync(ct) is not { } existing)
            {
                DbMetadataSource metadataSource = await CreateMetadata(mangaContext, searchResult, ct);
                db.Add(metadataSource);
                
                await mangaContext.SaveChangesAsync(ct);
            }else db.Add(existing);
        }
                
        Entities.MangaMetadata[] results = db.Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(results);
    }

    private static async Task<DbMetadataSource> CreateMetadata(MangaContext mangaContext, SearchResult searchResult, CancellationToken ct)
    {
        DbManga manga = new ();
        await mangaContext.AddAsync(manga, ct);
        
        DbMetadataSource source = new()
        {
            MangaId = manga.MangaId,
            Priority = 0,
            MetadataExtension = searchResult.MetadataExtensionIdentifier,
            Identifier = searchResult.Identifier,
            Series = searchResult.Series,
            Summary = searchResult.Summary,
            Year = searchResult.Year,
            Url = searchResult.Url
        };
        
        await SaveCover(mangaContext, searchResult, source, ct);
        
        await mangaContext.AddAsync(source, ct);

        if (searchResult.Genres is { Length: > 0 } genres)
        {
            source.Genres = await mangaContext.Genres.Where(dbGenre => genres.Any(g => dbGenre.Genre == g))
                .ToListAsync(ct);
            source.Genres = source.Genres.UnionBy(genres.Select(g => new DbGenre() { Genre = g }), g=> g.Genre).ToArray();
        }
        
        if (searchResult.Artists is { Length: > 0 } artists)
        {
            source.Artists = await mangaContext.MangaArtists.Where(dbPerson => artists.Any(a => dbPerson.Name == a))
                .ToListAsync(ct);
            source.Artists = source.Artists.UnionBy(artists.Select(a => new DbPerson() { Name = a }), p=> p.Name).ToArray();
        }
        
        if (searchResult.Authors is { Length: > 0 } authors)
        {
            source.Authors = await mangaContext.MangaAuthors.Where(dbPerson => authors.Any(a => dbPerson.Name == a))
                .ToListAsync(ct);
            source.Authors = source.Authors.UnionBy(authors.Select(a => new DbPerson() { Name = a }), p=> p.Name).ToArray();
        }

        return source;
    }

    private static async Task SaveCover(MangaContext mangaContext, SearchResult searchResult, DbMetadataSource metadataSource, CancellationToken ct)
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
            metadataSource.CoverId = file.FileId;
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
            metadataSource.CoverId = file.FileId;
        }
    }
}