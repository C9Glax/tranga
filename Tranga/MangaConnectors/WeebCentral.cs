using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Weebcentral : MangaConnector
{
    private readonly string _baseUrl = "https://weebcentral.com";

    private readonly string[] _filterWords =
        { "a", "the", "of", "as", "to", "no", "for", "on", "with", "be", "and", "in", "wa", "at", "be", "ni" };

    public Weebcentral(GlobalBase clone) : base(clone, "Weebcentral", ["en"])
    {
        downloadClient = new ChromiumDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        const int limit = 32; //How many values we want returned at once
        int offset = 0; //"Page"
        string requestUrl =
            $"{_baseUrl}/search/data?limit={limit}&offset={offset}&text={publicationTitle}&sort=Best+Match&order=Ascending&official=Any&display_mode=Minimal%20Display";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument == null)
        {
            Log($"Failed to retrieve search: {requestResult.statusCode}");
            return [];
        }

        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");

        return publications;
    }

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectNodes("//article") == null)
            return [];

        List<string> urls = document.DocumentNode.SelectNodes("/html/body/article/a[@class='link link-hover']")
            .Select(elem => elem.GetAttributeValue("href", "")).ToList();

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
        Regex publicationIdRex = new(@"https:\/\/weebcentral\.com\/series\/(\w*)\/(.*)");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 &&
            requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        HtmlNode? posterNode =
            document.DocumentNode.SelectSingleNode("//section[@class='flex items-center justify-center']/picture/img");
        string posterUrl = posterNode?.GetAttributeValue("src", "") ?? "";
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId, RequestType.MangaCover);

        HtmlNode? titleNode = document.DocumentNode.SelectSingleNode("//section/h1");
        string sortName = titleNode?.InnerText ?? "Undefined";

        HtmlNode[] authorsNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Author(s): ']/span")?.ToArray() ?? [];
        List<string> authors = authorsNodes.Select(n => n.InnerText).ToList();

        HtmlNode[] genreNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Tags(s): ']/span")?.ToArray() ?? [];
        HashSet<string> tags = genreNodes.Select(n => n.InnerText).ToHashSet();

        HtmlNode? statusNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Status: ']/a");
        string status = statusNode?.InnerText ?? "";
        Log("unable to parse status");
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "complete": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
        }

        HtmlNode? yearNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Released: ']/span");
        int year = Convert.ToInt32(yearNode?.InnerText ?? "0");

        HtmlNode? descriptionNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Description']/p");
        string description = descriptionNode?.InnerText ?? "Undefined";

        HtmlNode[] altTitleNodes = document.DocumentNode
            .SelectNodes("//ul/li[strong/text() = 'Associated Name(s)']/ul/li")?.ToArray() ?? [];
        Dictionary<string, string> altTitles = new(), links = new();
        for (int i = 0; i < altTitleNodes.Length; i++)
            altTitles.Add(i.ToString(), altTitleNodes[i].InnerText);

        string originalLanguage = "";

        Manga manga = new(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl,
            coverFileNameInCache, links,
            year, originalLanguage, publicationId, releaseStatus, websiteUrl);
        AddMangaToCache(manga);
        return manga;
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://weebcentral.com/series/{publicationId}");
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        Log($"Getting chapters {manga}");
        string requestUrl = $"{_baseUrl}/series/{manga.publicationId}/full-chapter-list";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return [];
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.OrderByDescending(c => c.name).ThenBy(c => c.volumeNumber).ThenBy(c => c.chapterNumber).ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        HtmlNode? chaptersWrapper = document.DocumentNode.SelectSingleNode("/html/body");

        Regex chapterRex = new(@"(\d+(?:\.\d+)*)");
        Regex chapterNameRex = new(@"(\w* )+");
        Regex idRex = new(@"https:\/\/weebcentral\.com\/chapters\/(\w*)");

        List<Chapter> ret = chaptersWrapper.Descendants("a").Select(elem =>
        {
            string url = elem.GetAttributeValue("href", "") ?? "Undefined";

            if (!url.StartsWith("https://") && !url.StartsWith("http://"))
                return new Chapter(manga, null, null, "-1", "undefined");

            Match idMatch = idRex.Match(url);
            string? id = idMatch.Success ? idMatch.Groups[1].Value : null;

            string chapterNode = elem.SelectSingleNode("span[@class='grow flex items-center gap-2']/span")?.InnerText ??
                                 "Undefined";

            MatchCollection chapterNumberMatch = chapterRex.Matches(chapterNode);
            string chapterNumber = chapterNumberMatch.Count > 0 ? chapterNumberMatch[^1].Groups[1].Value : "-1";
            MatchCollection chapterNameMatch = chapterNameRex.Matches(chapterNode);
            string chapterName = chapterNameMatch.Count > 0
                ? string.Join(" - ",
                    chapterNameMatch.Select(m => m.Groups[1].Value.Trim())
                        .Where(name => name.Length > 0 && !name.Equals("Chapter", StringComparison.OrdinalIgnoreCase)).ToArray()).Trim()
                : "";

            return new Chapter(manga, chapterName != "" ? chapterName : null, null, chapterNumber, url, id);
        }).Where(elem => elem.chapterNumber != -1 && elem.url != "undefined").ToList();

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
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        RequestResult requestResult = downloadClient.MakeRequest(chapter.url, RequestType.Default);
        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        HtmlDocument? document = requestResult.htmlDocument;

        HtmlNode[] imageNodes =
            document.DocumentNode.SelectNodes($"//section[@hx-get='{chapter.url}/images']/img")?.ToArray() ?? [];
        string[] urls = imageNodes.Select(imgNode => imgNode.GetAttributeValue("src", "")).ToArray();

        return DownloadChapterImages(urls, chapter, RequestType.MangaImage, progressToken: progressToken, referrer: "https://weebcentral.com/");
    }
}
