using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class ManhuaPlus : MangaConnector
{
    public ManhuaPlus() : base("ManhuaPlus", ["en"], ["manhuaplus.org"])
    {
        this.downloadClient = new ChromiumDownloadClient();
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://manhuaplus.org/search?keyword={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Manga>();

        if (requestResult.htmlDocument is null)
            return Array.Empty<Manga>();
        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }
    
    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectSingleNode("//h1/../..").ChildNodes//I already want to not.
                .Any(node => node.InnerText.Contains("No manga found")))
            return Array.Empty<Manga>();

        List<string> urls = document.DocumentNode
            .SelectNodes("//h1/../..//a[contains(@href, 'https://manhuaplus.org/manga/') and contains(concat(' ',normalize-space(@class),' '),' clamp ') and not(contains(@href, '/chapter'))]")
                .Select(mangaNode => mangaNode.GetAttributeValue("href", "")).ToList();

        HashSet<Manga> ret = new();
        foreach (string url in urls)
        {
            Manga? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add((Manga)manga);
        }

        return ret.ToArray();
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://manhuaplus.org/manga/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/manhuaplus.org\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 && requestResult.htmlDocument is not null && requestResult.redirectedToUrl != "https://manhuaplus.org/home") //When manga doesnt exists it redirects to home
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("/html/body/main/div/div/div[2]/div[1]/figure/a/img");//BRUH
        Regex posterRex = new(@".*(\/uploads/covers/[a-zA-Z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]+).*");
        string posterUrl = $"https://manhuaplus.org/{posterRex.Match(posterNode.GetAttributeValue("src", "")).Groups[1].Value}";

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//h1");
        string sortName = titleNode.InnerText.Replace("\n", "");
        
        List<string> authors = new();
        try
        {
            HtmlNode[] authorsNodes = document.DocumentNode
                .SelectNodes("//a[contains(@href, 'https://manhuaplus.org/authors/')]")
                .ToArray();
            foreach (HtmlNode authorNode in authorsNodes)
                authors.Add(authorNode.InnerText);
        }
        catch (ArgumentNullException e)
        {
        }

        try
        {
            HtmlNode[] genreNodes = document.DocumentNode
                .SelectNodes("//a[contains(@href, 'https://manhuaplus.org/genres/')]").ToArray();
            foreach (HtmlNode genreNode in genreNodes)
                tags.Add(genreNode.InnerText.Replace("\n", ""));
        }
        catch (ArgumentNullException e)
        {
        }

        Regex yearRex = new(@"(?:[0-9]{1,2}\/){2}([0-9]{2,4}) [0-9]{1,2}:[0-9]{1,2}");
        HtmlNode yearNode = document.DocumentNode.SelectSingleNode("//aside//i[contains(concat(' ',normalize-space(@class),' '),' fa-clock ')]/../span");
        Match match = yearRex.Match(yearNode.InnerText);
        int year = match.Success && match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 1960;

        status = document.DocumentNode.SelectSingleNode("//aside//i[contains(concat(' ',normalize-space(@class),' '),' fa-rss ')]/../span").InnerText.Replace("\n", "");
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

        Manga manga = //TODO
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        RequestResult result = downloadClient.MakeRequest($"https://manhuaplus.org/manga/{manga.MangaId}", RequestType.Default);
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

            float chapterNumber = float.Parse(rexMatch.Groups[1].Value);
            string fullUrl = url;
            try
            {
                chapters.Add(new Chapter(manga, fullUrl, chapterNumber, null, null));
            }
            catch (Exception e)
            {
            }
        }
        //Return Chapters ordered by Chapter-Number
        return chapters.Order().ToArray();
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.Url, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            return [];
        }

        HtmlDocument document = requestResult.htmlDocument;
        
        HtmlNode[] images = document.DocumentNode.SelectNodes("//a[contains(concat(' ',normalize-space(@class),' '),' readImg ')]/img").ToArray();
        List<string> urls = images.Select(node => node.GetAttributeValue("src", "")).ToList();
        return urls.ToArray();
    }
}