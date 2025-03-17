using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Manganato : MangaConnector
{
    public Manganato() : base("Manganato", ["en"],
        ["natomanga.com", "manganato.gg", "mangakakalot.gg", "nelomanga.com"],
        "https://www.manganato.gg/images/favicon-manganato.webp")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(
        string publicationTitle = "")
    {
        string sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0))
            .ToLower();
        string requestUrl = $"https://manganato.gg/search/story/{sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        if (requestResult.htmlDocument is null)
            return [];
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications =
            ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(
        HtmlDocument document)
    {
        List<HtmlNode> searchResults =
            document.DocumentNode.Descendants("div").Where(n => n.HasClass("story_item")).ToList();
        List<string> urls = new();
        foreach (HtmlNode mangaResult in searchResults)
        {
            try
            {
                urls.Add(mangaResult.Descendants("h3").First(n => n.HasClass("story_name"))
                    .Descendants("a").First().GetAttributeValue("href", ""));
            }
            catch
            {
                //failed to get a url, send it to the void
            }
        }

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
        foreach (string url in urls)
        {
            (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
            if (manga is { } m)
                ret.Add(m);
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
        RequestResult requestResult =
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
        Dictionary<string, string> altTitles = new();
        List<MangaTag> tags = new();
        List<Author> authors = new();
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode infoNode = document.DocumentNode.Descendants("ul").First(d => d.HasClass("manga-info-text"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        foreach (HtmlNode li in infoNode.Descendants("li"))
        {
            string text = li.InnerText.Trim().ToLower();

            if (text.StartsWith("author(s) :"))
            {
                authors = li.Descendants("a").Select(a => a.InnerText.Trim()).Select(a => new Author(a)).ToList();
            }
            else if (text.StartsWith("status :"))
            {
                string status = text.Replace("status :", "").Trim().ToLower();
                if (string.IsNullOrWhiteSpace(status))
                    releaseStatus = MangaReleaseStatus.Continuing;
                else if (status == "ongoing")
                    releaseStatus = MangaReleaseStatus.Continuing;
                else
                    releaseStatus = Enum.Parse<MangaReleaseStatus>(status, true);
            }
            else if (li.HasClass("genres"))
            {
                tags = li.Descendants("a").Select(a => new MangaTag(a.InnerText.Trim())).ToList();
            }
        }

        string posterUrl = document.DocumentNode.Descendants("div").First(s => s.HasClass("manga-info-pic"))
            .Descendants("img").First()
            .GetAttributes().First(a => a.Name == "src").Value;

        string description = document.DocumentNode.SelectSingleNode("//div[@id='contentBox']")
            .InnerText.Replace("Description :", "");
        while (description.StartsWith('\n'))
            description = description.Substring(1);

        string pattern = "MMM-dd-yyyy HH:mm";

        HtmlNode? oldestChapter = document.DocumentNode
            .SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),' row ')]/span[@title]").MaxBy(
                node => DateTime.ParseExact(node.GetAttributeValue("title", "Dec-31-2400 23:59"), pattern,
                    CultureInfo.InvariantCulture).Millisecond);


        uint year = Convert.ToUInt32(DateTime.ParseExact(
            oldestChapter?.GetAttributeValue("title", "Dec 31 2400, 23:59") ?? "Dec 31 2400, 23:59", pattern,
            CultureInfo.InvariantCulture).Year);

        Manga manga = new(publicationId, sortName, description, websiteUrl, posterUrl, null, year, null, releaseStatus,
            -1, this, authors, tags, [], []);
        return (manga, authors, tags, [], []);
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        string requestUrl = manga.WebsiteUrl;
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();

        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        return chapters.Order().ToArray();
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        string requestUrl = chapter.Url;
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument is null)
            return [];

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);

        return imageUrls;
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
                ret.Add(new Chapter(manga, url, chapterNumber, int.Parse(volumeNumber), chapterName));
            }
            catch (Exception e)
            {
            }
        }

        ret.Reverse();
        return ret;
    }

    private string[] ParseImageUrlsFromHtml(HtmlDocument document)
    {
        List<string> ret = new();

        HtmlNode imageContainer =
            document.DocumentNode.Descendants("div").First(i => i.HasClass("container-chapter-reader"));
        foreach (HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}