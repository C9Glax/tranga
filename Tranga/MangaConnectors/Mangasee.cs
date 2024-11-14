using System.Data;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using API.Schema;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Soenneker.Utils.String.NeedlemanWunsch;

namespace Tranga.MangaConnectors;

public class Mangasee : MangaConnector
{
    //["en"], ["mangasee123.com"]
    public Mangasee(string mangaConnectorId) : base(mangaConnectorId, new ChromiumDownloadClient())
    {
    }

    private struct SearchResult
    {
        public string i { get; set; }
        public string s { get; set; }
        public string[] a { get; set; }
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
    {
        log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
        string requestUrl = "https://mangasee123.com/_search.php";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            log.Info($"Failed to retrieve search: {requestResult.statusCode}");
            return [];
        }
        
        try
        {
            SearchResult[] searchResults = JsonConvert.DeserializeObject<SearchResult[]>(requestResult.htmlDocument!.DocumentNode.InnerText) ??
                                           throw new NoNullAllowedException();
            SearchResult[] filteredResults = FilteredResults(publicationTitle, searchResults);
            log.Info($"Total available manga: {searchResults.Length} Filtered down to: {filteredResults.Length}");
            

            string[] urls = filteredResults.Select(result => $"https://mangasee123.com/manga/{result.i}").ToArray();
            List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> searchResultManga = new();
            foreach (string url in urls)
            {
                (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? newManga = GetMangaFromUrl(url);
                if(newManga is { } manga)
                    searchResultManga.Add(manga);
            }
            log.Info($"Retrieved {searchResultManga.Count} publications. Term=\"{publicationTitle}\"");
            return searchResultManga.ToArray();
        }
        catch (NoNullAllowedException)
        {
            log.Info("Failed to retrieve search");
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

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://mangasee123.com/manga/{publicationId}");
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/mangasee123.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 && requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//h1");
        string sortName = titleNode.InnerText;

        HtmlNode[] authorsNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Author(s):']/..").Descendants("a")
            .ToArray();
        Author[] authors = authorsNodes.Select(a => new Author(a.InnerText)).ToArray();

        HtmlNode[] genreNodes = document.DocumentNode
            .SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Genre(s):']/..").Descendants("a")
            .ToArray();
        MangaTag[] tags = genreNodes.Select(g => new MangaTag(g.InnerText)).ToArray();

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

        Manga manga = new(MangaConnectorId, sortName, description, posterUrl, null, year, originalLanguage,
            releaseStatus, 0, null, null, publicationId,
        authors.Select(a => a.AuthorId).ToArray(),
        tags.Select(t => t.Tag).ToArray(),
        [],
        []);

        return (manga, authors, tags, [], []);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        log.Info($"Getting chapters {manga}");
        try
        {
            XDocument doc = XDocument.Load($"https://mangasee123.com/rss/{manga.MangaConnectorId}.xml");
            XElement[] chapterItems = doc.Descendants("item").ToArray();
            List<Chapter> chapters = new();
            Regex chVolRex = new(@".*chapter-([0-9\.]+)(?:-index-([0-9\.]+))?.*");
            foreach (XElement chapter in chapterItems)
            {
                string url = chapter.Descendants("link").First().Value;
                Match m = chVolRex.Match(url);
                string? volumeNumber = m.Groups[2].Success ? m.Groups[2].Value : "1";
                string chapterNumber = m.Groups[1].Value;

                string chapterUrl = Regex.Replace(url, @"-page-[0-9]+(\.html)", ".html");
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
                chapters.Add(new Chapter(manga, chapterUrl, chNum, volNum));
            }

            //Return Chapters ordered by Chapter-Number
            log.Info($"Got {chapters.Count} chapters. {manga}");
            return chapters.Order().ToArray();
        }
        catch (HttpRequestException e)
        {
            log.Info($"Failed to load https://mangasee123.com/rss/{manga.MangaConnectorId}.xml \n\r{e}");
            return Array.Empty<Chapter>();
        }
    }

    protected override string[] GetChapterImages(Chapter chapter)
    {
        Manga chapterParentManga = chapter.ParentManga;

        log.Info($"Retrieving chapter-info {chapter} {chapterParentManga}");

        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.Url, RequestType.Default);
        if (requestResult.htmlDocument is null)
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