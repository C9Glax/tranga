using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JobQueue;
using Microsoft.Extensions.Logging;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class MangaLife : MangaConnector
{
    public MangaLife(GlobalBase clone) : base(clone, "Manga4Life")
    {
        this.downloadClient = new ChromiumDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        logger?.LogInformation($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = WebUtility.UrlEncode(publicationTitle);
        string requestUrl = $"https://manga4life.com/search/?name={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Manga>();

        if (requestResult.htmlDocument is null)
            return Array.Empty<Manga>();
        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        logger?.LogDebug($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://manga4life.com/manga/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/(www\.)?manga4life.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[2].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if(requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId);
        return null;
    }

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        HtmlNode resultsNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']/div[last()]/div[1]/div");
        if (resultsNode.Descendants("div").Count() == 1 && resultsNode.Descendants("div").First().HasClass("NoResults"))
        {
            logger?.LogError("No results.");
            return Array.Empty<Manga>();
        }
        logger?.LogDebug($"{resultsNode.SelectNodes("div").Count} items.");

        HashSet<Manga> ret = new();

        foreach (HtmlNode resultNode in resultsNode.SelectNodes("div"))
        {
            string url = resultNode.Descendants().First(d => d.HasClass("SeriesName")).GetAttributeValue("href", "");
            Manga? manga = GetMangaFromUrl($"https://manga4life.com{url}");
            if (manga is not null)
                ret.Add((Manga)manga);
        }
        
        return ret.ToArray();
    }


    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, RequestType.MangaCover);

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//h1");
        string sortName = titleNode.InnerText;

        HtmlNode[] authorsNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Author(s):']/..").Descendants("a")
            .ToArray();
        List<string> authors = new();
        foreach (HtmlNode authorNode in authorsNodes)
            authors.Add(authorNode.InnerText);

        HtmlNode[] genreNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Genre(s):']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode genreNode in genreNodes)
            tags.Add(genreNode.InnerText);

        HtmlNode yearNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Released:']/..").Descendants("a")
            .First();
        int year = Convert.ToInt32(yearNode.InnerText);

        HtmlNode[] statusNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Status:']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode statusNode in statusNodes)
            if (statusNode.InnerText.Contains("publish", StringComparison.CurrentCultureIgnoreCase))
                status = statusNode.InnerText.Split(' ')[0];
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "discontinued": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "complete": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Description:']/..")
            .Descendants("div").First();
        string description = descriptionNode.InnerText;

        Manga manga = new(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl,
            coverFileNameInCache, links, year, originalLanguage, status, publicationId, releaseStatus);
        cachedPublications.Add(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        logger?.LogDebug($"Getting chapters {manga}");
        RequestResult result = downloadClient.MakeRequest($"https://manga4life.com/manga/{manga.publicationId}", RequestType.Default, clickButton:"[class*='ShowAllChapters']");
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
            chapters.Add(new Chapter(manga, "", volumeNumber, chapterNumber, fullUrl));
        }
        //Return Chapters ordered by Chapter-Number
        logger?.LogInformation($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.CancellationTokenSource.IsCancellationRequested ?? false)
        {
            progressToken.Value.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        if (progressToken?.CancellationTokenSource.IsCancellationRequested ?? false)
        {
            progressToken.Value.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        logger?.LogInformation($"Retrieving chapter-info {chapter} {chapterParentManga}");

        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.url, RequestType.Default);
        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        HtmlDocument document = requestResult.htmlDocument;
        
        HtmlNode gallery = document.DocumentNode.Descendants("div").First(div => div.HasClass("ImageGallery"));
        HtmlNode[] images = gallery.Descendants("img").Where(img => img.HasClass("img-fluid")).ToArray();
        List<string> urls = new();
        foreach(HtmlNode galleryImage in images)
            urls.Add(galleryImage.GetAttributeValue("src", ""));
            
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), RequestType.MangaImage, comicInfoPath, progressToken:progressToken);
    }
}