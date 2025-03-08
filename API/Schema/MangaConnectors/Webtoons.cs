using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Webtoons : MangaConnector
{

    public Webtoons() : base("Webtoons", ["en"], ["www.webtoons.com"], "https://webtoons-static.pstatic.net/image/favicon/favicon.ico")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    // Done
    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
        string requestUrl = $"https://www.webtoons.com/en/search?keyword={sanitizedTitle}&searchType=WEBTOON";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300) {
            return [];
        }

        if (requestResult.htmlDocument is null)
        {
            return [];
        }

        (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)[] publications =
            ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    // Done
    public override (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)? GetMangaFromId(string publicationId)
    {
        PublicationManager pb = new PublicationManager(publicationId);
        return GetMangaFromUrl($"https://www.webtoons.com/en/{pb.Category}/{pb.Title}/list?title_no={pb.Id}");
    }

    // Done
    public override (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)? GetMangaFromUrl(string url)
    {
        RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300) {
            return null;
        }
        if (requestResult.htmlDocument is null)
        {
            return null;
        }
        Regex regex = new Regex(@".*webtoons\.com/en/(?<category>[^/]+)/(?<title>[^/]+)/list\?title_no=(?<id>\d+).*");
        Match match = regex.Match(url);

        if(match.Success) {
            PublicationManager pm = new PublicationManager(match.Groups["title"].Value, match.Groups["category"].Value, match.Groups["id"].Value);
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, pm.getPublicationId(), url);
        }
        return null;
    }

    // Done
    private (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        HtmlNode mangaList = document.DocumentNode.SelectSingleNode("//ul[contains(@class, 'card_lst')]");
        if (!mangaList.ChildNodes.Any(node => node.Name == "li")) {
            return [];
        }

        List<string> urls = document.DocumentNode
                            .SelectNodes("//ul[contains(@class, 'card_lst')]/li/a")
                            .Select(node => node.GetAttributeValue("href", "https://www.webtoons.com"))
                            .ToList();

        List<(Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)> ret = new();
        foreach (string url in urls)
        {
            (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>)? manga = GetMangaFromUrl(url);
            if(manga is { } m)
                ret.Add(m);
        }

        return ret.ToArray();
    }

    private string capitalizeString(string str = "") {
        if(str.Length == 0) return "";
        if(str.Length == 1) return str.ToUpper();
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    // Done
    private (Manga, List<Author>, List<MangaTag>, List<Link>, List<MangaAltTitle>) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        HtmlNode infoNode1 = document.DocumentNode.SelectSingleNode("//*[@id='content']/div[2]/div[1]/div[1]");
        HtmlNode infoNode2 = document.DocumentNode.SelectSingleNode("//*[@id='content']/div[2]/div[2]/div[2]");

        string sortName = infoNode1.SelectSingleNode(".//h1[contains(@class, 'subj')]").InnerText;
        string description = infoNode2.SelectSingleNode(".//p[contains(@class, 'summary')]")
                            .InnerText.Trim();

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'detail_body') and contains(@class, 'banner')]");

        Regex regex = new Regex(@"url\((?<url>.*?)\)");
        Match match = regex.Match(posterNode.GetAttributeValue("style", ""));

        string coverUrl = match.Groups["url"].Value;

        string genre = infoNode1.SelectSingleNode(".//h2[contains(@class, 'genre')]")
                            .InnerText.Trim();
        List<MangaTag> mangaTags = [new MangaTag(genre)];

        List<HtmlNode> authorsNodes = infoNode1.SelectSingleNode(".//div[contains(@class, 'author_area')]").Descendants("a").ToList();
        List<Author> authors = authorsNodes.Select(node => new Author(node.InnerText.Trim())).ToList();

        string originalLanguage = "";

        uint year = 0;

        string status1 = infoNode2.SelectSingleNode(".//p").InnerText;
        string status2 = infoNode2.SelectSingleNode(".//p/span").InnerText;
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;
        if(status2.Length == 0 || status1.ToLower() == "completed") {
            releaseStatus = MangaReleaseStatus.Completed;
        } else if(status2.ToLower() == "up") {
            releaseStatus = MangaReleaseStatus.Continuing;
        }

        Manga manga = new (publicationId, sortName, description, websiteUrl, coverUrl, null, year,
            originalLanguage, releaseStatus, -1,
            this, 
            authors, 
            mangaTags, 
            [],
            []);
		
        return (manga, authors, mangaTags, [], []);
    }

    // Done
    public override Chapter[] GetChapters(Manga manga, string language = "en")
    {
        PublicationManager pm = new(manga.MangaId);
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

        return chapters.Order().ToArray();
    }

    // Done
    private List<Chapter> ParseChaptersFromHtml(Manga manga, string mangaUrl)
    {
        RequestResult result = downloadClient.MakeRequest(mangaUrl, RequestType.Default);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            return new List<Chapter>();
        }

        List<Chapter> ret = new();

        foreach (HtmlNode chapterInfo in result.htmlDocument.DocumentNode.SelectNodes("//ul/li[contains(@class, '_episodeItem')]"))
        {
            HtmlNode infoNode = chapterInfo.SelectSingleNode(".//a");
            string url = infoNode.GetAttributeValue("href", "");

            string id = chapterInfo.GetAttributeValue("id", "");
            if(id == "") continue;
            string chapterNumber = chapterInfo.GetAttributeValue("data-episode-no", "");
            if(chapterNumber == "") continue;
            string chapterName = infoNode.SelectSingleNode(".//span[contains(@class, 'subj')]/span").InnerText.Trim();
            ret.Add(new Chapter(manga, url, chapterNumber, null, chapterName));
        }

        return ret;
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        string requestUrl = chapter.Url;
        // Leaving this in to check if the page exists
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            return [];
        }

        string[] imageUrls = ParseImageUrlsFromHtml(requestUrl);
        return imageUrls;
    }

    private string[] ParseImageUrlsFromHtml(string mangaUrl)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(mangaUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            return [];
        }
        if (requestResult.htmlDocument is null)
        {
            return [];
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