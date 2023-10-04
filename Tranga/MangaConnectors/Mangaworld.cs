using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Mangaworld: MangaConnector
{
    public Mangaworld(GlobalBase clone) : base(clone, "Mangaworld")
    {
        this.downloadClient = new HttpDownloadClient(clone, new Dictionary<byte, int>()
        {
            {1, 60}
        });
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://www.mangaworld.bz/archive?keyword={sanitizedTitle}";
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

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (!document.DocumentNode.SelectSingleNode("//div[@class='comics-grid']").ChildNodes
                .Any(node => node.HasClass("entry")))
            return Array.Empty<Manga>();
        
        List<string> urls = document.DocumentNode
            .SelectNodes(
                "//div[@class='comics-grid']//div[@class='entry']//a[contains(concat(' ',normalize-space(@class),' '),'thumb')]")
            .Select(thumb => thumb.GetAttributeValue("href", "")).ToList();

        HashSet<Manga> ret = new();
        foreach (string url in urls)
        {
            Manga? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add((Manga)manga);
        }

        return ret.ToArray();
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(url, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        
        if (requestResult.htmlDocument is null)
            return null;
        
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^2]);
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId)
    {
        Dictionary<string, string> altTitles = new();
        Dictionary<string, string>? links = null;
        string originalLanguage = "";

        HtmlNode infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("info"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        HtmlNode metadata = infoNode.Descendants().First(d => d.HasClass("meta-data"));

        HtmlNode altTitlesNode = metadata.SelectSingleNode("//span[text()='Titoli alternativi: ']/..").ChildNodes[1];

        string[] alts = altTitlesNode.InnerText.Split(", ");
        for(int i = 0; i < alts.Length; i++)
            altTitles.Add(i.ToString(), alts[i]);

        HtmlNode genresNode =
            metadata.SelectSingleNode("//span[text()='Generi: ']/..");
        HashSet<string> tags = genresNode.SelectNodes("a").Select(node => node.InnerText).ToHashSet();
        
        HtmlNode authorsNode =
            metadata.SelectSingleNode("//span[text()='Autore: ']/..");
        string[] authors = new[] { authorsNode.SelectNodes("a").First().InnerText };

        string status = metadata.SelectSingleNode("//span[text()='Stato: ']/..").SelectNodes("a").First().InnerText;

        string posterUrl = document.DocumentNode.SelectSingleNode("//img[@class='rounded']").GetAttributeValue("src", "");

        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

        string description = document.DocumentNode.SelectSingleNode("//div[@id='noidungm']").InnerText;
        
        string yearString = metadata.SelectSingleNode("//span[text()='Anno di uscita: ']/..").SelectNodes("a").First().InnerText;
        int year = Convert.ToInt32(yearString);
        
        Manga manga = new (sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, status, publicationId);
        cachedPublications.Add(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        string requestUrl = $"https://www.mangaworld.bz/manga/{manga.publicationId}";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();
        
        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, numberFormatDecimalPoint)).ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chaptersWrapper =
            document.DocumentNode.SelectSingleNode(
                "//div[contains(concat(' ',normalize-space(@class),' '),'chapters-wrapper')]");

        if (chaptersWrapper.Descendants("div").Any(descendant => descendant.HasClass("volume-element")))
        {
            foreach (HtmlNode volNode in document.DocumentNode.SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),'volume-element')]"))
            {
                string volume = volNode.SelectNodes("div").First(node => node.HasClass("volume")).SelectSingleNode("p").InnerText.Split(' ')[^1];
                foreach (HtmlNode chNode in volNode.SelectNodes("div").First(node => node.HasClass("volume-chapters")).SelectNodes("div"))
                {
                    string number = chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText.Split(" ")[^1];
                    string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                    ret.Add(new Chapter(manga, null, volume, number, url));
                }
            }
        }
        else
        {
            foreach (HtmlNode chNode in chaptersWrapper.SelectNodes("div").Where(node => node.HasClass("chapter")))
            {
                string number = chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText.Split(" ")[^1];
                string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                ret.Add(new Chapter(manga, null, null, number, url));
            }
        }

        ret.Reverse();
        return ret;
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");
        string requestUrl = $"{chapter.url}?style=list";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            progressToken?.Cancel();
            return requestResult.statusCode;
        }

        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.InternalServerError;
        }

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);
        
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        return DownloadChapterImages(imageUrls, chapter.GetArchiveFilePath(settings.downloadLocation), 1, comicInfoPath, "https://www.mangaworld.bz/", progressToken:progressToken);
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