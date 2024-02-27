using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using JobQueue;
using Newtonsoft.Json;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Mangasee : MangaConnector
{
    public Mangasee(GlobalBase clone) : base(clone, "Mangasee")
    {
        this.downloadClient = new ChromiumDownloadClient(clone);
    }

    private struct SearchResult
    {
        public string i { get; set; }
        public string s { get; set; }
        public string[] a { get; set; }
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string requestUrl = "https://mangasee123.com/_search.php";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"Failed to retrieve search: {requestResult.statusCode}");
            return Array.Empty<Manga>();
        }
        
        try
        {
            SearchResult[] searchResults = JsonConvert.DeserializeObject<SearchResult[]>(requestResult.htmlDocument!.DocumentNode.InnerText) ??
                                           throw new NoNullAllowedException();
            SearchResult[] filteredResults = FilteredResults(publicationTitle, searchResults);
            Log($"Total available manga: {searchResults.Length} Filtered down to: {filteredResults.Length}");
            
            /*
            Dictionary<SearchResult, int> levenshteinRelation = filteredResults.ToDictionary(result => result,
                result =>
                {
                    Log($"Levenshtein {result.s}");
                    return LevenshteinDistance(publicationTitle.Replace(" ", "").ToLower(), result.s.Replace(" ", "").ToLower());
                });
            Log($"After levenshtein: {levenshteinRelation.Count}");*/

            string[] urls = filteredResults.Select(result => $"https://mangasee123.com/manga/{result.i}").ToArray();
            List<Manga> searchResultManga = new();
            foreach (string url in urls)
            {
                Manga? newManga = GetMangaFromUrl(url);
                if(newManga is { } manga)
                    searchResultManga.Add(manga);
            }
            Log($"Retrieved {searchResultManga.Count} publications. Term=\"{publicationTitle}\"");
            return searchResultManga.ToArray();
        }
        catch (NoNullAllowedException)
        {
            Log("Failed to retrieve search");
            return Array.Empty<Manga>();
        }
    }

    private SearchResult[] FilteredResults(string publicationTitle, SearchResult[] unfilteredSearchResults)
    {
        string[] bannedStrings = {"a", "the", "of", "as", "to", "no", "for", "on", "with", "be", "and", "in", "wa", "at"};
        string[] cleanSplitPublicationTitle = publicationTitle.Split(' ')
            .Where(part => part.Length > 0 && !bannedStrings.Contains(part.ToLower())).ToArray();
        
        return unfilteredSearchResults.Where(usr =>
        {
            string cleanSearchResultString = string.Join(' ', usr.s.Split(' ').Where(part => part.Length > 0 && !bannedStrings.Contains(part.ToLower())));
            foreach(string splitPublicationTitlePart in cleanSplitPublicationTitle)
                if (cleanSearchResultString.Contains(splitPublicationTitlePart, StringComparison.InvariantCultureIgnoreCase) ||
                    cleanSearchResultString.Contains(splitPublicationTitlePart, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            return false;
        }).ToArray();
    }

    private int LevenshteinDistance(string a, string b)
    {
        if (b.Length == 0)
            return a.Length;
        if (a.Length == 0)
            return b.Length;
        if (a[0] == b[0])
            return LevenshteinDistance(a[1..], b[1..]);

        int case1 = LevenshteinDistance(a, b[1..]);
        int case2 = LevenshteinDistance(a[1..], b[1..]);
        int case3 = LevenshteinDistance(a[1..], b);

        if (case1 < case2)
        {
            return 1 + (case1 < case3 ? case1 : case3);
        }
        else
        {
            return 1 + (case2 < case3 ? case2 : case3);
        }
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://mangasee123.com/manga/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/mangasee123.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 && requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId);
        return null;
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, RequestType.MangaCover);

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//h1");
        string sortName = titleNode.InnerText;

        HtmlNode[] authorsNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Author(s):']/..").Descendants("a")
            .ToArray();
        List<string> authors = new();
        foreach (HtmlNode authorNode in authorsNodes)
            authors.Add(authorNode.InnerText);

        HtmlNode[] genreNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Genre(s):']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode genreNode in genreNodes)
            tags.Add(genreNode.InnerText);

        HtmlNode yearNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Released:']/..").Descendants("a")
            .First();
        int year = Convert.ToInt32(yearNode.InnerText);

        HtmlNode[] statusNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Status:']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode statusNode in statusNodes)
            if (statusNode.InnerText.Contains("publish", StringComparison.CurrentCultureIgnoreCase))
                status = statusNode.InnerText.Split(' ')[0];
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "discontinued": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "complete": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Description:']/..")
            .Descendants("div").First();
        string description = descriptionNode.InnerText;

        Manga manga = new(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl,
            coverFileNameInCache, links,
            year, originalLanguage, status, publicationId, releaseStatus);
        cachedPublications.Add(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        try
        {
            XDocument doc = XDocument.Load($"https://mangasee123.com/rss/{manga.publicationId}.xml");
            XElement[] chapterItems = doc.Descendants("item").ToArray();
            List<Chapter> chapters = new();
            Regex chVolRex = new(@".*chapter-([0-9\.]+)(?:-index-([0-9\.]+))?.*");
            foreach (XElement chapter in chapterItems)
            {
                string url = chapter.Descendants("link").First().Value;
                Match m = chVolRex.Match(url);
                string? volumeNumber = m.Groups[2].Success ? m.Groups[2].Value : "1";
                string chapterNumber = m.Groups[1].Value;

                url = string.Concat(Regex.Match(url, @"(.*)-page-[0-9]+(\.html)").Groups.Values.Select(v => v.Value));
                chapters.Add(new Chapter(manga, "", volumeNumber, chapterNumber, url));
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
        if (progressToken?.CancellationTokenSource.IsCancellationRequested ?? false)
        {
            progressToken.Value.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        if (progressToken?.CancellationTokenSource.IsCancellationRequested ?? false)
        {
            progressToken.Value.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.url, RequestType.Default);
        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        HtmlDocument document = requestResult.htmlDocument;
        
        HtmlNode gallery = document.DocumentNode.Descendants("div").First(div => div.HasClass("ImageGallery"));
        HtmlNode[] images = gallery.Descendants("img").Where(img => img.HasClass("img-fluid")).ToArray();
        List<string> urls = new();
        foreach(HtmlNode galleryImage in images)
            urls.Add(galleryImage.GetAttributeValue("src", ""));
            
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), RequestType.MangaImage, comicInfoPath, progressToken:progressToken);
    }
}