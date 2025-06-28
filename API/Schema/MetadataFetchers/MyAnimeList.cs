using System.Text.RegularExpressions;
using API.Schema.Contexts;
using JikanDotNet;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MetadataFetchers;

public class MyAnimeList : MetadataFetcher
{
    private static readonly Jikan Jikan = new ();
    private static readonly Regex GetIdFromUrl = new(@"https?:\/\/myanimelist\.net\/manga\/([0-9]+)\/?.*");
    
    public override MetadataEntry? FindLinkedMetadataEntry(Manga manga)
    {
        if (manga.Links.Any(link => link.LinkProvider.Equals("MyAnimeList", StringComparison.InvariantCultureIgnoreCase)))
        {
            string url = manga.Links.First(link => link.LinkProvider.Equals("MyAnimeList", StringComparison.InvariantCultureIgnoreCase)).LinkUrl;
            Match m = GetIdFromUrl.Match(url);
            if (m.Success && m.Groups[1].Success)
            {
                long id = long.Parse(m.Groups[1].Value);
                return new MetadataEntry(this, manga, id.ToString()!);
            }
        }

        ICollection<JikanDotNet.Manga> resultData = Jikan.SearchMangaAsync(manga.Name).Result.Data;
        if (resultData.Count < 1)
            return null;
        return new MetadataEntry(this, manga, resultData.First().MalId.ToString());
    }

    /// <summary>
    /// Updates the Manga linked in the MetadataEntry
    /// </summary>
    /// <param name="metadataEntry"></param>
    /// <param name="dbContext"></param>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="DbUpdateException"></exception>
    public override void UpdateMetadata(MetadataEntry metadataEntry, PgsqlContext dbContext)
    {
        Manga dbManga = dbContext.Mangas.Find(metadataEntry.MangaId)!;
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

            dbContext.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            throw;
        }
    }
    
}