using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Webtoons : MangaConnector
{

    public Webtoons(GlobalBase clone) : base(clone, "Webtoons", ["en"])
    {
        this.downloadClient = new HttpDownloadClient(clone);
    }

    // Done
    public override Manga[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string requestUrl = $"https://www.webtoons.com/en/search?keyword={sanitizedTitle}&searchType=WEBTOON";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300) {
            Log($"Failed to retrieve site");
            return Array.Empty<Manga>();
        }

        if (requestResult.htmlDocument is null)
        {
            Log($"Failed to retrieve site");
            return Array.Empty<Manga>();
        }

        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    // Done
    public override Manga? GetMangaFromId(string publicationId)
    {
        PublicationManager pb = new PublicationManager(publicationId);
        return GetMangaFromUrl($"https://www.webtoons.com/en/{pb.Category}/{pb.Title}/list?title_no={pb.Id}");
    }

    // Done
    public override Manga? GetMangaFromUrl(string url)
    {
        RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300) {
            return null;
        }
        if (requestResult.htmlDocument is null)
        {
            Log($"Failed to retrieve site");
            return null;
        }
        Regex regex = new Regex(@".*webtoons\.com/en/(?<category>[^/]+)/(?<title>[^/]+)/list\?title_no=(?<id>\d+).*");
        Match match = regex.Match(url);

        if(match.Success) {
            PublicationManager pm = new PublicationManager(match.Groups["title"].Value, match.Groups["category"].Value, match.Groups["id"].Value);
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, pm.getPublicationId(), url);
        }
        Log($"Failed match Regex ID");
        return null;
    }

    // Done
    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        HtmlNode mangaList = document.DocumentNode.SelectSingleNode("//ul[contains(@class, 'card_lst')]");
        if (!mangaList.ChildNodes.Any(node => node.Name == "li")) {
            Log($"Failed to parse publication");
            return Array.Empty<Manga>();
        }

        List<string> urls = document.DocumentNode
                            .SelectNodes("//ul[contains(@class, 'card_lst')]/li/a")
                            .Select(node => node.GetAttributeValue("href", "https://www.webtoons.com"))
                            .ToList();

        HashSet<Manga> ret = new();
        foreach (string url in urls)
        {
            Manga? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add((Manga)manga);
        }

        return ret.ToArray();
    }

    private string capitalizeString(string str = "") {
        if(str.Length == 0) return "";
        if(str.Length == 1) return str.ToUpper();
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    // Done
    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        HtmlNode infoNode1 = document.DocumentNode.SelectSingleNode("//*[@id='content']/div[2]/div[1]/div[1]");
        HtmlNode infoNode2 = document.DocumentNode.SelectSingleNode("//*[@id='content']/div[2]/div[2]/div[2]");

        string sortName = infoNode1.SelectSingleNode(".//h1[contains(@class, 'subj')]").InnerText;
        string description = infoNode2.SelectSingleNode(".//p[contains(@class, 'summary')]")
                            .InnerText.Trim();

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'detail_body') and contains(@class, 'banner')]");

        Regex regex = new Regex(@"url\((?<url>.*?)\)");
        Match match = regex.Match(posterNode.GetAttributeValue("style", ""));

        string posterUrl = match.Groups["url"].Value;
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId, RequestType.MangaCover, websiteUrl);

        string genre = infoNode1.SelectSingleNode(".//h2[contains(@class, 'genre')]")
                            .InnerText.Trim();
        string[] tags = [ genre ];

        List<HtmlNode> authorsNodes = infoNode1.SelectSingleNode(".//div[contains(@class, 'author_area')]").Descendants("a").ToList();
        List<string> authors = authorsNodes.Select(node => node.InnerText.Trim()).ToList();

        string originalLanguage = "";

        int year = DateTime.Now.Year;

        string status1 = infoNode2.SelectSingleNode(".//p").InnerText;
        string status2 = infoNode2.SelectSingleNode(".//p/span").InnerText;
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;
        if(status2.Length == 0 || status1.ToLower() == "completed") {
            releaseStatus = Manga.ReleaseStatusByte.Completed;
        } else if(status2.ToLower() == "up") {
            releaseStatus = Manga.ReleaseStatusByte.Continuing;
        }

        Manga manga = new(sortName, authors, description, new Dictionary<string, string>(), tags, posterUrl, coverFileNameInCache, new Dictionary<string, string>(),
            year, originalLanguage, publicationId, releaseStatus, websiteUrl: websiteUrl);
        AddMangaToCache(manga);
        return manga;
    }

    // Done
    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        PublicationManager pm = new PublicationManager(manga.publicationId);
        string requestUrl = $"https://www.webtoons.com/en/{pm.Category}/{pm.Title}/list?title_no={pm.Id}";
        // Leaving this in for verification if the page exists
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();

        // Get number of pages
        int pages = requestResult.htmlDocument.DocumentNode
                            .SelectNodes("//div[contains(@class, 'paginate')]/a")
                            .ToList()
                            .Count;
        List<Chapter> chapters = new List<Chapter>();
        
        for(int page = 1; page <= pages; page++) {
            string pageRequestUrl = $"{requestUrl}&page={page}";
            chapters.AddRange(ParseChaptersFromHtml(manga, pageRequestUrl));
        }
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    // Done
    private List<Chapter> ParseChaptersFromHtml(Manga manga, string mangaUrl)
    {
        RequestResult result = downloadClient.MakeRequest(mangaUrl, RequestType.Default);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            Log("Failed to load site");
            return new List<Chapter>();
        }

        List<Chapter> ret = new();

        foreach (HtmlNode chapterInfo in result.htmlDocument.DocumentNode.SelectNodes("//ul/li[contains(@class, '_episodeItem')]"))
        {
            HtmlNode infoNode = chapterInfo.SelectSingleNode(".//a");
            string url = infoNode.GetAttributeValue("href", "");

            string id = chapterInfo.GetAttributeValue("id", "");
            if(id == "") continue;
            string? volumeNumber = null;
            string chapterNumber = chapterInfo.GetAttributeValue("data-episode-no", "");
            if(chapterNumber == "") continue;
            string chapterName = infoNode.SelectSingleNode(".//span[contains(@class, 'subj')]/span").InnerText.Trim();
            ret.Add(new Chapter(manga, chapterName, volumeNumber, chapterNumber, url));
        }

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
        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");
        string requestUrl = chapter.url;
        // Leaving this in to check if the page exists
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            progressToken?.Cancel();
            return requestResult.statusCode;
        }

        string[] imageUrls = ParseImageUrlsFromHtml(requestUrl);
		return DownloadChapterImages(imageUrls, chapter, RequestType.MangaImage, progressToken:progressToken, referrer: requestUrl);
    }

    private string[] ParseImageUrlsFromHtml(string mangaUrl)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(mangaUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            return Array.Empty<string>();
        }
        if (requestResult.htmlDocument is null)
        {
            Log($"Failed to retrieve site");
            return Array.Empty<string>();
        }

        return requestResult.htmlDocument.DocumentNode
        .SelectNodes("//*[@id='_imageList']/img")
        .Select(node =>
            node.GetAttributeValue("data-url", ""))
        .ToArray();
    }
}

internal class PublicationManager {
    public PublicationManager(string title = "", string category = "", string id = "") {
        this.Title = title;
        this.Category = category;
        this.Id = id;
    }

    public PublicationManager(string publicationId) {
        string[] parts = publicationId.Split("|");
        if(parts.Length == 3) {
            this.Title = parts[0];
            this.Category = parts[1];
            this.Id = parts[2];
        } else {
            this.Title = "";
            this.Category = "";
            this.Id = "";
        }
    }

    public string getPublicationId() {
        return $"{this.Title}|{this.Category}|{this.Id}";
    }

    public string Title { get; set; }
    public string Category { get; set; }
    public string Id { get; set; }
}