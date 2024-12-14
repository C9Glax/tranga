using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using API.MangaDownloadClients;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Soenneker.Utils.String.NeedlemanWunsch;

namespace API.Schema.MangaConnectors;

public class Mangasee : MangaConnector
{
    public Mangasee() : base("Mangasee", ["en"], ["mangasee123.com"])
    {
        this.downloadClient = new ChromiumDownloadClient();
    }

    private struct SearchResult
    {
        public string i { get; set; }
        public string s { get; set; }
        public string[] a { get; set; }
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        string requestUrl = "https://mangasee123.com/_search.php";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            return [];
        }
        
        try
        {
            SearchResult[] searchResults = JsonConvert.DeserializeObject<SearchResult[]>(requestResult.htmlDocument!.DocumentNode.InnerText) ??
                                           throw new NoNullAllowedException();
            SearchResult[] filteredResults = FilteredResults(publicationTitle, searchResults);
            

            string[] urls = filteredResults.Select(result => $"https://mangasee123.com/manga/{result.i}").ToArray();
            List<Manga> searchResultManga = new();
            foreach (string url in urls)
            {
                Manga? newManga = GetMangaFromUrl(url);
                if(newManga is { } manga)
                    searchResultManga.Add(manga);
            }
            return searchResultManga.ToArray();
        }
        catch (NoNullAllowedException)
        {
            return [];
        }
    }

    private readonly string[] _filterWords = {"a", "the", "of", "as", "to", "no", "for", "on", "with", "be", "and", "in", "wa", "at", "be", "ni"};
    private string ToFilteredString(string input) => string.Join(' ', input.ToLower().Split(' ').Where(word => _filterWords.Contains(word) == false));
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
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");

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

        Manga manga = //TODO
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        try
        {
            XDocument doc = XDocument.Load($"https://mangasee123.com/rss/{manga.MangaId}.xml");
            XElement[] chapterItems = doc.Descendants("item").ToArray();
            List<Chapter> chapters = new();
            Regex chVolRex = new(@".*chapter-([0-9\.]+)(?:-index-([0-9\.]+))?.*");
            foreach (XElement chapter in chapterItems)
            {
                string url = chapter.Descendants("link").First().Value;
                Match m = chVolRex.Match(url);
                float? volumeNumber = m.Groups[2].Success ? float.Parse(m.Groups[2].Value) : null;
                float chapterNumber = float.Parse(m.Groups[1].Value);

                string chapterUrl = Regex.Replace(url, @"-page-[0-9]+(\.html)", ".html");
                try
                {
                    chapters.Add(new Chapter(manga, chapterUrl,chapterNumber, volumeNumber, null));
                }
                catch (Exception e)
                {
                }
            }

            //Return Chapters ordered by Chapter-Number
            return chapters.Order().ToArray();
        }
        catch (HttpRequestException e)
        {
            return Array.Empty<Chapter>();
        }
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