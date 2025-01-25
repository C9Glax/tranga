﻿using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Mangaworld : MangaConnector
{
    public Mangaworld() : base("Mangaworld", ["it"], ["www.mangaworld.ac"])
    {
        this.downloadClient = new ChromiumDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://www.mangaworld.ac/archive?keyword={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        if (requestResult.htmlDocument is null)
            return [];
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (!document.DocumentNode.SelectSingleNode("//div[@class='comics-grid']").ChildNodes
                .Any(node => node.HasClass("entry")))
            return [];
        
        List<string> urls = document.DocumentNode
            .SelectNodes(
                "//div[@class='comics-grid']//div[@class='entry']//a[contains(concat(' ',normalize-space(@class),' '),'thumb')]")
            .Select(thumb => thumb.GetAttributeValue("href", "")).ToList();

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
        foreach (string url in urls)
        {
            (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
            if (manga is { } x)
                ret.Add(x);
        }

        return ret.ToArray();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://www.mangaworld.ac/manga/{publicationId}");
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        
        if (requestResult.htmlDocument is null)
            return null;
        
        Regex idRex = new (@"https:\/\/www\.mangaworld\.[a-z]{0,63}\/manga\/([0-9]+\/[0-9A-z\-]+).*");
        string id = idRex.Match(url).Groups[1].Value;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, id, url);
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        Dictionary<string, string> altTitlesDict = new();
        string originalLanguage = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("info"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        HtmlNode metadata = infoNode.Descendants().First(d => d.HasClass("meta-data"));

        HtmlNode altTitlesNode = metadata.SelectSingleNode("//span[text()='Titoli alternativi: ' or text()='Titolo alternativo: ']/..").ChildNodes[1];
        string[] alts = altTitlesNode.InnerText.Split(", ");
        for(int i = 0; i < alts.Length; i++)
            altTitlesDict.Add(i.ToString(), alts[i]);
        List<MangaAltTitle> altTitles = altTitlesDict.Select(a => new MangaAltTitle(a.Key, a.Value)).ToList();

        HtmlNode genresNode =
            metadata.SelectSingleNode("//span[text()='Generi: ' or text()='Genero: ']/..");
        HashSet<string> tags = genresNode.SelectNodes("a").Select(node => node.InnerText).ToHashSet();
        List<MangaTag> mangaTags = tags.Select(t => new MangaTag(t)).ToList();
        
        HtmlNode authorsNode =
            metadata.SelectSingleNode("//span[text()='Autore: ' or text()='Autori: ']/..");
        string[] authorNames = authorsNode.SelectNodes("a").Select(node => node.InnerText).ToArray();
        List<Author> authors = authorNames.Select(n => new Author(n)).ToList();

        string status = metadata.SelectSingleNode("//span[text()='Stato: ']/..").SelectNodes("a").First().InnerText;
        // ReSharper disable 5 times StringLiteralTypo
        switch (status.ToLower())
        {
            case "cancellato": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "in pausa": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "droppato": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "finito": releaseStatus = MangaReleaseStatus.Completed; break;
            case "in corso": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        string coverUrl = document.DocumentNode.SelectSingleNode("//img[@class='rounded']").GetAttributeValue("src", "");

        string description = document.DocumentNode.SelectSingleNode("//div[@id='noidungm']").InnerText;
        
        string yearString = metadata.SelectSingleNode("//span[text()='Anno di uscita: ']/..").SelectNodes("a").First().InnerText;
        uint year = uint.Parse(yearString);
        
        Manga manga = new (publicationId, sortName, description, websiteUrl, coverUrl, null, year,
            originalLanguage, releaseStatus, -1,
            this, 
            authors, 
            mangaTags, 
            [],
            altTitles);
		
        return (manga, authors, mangaTags, [], altTitles);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        string requestUrl = $"https://www.mangaworld.ac/manga/{manga.MangaId}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            return [];
        
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        return chapters.Order().ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chaptersWrapper =
            document.DocumentNode.SelectSingleNode(
                "//div[contains(concat(' ',normalize-space(@class),' '),'chapters-wrapper')]");

        Regex volumeRex = new(@"[Vv]olume ([0-9]+).*");
        Regex chapterRex = new(@"[Cc]apitolo ([0-9]+(?:\.[0-9]+)?).*");
        Regex idRex = new(@".*\/read\/([a-z0-9]+)(?:[?\/].*)?");
        if (chaptersWrapper.Descendants("div").Any(descendant => descendant.HasClass("volume-element")))
        {
            foreach (HtmlNode volNode in document.DocumentNode.SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),'volume-element')]"))
            {
                string volumeStr = volumeRex.Match(volNode.SelectNodes("div").First(node => node.HasClass("volume")).SelectSingleNode("p").InnerText).Groups[1].Value;
                int volume = int.Parse(volumeStr);
                foreach (HtmlNode chNode in volNode.SelectNodes("div").First(node => node.HasClass("volume-chapters")).SelectNodes("div"))
                {

                    string numberStr = chapterRex.Match(chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText).Groups[1].Value;
                    
                    string chapterNumber = new(numberStr);
                    string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                    string id = idRex.Match(chNode.SelectSingleNode("a").GetAttributeValue("href", "")).Groups[1].Value;
                    try
                    {
                        ret.Add(new Chapter(manga, url, chapterNumber, volume, null));
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }
        else
        {
            foreach (HtmlNode chNode in chaptersWrapper.SelectNodes("div").Where(node => node.HasClass("chapter")))
            {
                string numberStr = chapterRex.Match(chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText).Groups[1].Value;
                
                string chapterNumber = new(numberStr);
                string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                string id = idRex.Match(chNode.SelectSingleNode("a").GetAttributeValue("href", "")).Groups[1].Value;
                try
                {
                    ret.Add(new Chapter(manga, url, chapterNumber, null, null));
                }
                catch (Exception e)
                {
                }
            }
        }

        ret.Reverse();
        return ret;
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        string requestUrl = $"{chapter.Url}?style=list";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            return [];
        }

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);
        return imageUrls;
    }

    private string[] ParseImageUrlsFromHtml(HtmlDocument document)
    {
        List<string> ret = new();

        HtmlNode imageContainer =
            document.DocumentNode.SelectSingleNode("//div[@id='page']");
        foreach(HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}