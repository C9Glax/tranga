using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Soenneker.Utils.String.NeedlemanWunsch;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Weebcentral : MangaConnector
{
    private readonly string[] _filterWords =
        { "a", "the", "of", "as", "to", "no", "for", "on", "with", "be", "and", "in", "wa", "at", "be", "ni" };

    private readonly string baseURL = "https://weebcentral.com";

    public Weebcentral(GlobalBase clone) : base(clone, "Weebcentral", ["en"])
    {
        downloadClient = new ChromiumDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        const int limit = 32; //How many values we want returned at once
        var offset = 0; //"Page"
        var requestUrl =
            $"{baseURL}/search/data?limit={limit}&offset={offset}&text={publicationTitle}&sort=Best+Match&order=Ascending&official=Any&display_mode=Minimal%20Display";
        var requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument == null)
        {
            Log($"Failed to retrieve search: {requestResult.statusCode}");
            return [];
        }

        var publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    /*
     * Title: document.DocumentNode.SelectNodes("/html/body/article/section/div/a[@class='link link-hover']").Select(elem => elem.InnerText).ToList()
     * URL: document.DocumentNode.SelectNodes("/html/body/article/a[@class='link link-hover']").Select(elem => elem.GetAttributeValue("href", "")).ToList()
     */

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectNodes("//article") == null)
            return Array.Empty<Manga>();

        var urls = document.DocumentNode.SelectNodes("/html/body/article/a[@class='link link-hover']")
            .Select(elem => elem.GetAttributeValue("href", "")).ToList();

        HashSet<Manga> ret = new();
        foreach (var url in urls)
        {
            var manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add((Manga)manga);
        }

        return ret.ToArray();
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/weebcentral\.com\/series\/(\w*)\/(.*)");
        var publicationId = publicationIdRex.Match(url).Groups[1].Value;

        var requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 &&
            requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        var posterNode =
            document.DocumentNode.SelectSingleNode("//section[@class='flex items-center justify-center']/picture/img");
        var posterUrl = posterNode?.GetAttributeValue("src", "") ?? "";
        var coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId, RequestType.MangaCover);

        var titleNode = document.DocumentNode.SelectSingleNode("//section/h1");
        var sortName = titleNode?.InnerText ?? "Undefined";

        HtmlNode[] authorsNodes =
            document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Author(s): ']/span")?.ToArray() ?? [];
        var authors = authorsNodes.Select(n => n.InnerText).ToList();

        HtmlNode[] genreNodes = document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Tags(s): ']/span")?.ToArray() ?? [];
        HashSet<string> tags = genreNodes.Select(n => n.InnerText).ToHashSet();
        
        HtmlNode statusNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Status: ']/a");
        var status = statusNode?.InnerText ?? "";
        Log("unable to parse status");
        var releaseStatus = Manga.ReleaseStatusByte.Unreleased;
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "complete": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
        }

        var yearNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Released: ']/span");
        var year = Convert.ToInt32(yearNode?.InnerText ?? "0");

        var descriptionNode = document.DocumentNode.SelectSingleNode("//ul/li[strong/text() = 'Description']/p");
        var description = descriptionNode?.InnerText ?? "Undefined";
        
        HtmlNode[] altTitleNodes = document.DocumentNode.SelectNodes("//ul/li[strong/text() = 'Associated Name(s)']/ul/li")?.ToArray() ?? [];
        Dictionary<string, string> altTitles = new(), links = new();
        for(int i = 0; i < altTitleNodes.Length; i++)
            altTitles.Add(i.ToString(), altTitleNodes[i].InnerText);

        var originalLanguage = "";
        
        Manga manga = new(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl,
            coverFileNameInCache, links,
            year, originalLanguage, publicationId, releaseStatus, websiteUrl);
        AddMangaToCache(manga);
        return manga;
    }

    private string ToFilteredString(string input)
    {
        return string.Join(' ', input.ToLower().Split(' ').Where(word => _filterWords.Contains(word) == false));
    }

    private SearchResult[] FilteredResults(string publicationTitle, SearchResult[] unfilteredSearchResults)
    {
        Dictionary<SearchResult, int> similarity = new();
        foreach (var sr in unfilteredSearchResults)
        {
            List<int> scores = new();
            var filteredPublicationString = ToFilteredString(publicationTitle);
            var filteredSString = ToFilteredString(sr.s);
            scores.Add(NeedlemanWunschStringUtil.CalculateSimilarity(filteredSString, filteredPublicationString));
            foreach (var srA in sr.a)
            {
                var filteredAString = ToFilteredString(srA);
                scores.Add(NeedlemanWunschStringUtil.CalculateSimilarity(filteredAString, filteredPublicationString));
            }

            similarity.Add(sr, scores.Sum() / scores.Count);
        }

        var ret = similarity.OrderBy(s => s.Value).Take(10).Select(s => s.Key).ToList();
        return ret.ToArray();
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://mangasee123.com/manga/{publicationId}");
    }

    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        Log($"Getting chapters {manga}");
        try
        {
            var doc = XDocument.Load($"https://mangasee123.com/rss/{manga.publicationId}.xml");
            var chapterItems = doc.Descendants("item").ToArray();
            List<Chapter> chapters = new();
            Regex chVolRex = new(@".*chapter-([0-9\.]+)(?:-index-([0-9\.]+))?.*");
            foreach (var chapter in chapterItems)
            {
                var url = chapter.Descendants("link").First().Value;
                var m = chVolRex.Match(url);
                var volumeNumber = m.Groups[2].Success ? m.Groups[2].Value : "1";
                var chapterNumber = m.Groups[1].Value;

                var chapterUrl = Regex.Replace(url, @"-page-[0-9]+(\.html)", ".html");
                chapters.Add(new Chapter(manga, "", volumeNumber, chapterNumber, chapterUrl));
            }

            //Return Chapters ordered by Chapter-Number
            Log($"Got {chapters.Count} chapters. {manga}");
            return chapters.Order().ToArray();
        }
        catch (HttpRequestException e)
        {
            Log($"Failed to load https://mangasee123.com/rss/{manga.publicationId}.xml \n\r{e}");
            return Array.Empty<Chapter>();
        }
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        var chapterParentManga = chapter.parentManga;
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        var requestResult = downloadClient.MakeRequest(chapter.url, RequestType.Default);
        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        var document = requestResult.htmlDocument;

        var gallery = document.DocumentNode.Descendants("div").First(div => div.HasClass("ImageGallery"));
        HtmlNode[] images = gallery.Descendants("img").Where(img => img.HasClass("img-fluid")).ToArray();
        List<string> urls = new();
        foreach (var galleryImage in images)
            urls.Add(galleryImage.GetAttributeValue("src", ""));

        return DownloadChapterImages(urls.ToArray(), chapter, RequestType.MangaImage, progressToken: progressToken);
    }

    private struct SearchResult
    {
        public string i { get; set; }
        public string s { get; set; }
        public string[] a { get; set; }
    }
}