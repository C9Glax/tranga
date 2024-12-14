using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class MangaHere : MangaConnector
{
    public MangaHere() : base("MangaHere", ["en"], ["www.mangahere.cc"])
    {
        this.downloadClient = new ChromiumDownloadClient();
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = string.Join('+', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://www.mangahere.cc/search?title={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            return Array.Empty<Manga>();
        
        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),' container ')]").Any(node => node.ChildNodes.Any(cNode => cNode.HasClass("search-keywords"))))
            return Array.Empty<Manga>();
        
        List<string> urls = document.DocumentNode
            .SelectNodes("//a[contains(@href, '/manga/') and not(contains(@href, '.html'))]")
            .Select(thumb => $"https://www.mangahere.cc{thumb.GetAttributeValue("href", "")}").Distinct().ToList();

        HashSet<Manga> ret = new();
        foreach (string url in urls)
        {
            Manga? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add((Manga)manga);
        }

        return ret.ToArray();
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://www.mangahere.cc/manga/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            return null;
        
        Regex idRex = new (@"https:\/\/www\.mangahere\.[a-z]{0,63}\/manga\/([0-9A-z\-]+).*");
        string id = idRex.Match(url).Groups[1].Value;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, id, url);
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        //We dont get posters, because same origin bs HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//img[contains(concat(' ',normalize-space(@class),' '),' detail-info-cover-img ')]");
        string posterUrl = "http://static.mangahere.cc/v20230914/mangahere/images/nopicture.jpg";

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//span[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-title-font ')]");
        string sortName = titleNode.InnerText;
        
        List<string> authors = document.DocumentNode
            .SelectNodes("//p[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-say ')]/a")
            .Select(node => node.InnerText)
            .ToList();

        HashSet<string> tags = document.DocumentNode
            .SelectNodes("//p[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-tag-list ')]/a")
            .Select(node => node.InnerText)
            .ToHashSet();

        status = document.DocumentNode.SelectSingleNode("//span[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-title-tip ')]").InnerText;
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "hiatus": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "discontinued": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "complete": releaseStatus = MangaReleaseStatus.Completed; break;
            case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectSingleNode("//p[contains(concat(' ',normalize-space(@class),' '),' fullcontent ')]");
        string description = descriptionNode.InnerText;

        Manga manga =//TODO
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        string requestUrl = $"https://www.mangahere.cc/manga/{manga.MangaId}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        
        List<string> urls = requestResult.htmlDocument.DocumentNode.SelectNodes("//div[@id='list-1']/ul//li//a[contains(@href, '/manga/')]")
            .Select(node => node.GetAttributeValue("href", "")).ToList();
        Regex chapterRex = new(@".*\/manga\/[a-zA-Z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]+\/v([0-9(TBD)]+)\/c([0-9\.]+)\/.*");
        
        List<Chapter> chapters = new();
        foreach (string url in urls)
        {
            Match rexMatch = chapterRex.Match(url);

            float? volumeNumber = rexMatch.Groups[1].Value == "TBD" ? null : float.Parse(rexMatch.Groups[1].Value);
            float chapterNumber = float.Parse(rexMatch.Groups[2].Value);
            string fullUrl = $"https://www.mangahere.cc{url}";
                
            try
            {
                chapters.Add(new Chapter(manga, fullUrl, chapterNumber, volumeNumber, null));
            }
            catch (Exception e)
            {
            }
        }
        //Return Chapters ordered by Chapter-Number
        return chapters.Order().ToArray();
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        List<string> imageUrls = new();

        int downloaded = 1;
        int images = 1;
        string url = string.Join('/', chapter.Url.Split('/')[..^1]);
        do
        {
            RequestResult requestResult =
                downloadClient.MakeRequest($"{url}/{downloaded}.html", RequestType.Default);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            {
                return [];
            }
            
            imageUrls.AddRange(ParseImageUrlsFromHtml(requestResult.htmlDocument));

            images = requestResult.htmlDocument.DocumentNode
                .SelectNodes("//a[contains(@href, '/manga/')]")
                .MaxBy(node => node.GetAttributeValue("data-page", 0))!.GetAttributeValue("data-page", 0);
        } while (downloaded++ <= images);
        
        return imageUrls.ToArray();
    }

    private string[] ParseImageUrlsFromHtml(HtmlDocument document)
    {
        return document.DocumentNode
            .SelectNodes("//img[contains(concat(' ',normalize-space(@class),' '),' reader-main-img ')]")
            .Select(node =>
            {
                string url = node.GetAttributeValue("src", "");
                return url.StartsWith("//") ? $"https:{url}" : url;
            })
            .ToArray();
    }
}