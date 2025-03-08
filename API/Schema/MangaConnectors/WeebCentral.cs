using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Weebcentral : MangaConnector
{
    private readonly string[] _filterWords =
        { "a", "the", "of", "as", "to", "no", "for", "on", "with", "be", "and", "in", "wa", "at", "be", "ni" };

    public Weebcentral() : base("Weebcentral", ["en"], ["https://weebcentral.com"], "https://weebcentral.com/favicon.ico")
    {
        downloadClient = new ChromiumDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        const int limit = 32; //How many values we want returned at once
        var offset = 0; //"Page"
        var requestUrl =
            $"{BaseUris[0]}/search/data?limit={limit}&offset={offset}&text={publicationTitle}&sort=Best+Match&order=Ascending&official=Any&display_mode=Minimal%20Display";
        var requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument == null)
        {
            return [];
        }

        var publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        
        return publications;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectNodes("//article") == null)
            return [];

        var urls = document.DocumentNode.SelectNodes("/html/body/article/a[@class='link link-hover']")
            .Select(elem => elem.GetAttributeValue("href", "")).ToList();

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
        foreach (var url in urls)
        {
            (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
            if (manga is { })
                ret.Add(((Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?))manga);
        }

        return ret.ToArray();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/weebcentral\.com\/series\/(\w*)\/(.*)");
        var publicationId = publicationIdRex.Match(url).Groups[1].Value;

        var requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 &&
            requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        HtmlNode posterNode =
            document.DocumentNode.SelectSingleNode("//section[@class='flex items-center justify-center']/picture/img");
        string posterUrl = posterNode?.GetAttributeValue("src", "") ?? "";

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//section/h1");
        string sortName = titleNode?.InnerText ?? "Undefined";

        HtmlNode[] authorsNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Author(s): ']/span")?.ToArray() ?? [];
        List<Author> authors = authorsNodes.Select(n => new Author(n.InnerText)).ToList();

        HtmlNode[] genreNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Tags(s): ']/span")?.ToArray() ?? [];
        List<MangaTag> tags = genreNodes.Select(n => new MangaTag(n.InnerText)).ToList();

        HtmlNode statusNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Status: ']/a");
        string statusText = statusNode?.InnerText ?? "";
        MangaReleaseStatus releaseStatus = statusText.ToLower() switch
        {
            "cancelled" => MangaReleaseStatus.Cancelled,
            "hiatus" => MangaReleaseStatus.OnHiatus,
            "complete" => MangaReleaseStatus.Completed,
            "ongoing" => MangaReleaseStatus.Continuing,
            _ => MangaReleaseStatus.Unreleased
        };

        HtmlNode yearNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Released: ']/span");
        uint year = Convert.ToUInt32(yearNode?.InnerText ?? "0");

        HtmlNode descriptionNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Description']/p");
        string description = descriptionNode?.InnerText ?? "Undefined";

        HtmlNode[] altTitleNodes = document.DocumentNode
            .SelectNodes("//ul/li[strong/text() = 'Associated Name(s)']/ul/li")?.ToArray() ?? [];
        List<MangaAltTitle> altTitles = altTitleNodes.Select(n => new MangaAltTitle("", n.InnerText)).ToList();

        Manga m = new(publicationId, sortName, description, websiteUrl, posterUrl, null, year, null, releaseStatus, -1,
            this, authors, tags, [], altTitles);
        return (m, authors, tags, [], altTitles);
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://weebcentral.com/series/{publicationId}");
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
                var requestUrl = $"{BaseUris[0]}/series/{manga.MangaConnectorId}/full-chapter-list";
        var requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return [];
        var chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
                return chapters.Order().ToArray();
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        var requestResult = downloadClient.MakeRequest(chapter.Url, RequestType.Default);
        if (requestResult.htmlDocument is null)
            return [];

        var document = requestResult.htmlDocument;

        var imageNodes =
            document.DocumentNode.SelectNodes($"//section[@hx-get='{chapter.Url}/images']/img")?.ToArray() ?? [];
        var urls = imageNodes.Select(imgNode => imgNode.GetAttributeValue("src", "")).ToArray();
        
        return urls;
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        var chaptersWrapper = document.DocumentNode.SelectSingleNode("/html/body");

        Regex chapterRex = new(@"(\d+(?:\.\d+)*)");
        Regex idRex = new(@"https:\/\/weebcentral\.com\/chapters\/(\w*)");

        var ret = chaptersWrapper.Descendants("a").Select(elem =>
        {
            var url = elem.GetAttributeValue("href", "") ?? "Undefined";

            if (!url.StartsWith("https://") && !url.StartsWith("http://"))
                return new Chapter(manga, "", "");

            var idMatch = idRex.Match(url);
            var id = idMatch.Success ? idMatch.Groups[1].Value : null;

            var chapterNode = elem.SelectSingleNode("span[@class='grow flex items-center gap-2']/span")?.InnerText ??
                              "Undefined";

            var chapterNumberMatch = chapterRex.Match(chapterNode);
            var chapterNumber = chapterNumberMatch.Success ? chapterNumberMatch.Groups[1].Value : "-1";

            return new Chapter(manga, url, chapterNumber);
        }).Where(elem => elem.ChapterNumber != String.Empty && elem.Url != string.Empty).ToList();

        ret.Reverse();
        return ret;
    }
}