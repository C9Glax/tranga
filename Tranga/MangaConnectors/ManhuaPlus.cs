using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class ManhuaPlus : MangaConnector
{
    //["en"], ["manhuaplus.org"]
    public ManhuaPlus(string mangaConnectorName) : base(mangaConnectorName, new ChromiumDownloadClient())
    {
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
    {
        log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://manhuaplus.org/search?keyword={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        if (requestResult.htmlDocument is null)
            return [];
        (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        log.Info($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }
    
    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectSingleNode("//h1/../..").ChildNodes//I already want to not.
                .Any(node => node.InnerText.Contains("No manga found")))
            return [];

        List<string> urls = document.DocumentNode
            .SelectNodes("//h1/../..//a[contains(@href, 'https://manhuaplus.org/manga/') and contains(concat(' ',normalize-space(@class),' '),' clamp ') and not(contains(@href, '/chapter'))]")
                .Select(mangaNode => mangaNode.GetAttributeValue("href", "")).ToList();
        log.Info($"Got {urls.Count} urls.");

        List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();
        foreach (string url in urls)
        {
            (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add(((Manga, Author[], MangaTag[], Link[], MangaAltTitle[]))manga);
        }

        return ret.ToArray();
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://manhuaplus.org/manga/{publicationId}");
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/manhuaplus.org\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 && requestResult.htmlDocument is not null && requestResult.redirectedToUrl != "https://manhuaplus.org/home") //When manga doesnt exists it redirects to home
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("/html/body/main/div/div/div[2]/div[1]/figure/a/img");//BRUH
        Regex posterRex = new(@".*(\/uploads/covers/[a-zA-Z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]+).*");
        string posterUrl = $"https://manhuaplus.org/{posterRex.Match(posterNode.GetAttributeValue("src", "")).Groups[1].Value}";

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//h1");
        string sortName = titleNode.InnerText.Replace("\n", "");
        
        Author[] authors = [];
        try
        {
            HtmlNode[] authorsNodes = document.DocumentNode
                .SelectNodes("//a[contains(@href, 'https://manhuaplus.org/authors/')]")
                .ToArray();
            authors = authorsNodes.Select(a => new Author(a.InnerText)).ToArray();
        }
        catch (ArgumentNullException e)
        {
            log.Info("No authors found.");
        }

        MangaTag[] tags = [];
        try
        {
            HtmlNode[] genreNodes = document.DocumentNode
                .SelectNodes("//a[contains(@href, 'https://manhuaplus.org/genres/')]").ToArray();
            tags = genreNodes.Select(genreNode => new MangaTag(genreNode.InnerText.Replace("\n", ""))).ToArray();
        }
        catch (ArgumentNullException e)
        {
            log.Info("No genres found");
        }

        Regex yearRex = new(@"(?:[0-9]{1,2}\/){2}([0-9]{2,4}) [0-9]{1,2}:[0-9]{1,2}");
        HtmlNode yearNode = document.DocumentNode.SelectSingleNode("//aside//i[contains(concat(' ',normalize-space(@class),' '),' fa-clock ')]/../span");
        Match match = yearRex.Match(yearNode.InnerText);
        uint year = match.Success && match.Groups[1].Success ? uint.Parse(match.Groups[1].Value) : 0;

        string status = document.DocumentNode.SelectSingleNode("//aside//i[contains(concat(' ',normalize-space(@class),' '),' fa-rss ')]/../span").InnerText.Replace("\n", "");
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "hiatus": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "discontinued": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "complete": releaseStatus = MangaReleaseStatus.Completed; break;
            case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectSingleNode("//div[@id='syn-target']");
        string description = descriptionNode.InnerText;

        Manga manga = new(MangaConnectorName,sortName, description, posterUrl, null, year, null,
            releaseStatus, 0, null, null, publicationId,
            authors.Select(a => a.AuthorId).ToArray(),
            tags.Select(t => t.Tag).ToArray(),
            [],
            []);
        return (manga, authors, tags, [], []);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        log.Info($"Getting chapters {manga}");
        RequestResult result = downloadClient.MakeRequest($"https://manhuaplus.org/manga/{manga.ConnectorId}", RequestType.Default);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            return Array.Empty<Chapter>();
        }
        
        HtmlNodeCollection chapterNodes = result.htmlDocument.DocumentNode.SelectNodes("//li[contains(concat(' ',normalize-space(@class),' '),' chapter ')]//a");
        string[] urls = chapterNodes.Select(node => node.GetAttributeValue("href", "")).ToArray();
        Regex urlRex = new (@".*\/chapter-([0-9\-]+).*");
        
        List<Chapter> chapters = new();
        foreach (string url in urls)
        {
            Match rexMatch = urlRex.Match(url);

            string chapterNumber = rexMatch.Groups[1].Value;
            string fullUrl = url;
            if (!float.TryParse(chapterNumber, NumberFormatDecimalPoint, out float chNum))
            {
                log.Debug($"Failed parsing {chapterNumber} as float.");
                continue;
            }
            chapters.Add(new Chapter(manga, fullUrl, chNum));
        }
        //Return Chapters ordered by Chapter-Number
        log.Info($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    protected override string[] GetChapterImages(Chapter chapter)
    {
        Manga chapterParentManga = chapter.ParentManga;
        log.Info($"Retrieving chapter-info {chapter} {chapterParentManga}");

        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.Url, RequestType.Default);
        if (requestResult.htmlDocument is null)
        {
            return [];
        }

        HtmlDocument document = requestResult.htmlDocument;
        
        HtmlNode[] images = document.DocumentNode.SelectNodes("//a[contains(concat(' ',normalize-space(@class),' '),' readImg ')]/img").ToArray();
        List<string> urls = images.Select(node => node.GetAttributeValue("src", "")).ToList();

        return urls.ToArray();
    }
}