using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;
using Soenneker.Utils.String.NeedlemanWunsch;

namespace API.Schema.MangaConnectors;

public class Weebcentral : MangaConnector
{
    private readonly string _baseUrl = "https://weebcentral.com";

    private readonly string[] _filterWords =
        { "a", "the", "of", "as", "to", "no", "for", "on", "with", "be", "and", "in", "wa", "at", "be", "ni" };

    public Weebcentral() : base("Weebcentral", ["en"], ["https://weebcentral.com"])
    {
        downloadClient = new ChromiumDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(
        string publicationTitle = "")
    {
        const int limit = 32; //How many values we want returned at once
        int offset = 0; //"Page"
        string requestUrl =
            $"{_baseUrl}/search/data?limit={limit}&offset={offset}&text={publicationTitle}&sort=Best+Match&order=Ascending&official=Any&display_mode=Minimal%20Display";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument == null)
            return [];

        (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)[] publications =
            ParsePublicationsFromHtml(requestResult.htmlDocument);

        return publications;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(
        HtmlDocument document)
    {
        if (document.DocumentNode.SelectNodes("//article") == null)
            return [];

        List<string> urls = document.DocumentNode.SelectNodes("/html/body/article/a[@class='link link-hover']")
            .Select(elem => elem.GetAttributeValue("href", "")).ToList();

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
        foreach (string url in urls)
        {
            (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)? manga = GetMangaFromUrl(url);
            if (manga is { } x)
                ret.Add(x);
        }

        return ret.ToArray();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)?
        GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/weebcentral\.com\/series\/(\w*)\/(.*)");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 &&
            requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(
        HtmlDocument document, string publicationId, string websiteUrl)
    {
        HtmlNode? posterNode =
            document.DocumentNode.SelectSingleNode("//section[@class='flex items-center justify-center']/picture/img");
        string coverUrl = posterNode?.GetAttributeValue("src", "") ?? "";

        HtmlNode? titleNode = document.DocumentNode.SelectSingleNode("//section/h1");
        string sortName = titleNode?.InnerText ?? "Undefined";

        HtmlNode[] authorsNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Author(s): ']/span")?.ToArray() ?? [];
        List<string> authorNames = authorsNodes.Select(n => n.InnerText).ToList();
        List<Author> authors = authorNames.Select(n => new Author(n)).ToList();

        HtmlNode[] genreNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Tags(s): ']/span")?.ToArray() ?? [];
        HashSet<string> tags = genreNodes.Select(n => n.InnerText).ToHashSet();
        List<MangaTag> mangaTags = tags.Select(t => new MangaTag(t)).ToList();

        HtmlNode? statusNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Status: ']/a");
        string status = statusNode?.InnerText ?? "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "hiatus": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "complete": releaseStatus = MangaReleaseStatus.Completed; break;
            case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        HtmlNode? yearNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Released: ']/span");
        uint year = uint.Parse(yearNode?.InnerText ?? "0");

        HtmlNode? descriptionNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Description']/p");
        string description = descriptionNode?.InnerText ?? "Undefined";

        HtmlNode[] altTitleNodes = document.DocumentNode
            .SelectNodes("//ul/li[strong/text() = 'Associated Name(s)']/ul/li")?.ToArray() ?? [];
        Dictionary<string, string> altTitlesDict = new(), links = new();
        for (int i = 0; i < altTitleNodes.Length; i++)
            altTitlesDict.Add(i.ToString(), altTitleNodes[i].InnerText);
        List<MangaAltTitle> altTitles = altTitlesDict.Select(a => new MangaAltTitle(a.Key, a.Value)).ToList();

        string originalLanguage = "";

        Manga manga = new(publicationId, sortName, description, websiteUrl, coverUrl, null, year,
            originalLanguage, releaseStatus, -1,
            this,
            authors,
            mangaTags,
            [],
            altTitles);

        return (manga, authors, mangaTags, [], altTitles);
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(
        string publicationId)
    {
        return GetMangaFromUrl($"https://weebcentral.com/series/{publicationId}");
    }

    private string ToFilteredString(string input)
    {
        return string.Join(' ', input.ToLower().Split(' ').Where(word => _filterWords.Contains(word) == false));
    }

    private SearchResult[] FilteredResults(string publicationTitle, SearchResult[] unfilteredSearchResults)
    {
        Dictionary<SearchResult, int> similarity = new();
        foreach (SearchResult sr in unfilteredSearchResults)
        {
            List<int> scores = new();
            string filteredPublicationString = ToFilteredString(publicationTitle);
            string filteredSString = ToFilteredString(sr.s);
            scores.Add(NeedlemanWunschStringUtil.CalculateSimilarity(filteredSString, filteredPublicationString));
            foreach (string srA in sr.a)
            {
                string filteredAString = ToFilteredString(srA);
                scores.Add(NeedlemanWunschStringUtil.CalculateSimilarity(filteredAString, filteredPublicationString));
            }

            similarity.Add(sr, scores.Sum() / scores.Count);
        }

        List<SearchResult> ret = similarity.OrderBy(s => s.Value).Take(10).Select(s => s.Key).ToList();
        return ret.ToArray();
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        string requestUrl = $"{_baseUrl}/series/{manga.ConnectorId}/full-chapter-list";
        RequestResult requestResult =
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
        HtmlNode? chaptersWrapper = document.DocumentNode.SelectSingleNode("/html/body");

        Regex chapterRex = new(@"(\d+(?:\.\d+)*)");
        Regex idRex = new(@"https:\/\/weebcentral\.com\/chapters\/(\w*)");

        List<Chapter> ret = chaptersWrapper.Descendants("a").Select(elem =>
        {
            string url = elem.GetAttributeValue("href", "") ?? "Undefined";

            if (!url.StartsWith("https://") && !url.StartsWith("http://"))
                return new Chapter(manga, "undefined", "-1");

            Match idMatch = idRex.Match(url);
            string? id = idMatch.Success ? idMatch.Groups[1].Value : null;

            string chapterNode = elem.SelectSingleNode("span[@class='grow flex items-center gap-2']/span")?.InnerText ??
                                 "Undefined";

            Match chapterNumberMatch = chapterRex.Match(chapterNode);

            if (!chapterNumberMatch.Success)
                return new Chapter(manga, "undefined", "-1");

            string chapterNumber = chapterNumberMatch.Groups[1].Value;
            return new Chapter(manga, url, chapterNumber);
        }).Where(elem => elem.ChapterNumber.CompareTo("-1") != 0 && elem.Url != "undefined").ToList();

        ret.Reverse();
        return ret;
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        RequestResult requestResult = downloadClient.MakeRequest(chapter.Url, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument is null) return [];

        HtmlDocument? document = requestResult.htmlDocument;

        HtmlNode[] imageNodes =
            document.DocumentNode.SelectNodes($"//section[@hx-get='{chapter.Url}/images']/img")?.ToArray() ?? [];
        string[] urls = imageNodes.Select(imgNode => imgNode.GetAttributeValue("src", "")).ToArray();
        return urls;
    }

    private struct SearchResult
    {
        public string i { get; set; }
        public string s { get; set; }
        public string[] a { get; set; }
    }
}