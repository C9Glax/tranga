using System.Globalization;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Manganato : MangaConnector
{
    public Manganato() : base("Manganato", ["en"], ["manganato.com"])
    {
        downloadClient = new HttpDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(
        string publicationTitle = "")
    {
        var sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0))
            .ToLower();
        var requestUrl = $"https://manganato.com/search/story/{sanitizedTitle}";
        var requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument is null)
            return [];
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications =
            ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(
        HtmlDocument document)
    {
        List<HtmlNode> searchResults = document.DocumentNode.Descendants("div")
            .Where(n => n.HasClass("search-story-item")).ToList();
        List<string> urls = new();
        foreach (var mangaResult in searchResults)
            urls.Add(mangaResult.Descendants("a").First(n => n.HasClass("item-title")).GetAttributes()
                .First(a => a.Name == "href").Value);

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
        foreach (var url in urls)
        {
            (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
            if (manga is { } x)
                ret.Add(x);
        }

        return ret.ToArray();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(
        string publicationId)
    {
        return GetMangaFromUrl($"https://chapmanganato.com/{publicationId}");
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)?
        GetMangaFromUrl(string url)
    {
        var requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;

        if (requestResult.htmlDocument is null)
            return null;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(
        HtmlDocument document, string publicationId, string websiteUrl)
    {
        Dictionary<string, string> altTitlesDict = new();
        Dictionary<string, string>? links = null;
        HashSet<string> tags = new();
        string[] authorNames = [];
        var originalLanguage = "";
        var releaseStatus = MangaReleaseStatus.Unreleased;

        var infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("story-info-right"));

        var sortName = infoNode.Descendants("h1").First().InnerText;

        var infoTable = infoNode.Descendants().First(d => d.Name == "table");

        foreach (var row in infoTable.Descendants("tr"))
        {
            var key = row.SelectNodes("td").First().InnerText.ToLower();
            var value = row.SelectNodes("td").Last().InnerText;
            var keySanitized = string.Concat(Regex.Matches(key, "[a-z]"));

            switch (keySanitized)
            {
                case "alternative":
                    string[] alts = value.Split(" ; ");
                    for (var i = 0; i < alts.Length; i++)
                        altTitlesDict.Add(i.ToString(), alts[i]);
                    break;
                case "authors":
                    authorNames = value.Split('-');
                    for (var i = 0; i < authorNames.Length; i++)
                        authorNames[i] = authorNames[i].Replace("\r\n", "");
                    break;
                case "status":
                    switch (value.ToLower())
                    {
                        case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
                        case "completed": releaseStatus = MangaReleaseStatus.Completed; break;
                    }

                    break;
                case "genres":
                    string[] genres = value.Split(" - ");
                    for (var i = 0; i < genres.Length; i++)
                        genres[i] = genres[i].Replace("\r\n", "");
                    tags = genres.ToHashSet();
                    break;
            }
        }

        List<Author> authors = authorNames.Select(n => new Author(n)).ToList();
        List<MangaTag> mangaTags = tags.Select(n => new MangaTag(n)).ToList();
        List<MangaAltTitle> mangaAltTitles = altTitlesDict.Select(a => new MangaAltTitle(a.Key, a.Value)).ToList();

        var coverUrl = document.DocumentNode.Descendants("span").First(s => s.HasClass("info-image")).Descendants("img")
            .First()
            .GetAttributes().First(a => a.Name == "src").Value;

        var description = document.DocumentNode.Descendants("div")
            .First(d => d.HasClass("panel-story-info-description"))
            .InnerText.Replace("Description :", "");
        while (description.StartsWith('\n'))
            description = description.Substring(1);

        var pattern = "MMM dd,yyyy HH:mm";

        var oldestChapter = document.DocumentNode
            .SelectNodes("//span[contains(concat(' ',normalize-space(@class),' '),' chapter-time ')]").MaxBy(
                node => DateTime.ParseExact(node.GetAttributeValue("title", "Dec 31 2400, 23:59"), pattern,
                    CultureInfo.InvariantCulture).Millisecond);


        var year = (uint)DateTime.ParseExact(
            oldestChapter?.GetAttributeValue("title", "Dec 31 2400, 23:59") ?? "Dec 31 2400, 23:59", pattern,
            CultureInfo.InvariantCulture).Year;

        Manga manga = new(publicationId, sortName, description, websiteUrl, coverUrl, null, year,
            originalLanguage, releaseStatus, -1,
            this,
            authors,
            mangaTags,
            [],
            mangaAltTitles);

        return (manga, authors, mangaTags, [], mangaAltTitles);
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        var requestUrl = $"https://chapmanganato.com/{manga.MangaId}";
        var requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return [];
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        return chapters.Order().ToArray();
    }


    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        var chapterList = document.DocumentNode.Descendants("ul").First(l => l.HasClass("row-content-chapter"));

        Regex volRex = new(@"Vol\.([0-9]+).*");
        Regex chapterRex = new(@"https:\/\/chapmanganato.[A-z]+\/manga-[A-z0-9]+\/chapter-([0-9\.]+)");
        Regex nameRex = new(@"Chapter ([0-9]+(\.[0-9]+)*){1}:? (.*)");

        foreach (var chapterInfo in chapterList.Descendants("li"))
        {
            var fullString = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name")).InnerText;

            var url = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name"))
                .GetAttributeValue("href", "");

            int? volumeNumber = volRex.IsMatch(fullString)
                ? int.Parse(volRex.Match(fullString).Groups[1].Value)
                : null;

            string chapterNumber = new(chapterRex.Match(url).Groups[1].Value);
            var chapterName = nameRex.Match(fullString).Groups[3].Value;
            try
            {
                ret.Add(new Chapter(manga, url, chapterNumber, volumeNumber, chapterName));
            }
            catch (Exception e)
            {
            }
        }

        ret.Reverse();
        return ret;
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        var requestUrl = chapter.Url;
        var requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument is null)
            return [];

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);
        return imageUrls;
    }

    private string[] ParseImageUrlsFromHtml(HtmlDocument document)
    {
        List<string> ret = new();

        var imageContainer =
            document.DocumentNode.Descendants("div").First(i => i.HasClass("container-chapter-reader"));
        foreach (var imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}