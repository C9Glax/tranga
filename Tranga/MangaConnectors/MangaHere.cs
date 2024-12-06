using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class MangaHere : MangaConnector
{
    //["en"], ["www.mangahere.cc"]
    public MangaHere(string mangaConnectorName) : base(mangaConnectorName, new ChromiumDownloadClient())
    {
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
    {
        log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join('+', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://www.mangahere.cc/search?title={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            return [];
        
        (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        log.Info($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),' container ')]").Any(node => node.ChildNodes.Any(cNode => cNode.HasClass("search-keywords"))))
            return [];
        
        List<string> urls = document.DocumentNode
            .SelectNodes("//a[contains(@href, '/manga/') and not(contains(@href, '.html'))]")
            .Select(thumb => $"https://www.mangahere.cc{thumb.GetAttributeValue("href", "")}").Distinct().ToList();

        List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();
        foreach (string url in urls)
        {
            (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add(((Manga, Author[], MangaTag[], Link[], MangaAltTitle[]))manga);
        }

        return ret.ToArray();
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://www.mangahere.cc/manga/{publicationId}");
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
            return null;
        
        Regex idRex = new (@"https:\/\/www\.mangahere\.[a-z]{0,63}\/manga\/([0-9A-z\-]+).*");
        string id = idRex.Match(url).Groups[1].Value;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, id, url);
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        //We dont get posters, because same origin bs HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//img[contains(concat(' ',normalize-space(@class),' '),' detail-info-cover-img ')]");
        string posterUrl = "http://static.mangahere.cc/v20230914/mangahere/images/nopicture.jpg";

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//span[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-title-font ')]");
        string sortName = titleNode.InnerText;
        
        List<Author> authors = document.DocumentNode
            .SelectNodes("//p[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-say ')]/a")
            .Select(node => new Author(node.InnerText))
            .ToList();

        List<MangaTag> tags = document.DocumentNode
            .SelectNodes("//p[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-tag-list ')]/a")
            .Select(node => new MangaTag(node.InnerText))
            .ToList();

        string status = document.DocumentNode.SelectSingleNode("//span[contains(concat(' ',normalize-space(@class),' '),' detail-info-right-title-tip ')]").InnerText;
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

        Manga manga = new(MangaConnectorName, sortName, description, posterUrl, null, 0,
            originalLanguage, releaseStatus, 0, null, null, 
            publicationId, 
            authors.Select(a => a.AuthorId).ToArray(), 
            tags.Select(t => t.Tag).ToArray(),
            [], []);

        return (manga, authors.ToArray(), tags.ToArray(), [], []);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        log.Info($"Getting chapters {manga}");
        string requestUrl = $"https://www.mangahere.cc/manga/{manga.ConnectorId}";
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

            string volumeNumber = rexMatch.Groups[1].Value == "TBD" ? "0" : rexMatch.Groups[1].Value;
            string chapterNumber = rexMatch.Groups[2].Value;
            if (!float.TryParse(volumeNumber, NumberFormatDecimalPoint, out float volNum))
            {
                log.Debug($"Failed parsing {volumeNumber} as float.");
                continue;
            }
            if (!float.TryParse(chapterNumber, NumberFormatDecimalPoint, out float chNum))
            {
                log.Debug($"Failed parsing {chapterNumber} as float.");
                continue;
            }
            string fullUrl = $"https://www.mangahere.cc{url}";
            chapters.Add(new Chapter(manga, fullUrl, chNum, volNum));
        }
        //Return Chapters ordered by Chapter-Number
        log.Info($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    protected override string[] GetChapterImages(Chapter chapter)
    {
        Manga chapterParentManga = chapter.ParentManga;
        log.Info($"Retrieving chapter-info {chapter} {chapterParentManga}");

        List<string> imageUrls = new();

        int downloaded = 1;
        int images = 1;
        string url = string.Join('/', chapter.Url.Split('/')[..^1]);
        do
        {
            RequestResult requestResult =
                downloadClient.MakeRequest($"{url}/{downloaded}.html", RequestType.Default);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            {
                return [];
            }

            if (requestResult.htmlDocument is null)
            {
                return [];
            }
            
            imageUrls.AddRange(ParseImageUrlsFromHtml(requestResult.htmlDocument));

            images = requestResult.htmlDocument.DocumentNode
                .SelectNodes("//a[contains(@href, '/manga/')]")
                .MaxBy(node => node.GetAttributeValue("data-page", 0))!.GetAttributeValue("data-page", 0);
            log.Info($"MangaHere speciality: Get Image-url {downloaded}/{images}");
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