using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Manganato : MangaConnector
{
    public Manganato(GlobalBase clone) : base(clone, "Manganato", ["en"])
    {
        this.downloadClient = new HttpDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://manganato.gg/search/story/{sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
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
        List<HtmlNode> searchResults = document.DocumentNode.Descendants("div").Where(n => n.HasClass("story_item")).ToList();
        Log($"{searchResults.Count} items.");
        List<string> urls = new();
        foreach (HtmlNode mangaResult in searchResults)
        {
            try
            {
            urls.Add(mangaResult.Descendants("h3").First(n => n.HasClass("story_name"))
                .Descendants("a").First().GetAttributeValue("href", ""));
            } catch
            {
                //failed to get a url, send it to the void
            }
        }

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
        return GetMangaFromUrl($"https://chapmanganato.com/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        
        if (requestResult.htmlDocument is null)
            return null;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        Dictionary<string, string> altTitles = new();
        Dictionary<string, string>? links = null;
        HashSet<string> tags = new();
        string[] authors = Array.Empty<string>();
        string originalLanguage = "";
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;

        HtmlNode infoNode = document.DocumentNode.Descendants("ul").First(d => d.HasClass("manga-info-text"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        foreach (HtmlNode li in infoNode.Descendants("li"))
        {
            string text = li.InnerText.Trim().ToLower();
            
            if (text.StartsWith("author(s) :"))
            {
                authors = li.Descendants("a").Select(a => a.InnerText.Trim()).ToArray();
            }
            else if (text.StartsWith("status :"))
            {
                string status = text.Replace("status :", "").Trim().ToLower();
                if (status == "")
                    releaseStatus = Manga.ReleaseStatusByte.Continuing;
                else if (status == "ongoing")
                    releaseStatus = Manga.ReleaseStatusByte.Continuing;
                else
                    releaseStatus = Enum.Parse<Manga.ReleaseStatusByte>(status, true);
            }
            else if (li.HasClass("genres"))
            {
                tags = li.Descendants("a").Select(a => a.InnerText.Trim()).ToHashSet();
            }
        }

        string posterUrl = document.DocumentNode.Descendants("div").First(s => s.HasClass("manga-info-pic")).Descendants("img").First()
            .GetAttributes().First(a => a.Name == "src").Value;

        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId, RequestType.MangaCover, "https://www.manganato.gg/");

        string description = document.DocumentNode.SelectSingleNode("//div[@id='contentBox']")
            .InnerText.Replace("Description :", "");
        while (description.StartsWith('\n'))
            description = description.Substring(1);
        
        string pattern = "MMM-dd-yyyy HH:mm";

        HtmlNode? oldestChapter = document.DocumentNode
            .SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),' row ')]/span[@title]").MaxBy(
                node => DateTime.ParseExact(node.GetAttributeValue("title", "Dec-31-2400 23:59"), pattern,
                    CultureInfo.InvariantCulture).Millisecond);


        int year = DateTime.ParseExact(oldestChapter?.GetAttributeValue("title", "Dec 31 2400, 23:59")??"Dec 31 2400, 23:59", pattern,
            CultureInfo.InvariantCulture).Year;
        
        Manga manga = new (sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, publicationId, releaseStatus, websiteUrl: websiteUrl);
        AddMangaToCache(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        string requestUrl = manga.websiteUrl;
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();
        
        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chapterList = document.DocumentNode.Descendants("div").First(l => l.HasClass("chapter-list"));

        Regex volRex = new(@"Vol\.([0-9]+).*");
        Regex chapterRex = new(@"https:\/\/chapmanganato.[A-z]+\/manga-[A-z0-9]+\/chapter-([0-9\.]+)");
        Regex nameRex = new(@"Chapter ([0-9]+(\.[0-9]+)*){1}:? (.*)");

        foreach (HtmlNode chapterInfo in chapterList.Descendants("div").Where(x => x.HasClass("row")))
        {
            string url = chapterInfo.Descendants("a").First().GetAttributeValue("href", "");
            var name = chapterInfo.Descendants("a").First().InnerText.Trim();
            string chapterName = nameRex.Match(name).Groups[3].Value;
            string chapterNumber = Regex.Match(name, @"Chapter ([0-9]+(\.[0-9]+)*)").Groups[1].Value;
            string? volumeNumber = Regex.Match(chapterName, @"Vol\.([0-9]+)").Groups[1].Value;
            if (string.IsNullOrWhiteSpace(volumeNumber))
                volumeNumber = "0";
            try
            {
                ret.Add(new Chapter(manga, chapterName, volumeNumber, chapterNumber, url));
            }
            catch (Exception e)
            {
                Log($"Failed to load chapter {chapterNumber}: {e.Message}");
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
        string requestUrl = chapter.url;
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
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
        
        return DownloadChapterImages(imageUrls, chapter, RequestType.MangaImage, "https://www.manganato.gg", progressToken:progressToken);
    }

    private string[] ParseImageUrlsFromHtml(HtmlDocument document)
    {
        List<string> ret = new();

        HtmlNode imageContainer =
            document.DocumentNode.Descendants("div").First(i => i.HasClass("container-chapter-reader"));
        foreach(HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}