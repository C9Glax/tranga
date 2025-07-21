using System.Text.RegularExpressions;
using JikanDotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace API.Schema.MangaContext.MetadataFetchers;

public class MyAnimeList : MetadataFetcher
{
    private static readonly Jikan Jikan = new ();
    private static readonly Regex GetIdFromUrl = new(@"https?:\/\/myanimelist\.net\/manga\/([0-9]+)\/?.*");
    
    public override MetadataSearchResult[] SearchMetadataEntry(Manga manga)
    {
        if (manga.Links.Any(link => link.LinkProvider.Equals("MyAnimeList", StringComparison.InvariantCultureIgnoreCase)))
        {
            string url = manga.Links.First(link => link.LinkProvider.Equals("MyAnimeList", StringComparison.InvariantCultureIgnoreCase)).LinkUrl;
            Match m = GetIdFromUrl.Match(url);
            if (m.Success && m.Groups[1].Success)
            {
                long id = long.Parse(m.Groups[1].Value);
                JikanDotNet.Manga data = Jikan.GetMangaAsync(id).Result.Data;
                return [new MetadataSearchResult(id.ToString(), data.Titles.First().Title, data.Url, data.Synopsis)];
            }
        }

        return SearchMetadataEntry(manga.Name);
    }

    public override MetadataSearchResult[] SearchMetadataEntry(string searchTerm)
    {
        
        ICollection<JikanDotNet.Manga> resultData = Jikan.SearchMangaAsync(searchTerm).Result.Data;
        if (resultData.Count < 1)
            return [];
        return resultData.Select(data =>
                new MetadataSearchResult(data.MalId.ToString(), data.Titles.First().Title, data.Url, data.Synopsis))
            .ToArray();
    }

    /// <summary>
    /// Updates the Manga linked in the MetadataEntry
    /// </summary>
    /// <param name="metadataEntry"></param>
    /// <param name="dbContext"></param>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="DbUpdateException"></exception>
    public override void UpdateMetadata(MetadataEntry metadataEntry, MangaContext dbContext)
    {
        Manga dbManga = dbContext.Mangas.Find(metadataEntry.MangaId)!;
        
        foreach (CollectionEntry collectionEntry in dbContext.Entry(dbManga).Collections)
            collectionEntry.Load();
        dbContext.Entry(dbManga).Navigation(nameof(Manga.Library)).Load();
        
        MangaFull resultData;
        try
        {
            long id = long.Parse(metadataEntry.Identifier);
            resultData = Jikan.GetMangaFullDataAsync(id).Result.Data;
        }
        catch (Exception)
        {
            throw new FormatException("ID was not in correct format");
        }

        try
        {
            dbManga.Name = resultData.Titles.First().Title;
            dbManga.Description = resultData.Synopsis;
            dbManga.AltTitles.Clear();
            dbManga.AltTitles = resultData.Titles.Select(t => new MangaAltTitle(t.Type, t.Title)).ToList();
            dbManga.Authors.Clear();
            dbManga.Authors = resultData.Authors.Select(a => new Author(a.Name)).ToList();

            dbContext.Sync();
        }
        catch (DbUpdateException e)
        {
            throw;
        }
    }
    
}