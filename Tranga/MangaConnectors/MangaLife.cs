using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class MangaLife : MangaConnector
{
    public MangaLife(GlobalBase clone) : base(clone, "MangaLife")
    {
        this.downloadClient = new ChromiumDownloadClient(clone, new Dictionary<byte, int>()
        {
            { 1, 60 }
        });
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = WebUtility.UrlEncode(publicationTitle);
        string requestUrl = $"https://manga4life.com/search/?name={sanitizedTitle}";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Manga>();

        if (requestResult.htmlDocument is null)
            return Array.Empty<Manga>();
        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/manga4life.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        DownloadClient.RequestResult requestResult = this.downloadClient.MakeRequest(url, 1);
        if(requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId);
        return null;
    }

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        HtmlNode resultsNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']/div[last()]/div[1]/div");
        if (resultsNode.Descendants("div").Count() == 1 && resultsNode.Descendants("div").First().HasClass("NoResults"))
        {
            Log("No results.");
            return Array.Empty<Manga>();
        }
        Log($"{resultsNode.SelectNodes("div").Count} items.");

        HashSet<Manga> ret = new();

        foreach (HtmlNode resultNode in resultsNode.SelectNodes("div"))
        {
            string url = resultNode.Descendants().First(d => d.HasClass("SeriesName")).GetAttributeValue("href", "");
            Manga? manga = GetMangaFromUrl($"https://mangasee123.com{url}");
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

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

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

        HtmlNode descriptionNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Description:']/..")
            .Descendants("div").First();
        string description = descriptionNode.InnerText;

        Manga manga = new(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl,
            coverFileNameInCache, links,
            year, originalLanguage, status, publicationId);
        cachedPublications.Add(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        DownloadClient.RequestResult result = downloadClient.MakeRequest($"https://mangasee123.com/rss/{manga.publicationId}.xml", 1);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
        {
            Log("Failed to load chapterinfo");
            return Array.Empty<Chapter>();
        }

        StreamReader sr = new (result.result);
        string unformattedString = sr.ReadToEnd();
        Regex urlRex = new(@"(https:\/\/manga4life.com/read-online/[A-z0-9\-]+\.html)");
        string[] urls = urlRex.Matches(unformattedString).Select(match => match.Groups[1].Value).ToArray();
        List<Chapter> chapters = new();
        foreach (string url in urls)
        {
            string volumeNumber = "1";
            string chapterNumber = Regex.Match(url, @"-chapter-([0-9\.]+)").Groups[1].ToString();
            string fullUrl = url.Replace(Regex.Match(url,"(-page-[0-9])").Value,"");
            chapters.Add(new Chapter(manga, "", volumeNumber, chapterNumber, fullUrl));
        }
        //Return Chapters ordered by Chapter-Number
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, numberFormatDecimalPoint)).ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        DownloadClient.RequestResult requestResult = this.downloadClient.MakeRequest(chapter.url, 1);
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
        
        return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), 1, comicInfoPath, progressToken:progressToken);
    }
}