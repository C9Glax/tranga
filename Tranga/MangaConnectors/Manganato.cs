using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Manganato : MangaConnector
{
    public override string name { get; }

    public Manganato(GlobalBase clone) : base(clone)
    {
        this.name = "Manganato";
        this.downloadClient = new DownloadClient(clone, new Dictionary<byte, int>()
        {
            {1, 60}
        });
    }

    public override Manga[] GetPublications(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*")).ToLower();
        string requestUrl = $"https://manganato.com/search/story/{sanitizedTitle}";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Manga>();

        Manga[] publications = ParsePublicationsFromHtml(requestResult.result);
        cachedPublications.AddRange(publications);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    private Manga[] ParsePublicationsFromHtml(Stream html)
    {
        StreamReader reader = new (html);
        string htmlString = reader.ReadToEnd();
        HtmlDocument document = new ();
        document.LoadHtml(htmlString);
        IEnumerable<HtmlNode> searchResults = document.DocumentNode.Descendants("div").Where(n => n.HasClass("search-story-item"));
        List<string> urls = new();
        foreach (HtmlNode mangaResult in searchResults)
        {
            urls.Add(mangaResult.Descendants("a").First(n => n.HasClass("item-title")).GetAttributes()
                .First(a => a.Name == "href").Value);
        }

        HashSet<Manga> ret = new();
        foreach (string url in urls)
        {
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest(url, 1);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
                return Array.Empty<Manga>();

            ret.Add(ParseSinglePublicationFromHtml(requestResult.result, url.Split('/')[^1]));
        }

        return ret.ToArray();
    }

    private Manga ParseSinglePublicationFromHtml(Stream html, string publicationId)
    {
        StreamReader reader = new (html);
        string htmlString = reader.ReadToEnd();
        HtmlDocument document = new ();
        document.LoadHtml(htmlString);
        string status = "";
        Dictionary<string, string> altTitles = new();
        Dictionary<string, string>? links = null;
        HashSet<string> tags = new();
        string[] authors = Array.Empty<string>();
        string originalLanguage = "";

        HtmlNode infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("story-info-right"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        HtmlNode infoTable = infoNode.Descendants().First(d => d.Name == "table");
        
        foreach (HtmlNode row in infoTable.Descendants("tr"))
        {
            string key = row.SelectNodes("td").First().InnerText.ToLower();
            string value = row.SelectNodes("td").Last().InnerText;
            string keySanitized = string.Concat(Regex.Matches(key, "[a-z]"));

            switch (keySanitized)
            {
                case "alternative":
                    string[] alts = value.Split(" ; ");
                    for(int i = 0; i < alts.Length; i++)
                        altTitles.Add(i.ToString(), alts[i]);
                    break;
                case "authors":
                    authors = value.Split('-');
                    break;
                case "status":
                    status = value;
                    break;
                case "genres":
                    string[] genres = value.Split(" - ");
                    tags = genres.ToHashSet();
                    break;
            }
        }

        string posterUrl = document.DocumentNode.Descendants("span").First(s => s.HasClass("info-image")).Descendants("img").First()
            .GetAttributes().First(a => a.Name == "src").Value;

        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

        string description = document.DocumentNode.Descendants("div").First(d => d.HasClass("panel-story-info-description"))
            .InnerText.Replace("Description :", "");
        while (description.StartsWith('\n'))
            description = description.Substring(1);

        string yearString = document.DocumentNode.Descendants("li").Last(li => li.HasClass("a-h")).Descendants("span")
            .First(s => s.HasClass("chapter-time")).InnerText;
        int year = Convert.ToInt32(yearString.Split(',')[^1]) + 2000;
        
        return new Manga(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, status, publicationId);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        string requestUrl = $"https://chapmanganato.com/{manga.publicationId}";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();
        
        //Return Chapters ordered by Chapter-Number
        NumberFormatInfo chapterNumberFormatInfo = new()
        {
            NumberDecimalSeparator = "."
        };
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.result);
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, Stream html)
    {
        StreamReader reader = new (html);
        string htmlString = reader.ReadToEnd();
        HtmlDocument document = new ();
        document.LoadHtml(htmlString);
        List<Chapter> ret = new();

        HtmlNode chapterList = document.DocumentNode.Descendants("ul").First(l => l.HasClass("row-content-chapter"));

        foreach (HtmlNode chapterInfo in chapterList.Descendants("li"))
        {
            string fullString = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name")).InnerText;

            string? volumeNumber = fullString.Contains("Vol.") ? fullString.Replace("Vol.", "").Split(' ')[0] : null;
            string chapterNumber = fullString.Split(':')[0].Split("Chapter ")[1].Replace('-','.');
            string chapterName = string.Concat(fullString.Split(':')[1..]);
            string url = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name"))
                .GetAttributeValue("href", "");
            ret.Add(new Chapter(manga, chapterName, volumeNumber, chapterNumber, url));
        }
        ret.Reverse();
        return ret;
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Manga chapterParentManga = chapter.parentManga;
        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");
        string requestUrl = chapter.url;
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return requestResult.statusCode;

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.result);
        
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        return DownloadChapterImages(imageUrls, chapter.GetArchiveFilePath(settings.downloadLocation), 1, comicInfoPath, "https://chapmanganato.com/", progressToken:progressToken);
    }

    private string[] ParseImageUrlsFromHtml(Stream html)
    {
        StreamReader reader = new (html);
        string htmlString = reader.ReadToEnd();
        HtmlDocument document = new ();
        document.LoadHtml(htmlString);
        List<string> ret = new();

        HtmlNode imageContainer =
            document.DocumentNode.Descendants("div").First(i => i.HasClass("container-chapter-reader"));
        foreach(HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}