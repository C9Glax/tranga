using System.Text.RegularExpressions;
using JikanDotNet;
using JikanDotNet.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace API.Schema.MangaContext.MetadataFetchers;

public class MyAnimeList : MetadataFetcher
{
    private static readonly Jikan Jikan = new (new JikanClientConfiguration(), new HttpClient()
    {
        BaseAddress = new Uri(Constants.JikanBaseUrl.EndsWith('/') ? Constants.JikanBaseUrl : $"{Constants.JikanBaseUrl}/")
    });
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

        // Only fill in what is missing: the name, description and authors chosen by the user or the MangaConnector are kept.
        if (string.IsNullOrWhiteSpace(dbManga.Description) && !string.IsNullOrWhiteSpace(resultData.Synopsis))
            dbManga.Description = resultData.Synopsis;

        if (dbManga.Year is null && resultData.Published?.From?.Year is { } year)
            dbContext.Entry(dbManga).Property(m => m.Year).CurrentValue = (uint)year;

        if (dbManga.Authors.Count == 0)
            foreach (string authorName in resultData.Authors.Select(a => AuthorName(a.Name)).Distinct())
            {
                Author author = new (authorName);
                dbManga.Authors.Add(await dbContext.Authors.FindAsync([author.Key], token) ?? author);
            }

        foreach (TitleEntry title in resultData.Titles)
        {
            if (string.IsNullOrWhiteSpace(title.Title) || title.Title.Length > 256 ||
                title.Title.Equals(dbManga.Name, StringComparison.InvariantCultureIgnoreCase) ||
                dbManga.AltTitles.Any(t => t.Title.Equals(title.Title, StringComparison.InvariantCultureIgnoreCase)))
                continue;
            dbManga.AltTitles.Add(new AltTitle(TitleLanguage(title.Type), title.Title));
        }

        if (await dbContext.Sync(token, GetType(), "Update metadata") is { success: true })
        {
            Log.InfoFormat("Updated Metadata: {0}", metadataEntry.MangaId);
        }
    }

    /// <summary>
    /// MyAnimeList lists people as "Last, First". Comma separated names would be split into several people (e.g. ComicInfo Writer).
    /// </summary>
    private static string AuthorName(string malName) =>
        malName.Split(',', 2, StringSplitOptions.TrimEntries) is [{ Length: > 0 } last, { Length: > 0 } first] ? $"{first} {last}" : malName.Trim();

    private static string TitleLanguage(string? type) => type switch
    {
        "English" => "en",
        "Japanese" => "ja",
        "German" => "de",
        "French" => "fr",
        "Spanish" => "es",
        _ => "und"
    };
}
