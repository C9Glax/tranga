using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Mangasee : MangaConnector
{
    public Mangasee(GlobalBase clone) : base(clone, "Mangasee")
    {
        this.downloadClient = new ChromiumDownloadClient(clone, new Dictionary<byte, int>()
        {
            { 1, 60 }
        });
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string requestUrl = $"https://mangasee123.com/_search.php";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Manga>();

        if (requestResult.htmlDocument is null)
            return Array.Empty<Manga>();
        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument, publicationTitle);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/mangasee123.com\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        DownloadClient.RequestResult requestResult = this.downloadClient.MakeRequest(url, 1);
        if(requestResult.htmlDocument is not null)
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId);
        return null;
    }

    private Manga[] ParsePublicationsFromHtml(HtmlDocument document, string publicationTitle)
    {
        string jsonString = document.DocumentNode.SelectSingleNode("//body").InnerText;
        List<SearchResultItem> result = JsonConvert.DeserializeObject<List<SearchResultItem>>(jsonString)!;
        Dictionary<SearchResultItem, int> queryFiltered = new();
        foreach (SearchResultItem resultItem in result)
        {
            int matches = resultItem.GetMatches(publicationTitle);
            if (matches > 0)
                queryFiltered.TryAdd(resultItem, matches);
        }

        queryFiltered = queryFiltered.Where(item => item.Value >= publicationTitle.Split(' ').Length - 1)
            .ToDictionary(item => item.Key, item => item.Value);
        
        Log($"Retrieved {queryFiltered.Count} publications.");

        HashSet<Manga> ret = new();
        List<SearchResultItem> orderedFiltered =
            queryFiltered.OrderBy(item => item.Value).ToDictionary(item => item.Key, item => item.Value).Keys.ToList();

        foreach (SearchResultItem orderedItem in orderedFiltered)
        {
            Manga? manga = GetMangaFromUrl($"https://mangasee123.com/manga/{orderedItem.i}");
            if (manga is not null)
                ret.Add((Manga)manga);
        }
        return ret.ToArray();
    }

    
    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//img");
        string posterUrl = posterNode.GetAttributeValue("src", "");
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//div[@class='BoxBody']//div[@class='row']//h1");
        string sortName = titleNode.InnerText;

        HtmlNode[] authorsNodes = document.DocumentNode.SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Author(s):']/..").Descendants("a").ToArray();
        List<string> authors = new();
        foreach(HtmlNode authorNode in authorsNodes)
            authors.Add(authorNode.InnerText);
        
        HtmlNode[] genreNodes = document.DocumentNode.SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Genre(s):']/..").Descendants("a").ToArray();
        foreach (HtmlNode genreNode in genreNodes)
            tags.Add(genreNode.InnerText);
        
        HtmlNode yearNode = document.DocumentNode.SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Released:']/..").Descendants("a").First();
        int year = Convert.ToInt32(yearNode.InnerText);
        
        HtmlNode[] statusNodes = document.DocumentNode.SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Status:']/..").Descendants("a").ToArray();
        foreach(HtmlNode statusNode in statusNodes)
            if (statusNode.InnerText.Contains("publish", StringComparison.CurrentCultureIgnoreCase))
                status = statusNode.InnerText.Split(' ')[0];
        
        HtmlNode descriptionNode = document.DocumentNode.SelectNodes("//div[@class='BoxBody']//div[@class='row']//span[text()='Description:']/..").Descendants("div").First();
        string description = descriptionNode.InnerText;
        
        Manga manga = new (sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, status, publicationId);
        cachedPublications.Add(manga);
        return manga;
    }
    
    // ReSharper disable once ClassNeverInstantiated.Local Will be instantiated during deserialization
    private class SearchResultItem
    {
        public string i { get; init; }
        public string s { get; init; }
        public string[] a { get; init; }

        [JsonConstructor]
        public SearchResultItem(string i, string s, string[] a)
        {
            this.i = i;
            this.s = s;
            this.a = a;
        }

        public int GetMatches(string title)
        {
            int ret = 0;
            Regex cleanRex = new("[A-z0-9]*");
            string[] badWords = { "a", "an", "no", "ni", "so", "as", "and", "the", "of", "that", "in", "is", "for" };

            string[] titleTerms = title.Split(new[] { ' ', '-' }).Where(str => !badWords.Contains(str)).ToArray();

            foreach (Match matchTerm in cleanRex.Matches(this.i))
                ret += titleTerms.Count(titleTerm =>
                    titleTerm.Equals(matchTerm.Value, StringComparison.OrdinalIgnoreCase));
            
            foreach (Match matchTerm in cleanRex.Matches(this.s))
                ret += titleTerms.Count(titleTerm =>
                    titleTerm.Equals(matchTerm.Value, StringComparison.OrdinalIgnoreCase));
            
            foreach(string alt in this.a)
                foreach (Match matchTerm in cleanRex.Matches(alt))
                    ret += titleTerms.Count(titleTerm =>
                        titleTerm.Equals(matchTerm.Value, StringComparison.OrdinalIgnoreCase));
            
            return ret;
        }
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        XDocument doc = XDocument.Load($"https://mangasee123.com/rss/{manga.publicationId}.xml");
        XElement[] chapterItems = doc.Descendants("item").ToArray();
        List<Chapter> chapters = new();
        foreach (XElement chapter in chapterItems)
        {
            string volumeNumber = "1";
            string chapterName = chapter.Descendants("title").First().Value;
            string chapterNumber = Regex.Matches(chapterName, "[0-9]+")[^1].ToString();

            string url = chapter.Descendants("link").First().Value;
            url = url.Replace(Regex.Matches(url,"(-page-[0-9])")[0].ToString(),"");
            chapters.Add(new Chapter(manga, "", volumeNumber, chapterNumber, url));
        }

        //Return Chapters ordered by Chapter-Number
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, numberFormatDecimalPoint)).ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        DownloadClient.RequestResult requestResult = this.downloadClient.MakeRequest(chapter.url, 1);
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
        
        return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), 1, comicInfoPath, progressToken:progressToken);
    }
}