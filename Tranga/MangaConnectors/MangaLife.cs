using System.Net;
using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class MangaLife : MangaConnector
{
    //["en"], ["manga4life.com"]
    public MangaLife(string mangaConnectorName) : base(mangaConnectorName, new ChromiumDownloadClient())
    {
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
    {
        log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = WebUtility.UrlEncode(publicationTitle);
        string requestUrl = $"https://manga4life.com/search/?name={sanitizedTitle}";
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

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://manga4life.com/manga/{publicationId}");
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/(www\.)?manga4life.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[2].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if(requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        HtmlNode resultsNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']/div[last()]/div[1]/div");
        if (resultsNode.Descendants("div").Count() == 1 && resultsNode.Descendants("div").First().HasClass("NoResults"))
        {
            log.Info("No results.");
            return [];
        }
        log.Info($"{resultsNode.SelectNodes("div").Count} items.");

        HashSet<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();

        foreach (HtmlNode resultNode in resultsNode.SelectNodes("div"))
        {
            string url = resultNode.Descendants().First(d => d.HasClass("SeriesName")).GetAttributeValue("href", "");
            (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? manga = GetMangaFromUrl($"https://manga4life.com{url}");
            if (manga is not null)
                ret.Add(((Manga, Author[], MangaTag[], Link[], MangaAltTitle[]))manga);
        }
        
        return ret.ToArray();
    }


    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//h1");
        string sortName = titleNode.InnerText;

        HtmlNode[] authorsNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Author(s):']/..").Descendants("a")
            .ToArray();
        Author[] authors = authorsNodes.Select(a => new Author(a.InnerText)).ToArray();

        HtmlNode[] genreNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Genre(s):']/..").Descendants("a")
            .ToArray();
        MangaTag[] tags = genreNodes.Select(gn => new MangaTag(gn.InnerText)).ToArray();

        HtmlNode yearNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Released:']/..").Descendants("a")
            .First();
        uint year = uint.Parse(yearNode.InnerText);

        HtmlNode[] statusNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Status:']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode statusNode in statusNodes)
            if (statusNode.InnerText.Contains("publish", StringComparison.CurrentCultureIgnoreCase))
                status = statusNode.InnerText.Split(' ')[0];
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "hiatus": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "discontinued": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "complete": releaseStatus = MangaReleaseStatus.Completed; break;
            case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Description:']/..")
            .Descendants("div").First();
        string description = descriptionNode.InnerText;

        Manga manga = new(MangaConnectorName, sortName, description, posterUrl, null, year, originalLanguage,
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
        RequestResult result = downloadClient.MakeRequest($"https://manga4life.com/manga/{manga.ConnectorId}", RequestType.Default, clickButton:"[class*='ShowAllChapters']");
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            return Array.Empty<Chapter>();
        }
        
        HtmlNodeCollection chapterNodes = result.htmlDocument.DocumentNode.SelectNodes(
            "//a[contains(concat(' ',normalize-space(@class),' '),' ChapterLink ')]");
        string[] urls = chapterNodes.Select(node => node.GetAttributeValue("href", "")).ToArray();
        Regex urlRex = new (@"-chapter-([0-9\\.]+)(-index-([0-9\\.]+))?");
        
        List<Chapter> chapters = new();
        foreach (string url in urls)
        {
            Match rexMatch = urlRex.Match(url);

            string volumeNumber = "1";
            if (rexMatch.Groups[3].Value.Length > 0)
                volumeNumber = rexMatch.Groups[3].Value;
            string chapterNumber = rexMatch.Groups[1].Value;
            string fullUrl = $"https://manga4life.com{url}";
            fullUrl = fullUrl.Replace(Regex.Match(url,"(-page-[0-9])").Value,"");
            if (!float.TryParse(volumeNumber, NumberFormatDecimalPoint, out float volNum))
            {
                log.Debug($"Failed parsing {volumeNumber} as float.");
                continue;
            }
            if (!float.TryParse(chapterNumber, NumberFormatDecimalPoint, out float chNum))
            {
                log.Debug($"Failed parsing {chapterNumber} as float.");
                continue;
            }
            chapters.Add(new Chapter(manga, fullUrl, chNum, volNum));
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
        
        HtmlNode gallery = document.DocumentNode.Descendants("div").First(div => div.HasClass("ImageGallery"));
        HtmlNode[] images = gallery.Descendants("img").Where(img => img.HasClass("img-fluid")).ToArray();
        List<string> urls = new();
        foreach(HtmlNode galleryImage in images)
            urls.Add(galleryImage.GetAttributeValue("src", ""));

        return urls.ToArray();
    }
}