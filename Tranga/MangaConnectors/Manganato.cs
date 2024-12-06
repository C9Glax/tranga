using System.Globalization;
using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class Manganato : MangaConnector
{
    //["en"], ["manganato.com"]
    public Manganato(string mangaConnectorName) : base(mangaConnectorName, new HttpDownloadClient(), "https://chapmanganato.com/")
    {
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
    {
        log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://manganato.com/search/story/{sanitizedTitle}";
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
        List<HtmlNode> searchResults = document.DocumentNode.Descendants("div").Where(n => n.HasClass("search-story-item")).ToList();
        log.Info($"{searchResults.Count} items.");
        List<string> urls = new();
        foreach (HtmlNode mangaResult in searchResults)
        {
            urls.Add(mangaResult.Descendants("a").First(n => n.HasClass("item-title")).GetAttributes()
                .First(a => a.Name == "href").Value);
        }

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
        return GetMangaFromUrl($"https://chapmanganato.com/{publicationId}");
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        
        if (requestResult.htmlDocument is null)
            return null;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        MangaAltTitle[] altTitles = [];
        MangaTag[] tags = [];
        Author[] authors = [];
        string originalLanguage = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

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
                    int i = 0;
                    altTitles = value.Split(" ; ").Select(v => new MangaAltTitle(i++.ToString(), v)).ToArray();
                    break;
                case "authors":
                    authors = value.Split('-').Select(v => new Author(v.Replace("\r\n", ""))).ToArray();
                    break;
                case "status":
                    switch (value.ToLower())
                    {
                        case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
                        case "completed": releaseStatus = MangaReleaseStatus.Completed; break;
                    }
                    break;
                case "genres":
                    tags = value.Split(" - ").Select(v => new MangaTag(v.Replace("\r\n", ""))).ToArray();
                    break;
            }
        }

        string posterUrl = document.DocumentNode.Descendants("span").First(s => s.HasClass("info-image")).Descendants("img").First()
            .GetAttributes().First(a => a.Name == "src").Value;

        string description = document.DocumentNode.Descendants("div").First(d => d.HasClass("panel-story-info-description"))
            .InnerText.Replace("Description :", "");
        while (description.StartsWith('\n'))
            description = description.Substring(1);
        
        string pattern = "MMM dd,yyyy HH:mm";

        HtmlNode? oldestChapter = document.DocumentNode
            .SelectNodes("//span[contains(concat(' ',normalize-space(@class),' '),' chapter-time ')]").MaxBy(
                node => DateTime.ParseExact(node.GetAttributeValue("title", "Dec 31 2400, 23:59"), pattern,
                    CultureInfo.InvariantCulture).Millisecond);


        uint year = (uint)DateTime.ParseExact(oldestChapter.GetAttributeValue("title", "Dec 31 2400, 23:59"), pattern,
            CultureInfo.InvariantCulture).Year;

        Manga manga = new(MangaConnectorName, sortName, description, posterUrl, null, year, originalLanguage,
            releaseStatus, 0, null, null, publicationId,
            authors.Select(a => a.AuthorId).ToArray(),
            tags.Select(t => t.Tag).ToArray(),
            [],
            altTitles.Select(a => a.AltTitleId).ToArray());
        
        return (manga, authors, tags, [], altTitles);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        log.Info($"Getting chapters {manga}");
        string requestUrl = $"https://chapmanganato.com/{manga.ConnectorId}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();
        
        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        log.Info($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chapterList = document.DocumentNode.Descendants("ul").First(l => l.HasClass("row-content-chapter"));

        Regex volRex = new(@"Vol\.([0-9]+).*");
        Regex chapterRex = new(@"https:\/\/chapmanganato.[A-z]+\/manga-[A-z0-9]+\/chapter-([0-9\.]+)");
        Regex nameRex = new(@"Chapter ([0-9]+(\.[0-9]+)*){1}:? (.*)");

        foreach (HtmlNode chapterInfo in chapterList.Descendants("li"))
        {
            string fullString = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name")).InnerText;

            string url = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name"))
                .GetAttributeValue("href", "");
            string? volumeNumber = volRex.IsMatch(fullString) ? volRex.Match(fullString).Groups[1].Value : null;
            string chapterNumber = chapterRex.Match(url).Groups[1].Value;
            string chapterName = nameRex.Match(fullString).Groups[3].Value;
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
            ret.Add(new Chapter(manga, url, chNum, volNum, chapterName));
        }
        ret.Reverse();
        return ret;
    }

    protected override string[] GetChapterImages(Chapter chapter)
    {
        Manga chapterParentManga = chapter.ParentManga;
        log.Info($"Retrieving chapter-info {chapter} {chapterParentManga}");
        string requestUrl = chapter.Url;
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            return [];
        }

        if (requestResult.htmlDocument is null)
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
            document.DocumentNode.Descendants("div").First(i => i.HasClass("container-chapter-reader"));
        foreach(HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}