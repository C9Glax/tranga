using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class MangaLife : MangaConnector
{
    public MangaLife() : base("Manga4Life", ["en"], ["manga4life.com"])
    {
        this.downloadClient = new ChromiumDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = WebUtility.UrlEncode(publicationTitle);
        string requestUrl = $"https://manga4life.com/search/?name={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        if (requestResult.htmlDocument is null)
            return [];
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://manga4life.com/manga/{publicationId}");
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/(www\.)?manga4life.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[2].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if(requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        HtmlNode resultsNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']/div[last()]/div[1]/div");
        if (resultsNode.Descendants("div").Count() == 1 && resultsNode.Descendants("div").First().HasClass("NoResults"))
        {
            return [];
        }

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();

        foreach (HtmlNode resultNode in resultsNode.SelectNodes("div"))
        {
            string url = resultNode.Descendants().First(d => d.HasClass("SeriesName")).GetAttributeValue("href", "");
            (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl($"https://manga4life.com{url}");
            if (manga is { } x)
                ret.Add(x);
        }
        
        return ret.ToArray();
    }


    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string coverUrl = posterNode.GetAttributeValue("src", "");

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//h1");
        string sortName = titleNode.InnerText;

        HtmlNode[] authorsNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Author(s):']/..").Descendants("a")
            .ToArray();
        List<string> authorNames = new();
        foreach (HtmlNode authorNode in authorsNodes)
            authorNames.Add(authorNode.InnerText);
        List<Author> authors = authorNames.Select(a => new Author(a)).ToList();

        HtmlNode[] genreNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Genre(s):']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode genreNode in genreNodes)
            tags.Add(genreNode.InnerText);
        List<MangaTag> mangaTags = tags.Select(t => new MangaTag(t)).ToList();

        HtmlNode yearNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Released:']/..").Descendants("a")
            .First();
        uint year = uint.Parse(yearNode.InnerText);

        HtmlNode[] statusNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Status:']/..").Descendants("a")
            .ToArray();
        foreach (HtmlNode statusNode in statusNodes)
            if (statusNode.InnerText.Contains("publish", StringComparison.CurrentCultureIgnoreCase))
                status = statusNode.InnerText.Split(' ')[0];
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "hiatus": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "discontinued": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "complete": releaseStatus = MangaReleaseStatus.Completed; break;
            case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Description:']/..")
            .Descendants("div").First();
        string description = descriptionNode.InnerText;

        Manga manga = new (publicationId, sortName, description, websiteUrl, coverUrl, null, year,
            originalLanguage, releaseStatus, -1,
            this, 
            authors, 
            mangaTags, 
            [],
            []);
		
        return (manga, authors, mangaTags, [], []);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        RequestResult result = downloadClient.MakeRequest($"https://manga4life.com/manga/{manga.MangaId}", RequestType.Default, clickButton:"[class*='ShowAllChapters']");
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            return Array.Empty<Chapter>();
        }
        
        HtmlNodeCollection chapterNodes = result.htmlDocument.DocumentNode.SelectNodes(
            "//a[contains(concat(' ',normalize-space(@class),' '),' ChapterLink ')]");
        string[] urls = chapterNodes.Select(node => node.GetAttributeValue("href", "")).ToArray();
        Regex urlRex = new (@"-chapter-([0-9\\.]+)(-index-([0-9\\.]+))?");
        
        List<Chapter> chapters = new();
        foreach (string url in urls)
        {
            Match rexMatch = urlRex.Match(url);

            int? volumeNumber = rexMatch.Groups[3].Success && rexMatch.Groups[3].Value.Length > 0
                ? int.Parse(rexMatch.Groups[3].Value)
                : null;
            
          
            string chapterNumber = new(rexMatch.Groups[1].Value);
            string fullUrl = $"https://manga4life.com{url}";
            fullUrl = fullUrl.Replace(Regex.Match(url,"(-page-[0-9])").Value,"");
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
        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.Url, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            return [];
        }

        HtmlDocument document = requestResult.htmlDocument;
        
        HtmlNode gallery = document.DocumentNode.Descendants("div").First(div => div.HasClass("ImageGallery"));
        HtmlNode[] images = gallery.Descendants("img").Where(img => img.HasClass("img-fluid")).ToArray();
        List<string> urls = new();
        foreach(HtmlNode galleryImage in images)
            urls.Add(galleryImage.GetAttributeValue("src", ""));
        return urls.ToArray();
    }
}