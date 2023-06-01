using System.Collections;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Logging;

namespace Tranga.Connectors;

public class Manganato : Connector
{
    public override string name { get; }
    
    public Manganato(string downloadLocation, string imageCachePath, Logger? logger) : base(downloadLocation, imageCachePath, logger)
    {
        this.name = "Manganato";
        this.downloadClient = new DownloadClient(new Dictionary<byte, int>()
        {
            {(byte)1, 100}
        }, logger);
    }

    public override Publication[] GetPublications(string publicationTitle = "")
    {
        string sanitizedTitle = publicationTitle.ToLower().Replace(' ', '_');
        logger?.WriteLine(this.GetType().ToString(), $"Getting Publications (title={sanitizedTitle})");
        string requestUrl = $"https://manganato.com/search/story/{sanitizedTitle}";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, (byte)1);
        if (requestResult.statusCode != HttpStatusCode.OK)
            return Array.Empty<Publication>();

        return ParsePublicationsFromHtml(requestResult.result);
    }

    private Publication[] ParsePublicationsFromHtml(Stream html)
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

        HashSet<Publication> ret = new();
        foreach (string url in urls)
        {
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest(url, (byte)1);
            if (requestResult.statusCode != HttpStatusCode.OK)
                return Array.Empty<Publication>();

            ret.Add(ParseSinglePublicationFromHtml(requestResult.result, url.Split('/')[^1]));
        }

        return ret.ToArray();
    }

    private Publication ParseSinglePublicationFromHtml(Stream html, string publicationId)
    {
        StreamReader reader = new (html);
        string htmlString = reader.ReadToEnd();
        HtmlDocument document = new ();
        document.LoadHtml(htmlString);
        string status = "";
        Dictionary<string, string> altTitles = new();
        Dictionary<string, string>? links = null;
        HashSet<string> tags = new();
        string? author = null, originalLanguage = null;
        int? year = null;

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
                    altTitles.Add("",value);
                    break;
                case "authors":
                    author = value;
                    break;
                case "status":
                    status = value;
                    break;
                case "genres":
                    string[] genres = value.Split(" - ");
                    tags = genres.ToHashSet();
                    break;
                default: break;
            }
        }

        string posterUrl = document.DocumentNode.Descendants("span").First(s => s.HasClass("info-image")).Descendants("img").First()
            .GetAttributes().First(a => a.Name == "src").Value;

        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

        string description = document.DocumentNode.Descendants("div").First(d => d.HasClass("panel-story-info-description"))
            .InnerText;
        

        return new Publication(sortName, author, description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, status, publicationId);
    }

    public override Chapter[] GetChapters(Publication publication, string language = "")
    {
        string requestUrl = $"https://manganato.com/{publication.publicationId}";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, (byte)1);
        if (requestResult.statusCode != HttpStatusCode.OK)
            return Array.Empty<Chapter>();

        return ParseChaptersFromHtml(requestResult.result);
    }

    private Chapter[] ParseChaptersFromHtml(Stream html)
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
            string? chapterNumber = fullString.Split(':')[0].Split(' ')[^1];
            string chapterName = string.Concat(fullString.Split(':')[1..]);
            string url = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name"))
                .GetAttributeValue("href", "");
            ret.Add(new Chapter(chapterName, volumeNumber, chapterNumber, url));
        }

        return ret.ToArray();
    }

    public override void DownloadChapter(Publication publication, Chapter chapter)
    {
        string requestUrl = chapter.url;
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, (byte)1);
        if (requestResult.statusCode != HttpStatusCode.OK)
            return;

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.result);
        
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, GetComicInfoXmlString(publication, chapter, logger));
        
        DownloadChapterImages(imageUrls, GetArchiveFilePath(publication, chapter), (byte)1, comicInfoPath, "https://chapmanganato.com/");
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