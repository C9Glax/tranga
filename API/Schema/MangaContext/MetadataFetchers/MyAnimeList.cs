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
        Log.DebugFormat("Searching '{0}'...", searchTerm);
        ICollection<JikanDotNet.Manga> resultData = Jikan.SearchMangaAsync(searchTerm).Result.Data;
        Log.DebugFormat("Found {0} results.", resultData.Count);
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
    /// <param name="token"></param>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="DbUpdateException"></exception>
    public override async Task UpdateMetadata(MetadataEntry metadataEntry, MangaContext dbContext, CancellationToken token)
    {
        Log.DebugFormat("Updating Metadata: {0}", metadataEntry.MangaId);
        Manga? dbManga = metadataEntry.Manga; //Might be null!
        if (dbManga is null)
        {
            if (await dbContext.Mangas.FirstOrDefaultAsync(m => m.Key == metadataEntry.MangaId, token) is not
                { } update)
                throw new DbUpdateException("Manga not found");
            dbManga = update;
        }

        // Load all collections (tags, links, authors)...
        foreach (CollectionEntry collectionEntry in dbContext.Entry(dbManga).Collections)
        {
            if(!collectionEntry.IsLoaded)
                await collectionEntry.LoadAsync(token);
        }
        await dbContext.Entry(dbManga).Reference(m => m.Library).LoadAsync(token);
        
        MangaFull resultData;
        try
        {
            long id = long.Parse(metadataEntry.Identifier);
            if (await Jikan.GetMangaFullDataAsync(id, token) is not { } response)
            {
                Log.ErrorFormat("Manga Data not found: {0}", metadataEntry.MangaId);
                return;
            }
            resultData = response.Data;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return;
        }

        dbManga.Name = resultData.Titles.First().Title;
        dbManga.Description = resultData.Synopsis;
        dbManga.AltTitles.Clear();
        dbManga.AltTitles = resultData.Titles.Select(t => new AltTitle(t.Type, t.Title)).ToList();
        dbManga.Authors.Clear();
        dbManga.Authors = resultData.Authors.Select(a => new Author(a.Name)).ToList();

        if (await dbContext.Sync(token, GetType(), "Update metadata") is { success: true })
        {
            Log.InfoFormat("Updated Metadata: {0}", metadataEntry.MangaId);
        }
    }
    
}