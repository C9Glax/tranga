using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Newtonsoft.Json;
using PuppeteerSharp;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Mangasee : MangaConnector
{
    public override string name { get; }
    private IBrowser? _browser;
    private const string ChromiumVersion = "1154303";

    public Mangasee(GlobalBase clone) : base(clone)
    {
        this.name = "Mangasee";
        this.downloadClient = new DownloadClient(new Dictionary<byte, int>()
        {
            { 1, 60 }
        }, clone);

        Task d = new Task(DownloadBrowser);
        d.Start();
    }

    private async void DownloadBrowser()
    {
        BrowserFetcher browserFetcher = new BrowserFetcher();
        foreach(string rev in browserFetcher.LocalRevisions().Where(rev => rev != ChromiumVersion))
            browserFetcher.Remove(rev);
        if (!browserFetcher.LocalRevisions().Contains(ChromiumVersion))
        {
            Log("Downloading headless browser");
            DateTime last = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));
            browserFetcher.DownloadProgressChanged += (_, args) =>
            {
                double currentBytes = Convert.ToDouble(args.BytesReceived) / Convert.ToDouble(args.TotalBytesToReceive);
                if (args.TotalBytesToReceive == args.BytesReceived)
                    Log("Browser downloaded.");
                else if (DateTime.Now > last.AddSeconds(1))
                {
                    Log($"Browser download progress: {currentBytes:P2}");
                    last = DateTime.Now;
                }

            };
            if (!browserFetcher.CanDownloadAsync(ChromiumVersion).Result)
            {
                Log($"Can't download browser version {ChromiumVersion}");
                throw new Exception();
            }
            await browserFetcher.DownloadAsync(ChromiumVersion);
        }
        
        Log("Starting Browser.");
        this._browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = browserFetcher.GetExecutablePath(ChromiumVersion),
            Args = new [] {
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--no-sandbox"}
        });
    }

    public override Publication[] GetPublications(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string requestUrl = $"https://mangasee123.com/_search.php";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, 1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Publication>();

        Publication[] publications = ParsePublicationsFromHtml(requestResult.result, publicationTitle);
        cachedPublications.AddRange(publications);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    private Publication[] ParsePublicationsFromHtml(Stream html, string publicationTitle)
    {
        string jsonString = new StreamReader(html).ReadToEnd();
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

        HashSet<Publication> ret = new();
        List<SearchResultItem> orderedFiltered =
            queryFiltered.OrderBy(item => item.Value).ToDictionary(item => item.Key, item => item.Value).Keys.ToList();

        uint index = 1;
        foreach (SearchResultItem orderedItem in orderedFiltered)
        {
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest($"https://mangasee123.com/manga/{orderedItem.i}", 1);
            if ((int)requestResult.statusCode >= 200 || (int)requestResult.statusCode < 300)
            {
                Log($"Retrieving Publication info: {orderedItem.s} {index++}/{orderedFiltered.Count}");
                ret.Add(ParseSinglePublicationFromHtml(requestResult.result, orderedItem.s, orderedItem.i, orderedItem.a));
            }
        }
        return ret.ToArray();
    }

    
    private Publication ParseSinglePublicationFromHtml(Stream html, string sortName, string publicationId, string[] a)
    {
        StreamReader reader = new (html);
        HtmlDocument document = new ();
        document.LoadHtml(reader.ReadToEnd());

        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();

        HtmlNode posterNode =
            document.DocumentNode.Descendants("img").First(img => img.HasClass("img-fluid") && img.HasClass("bottom-5"));
        string posterUrl = posterNode.GetAttributeValue("src", "");
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

        HtmlNode attributes = document.DocumentNode.Descendants("div")
            .First(div => div.HasClass("col-md-9") && div.HasClass("col-sm-8") && div.HasClass("top-5"))
            .Descendants("ul").First();

        HtmlNode[] authorsNodes = attributes.Descendants("li")
            .First(node => node.InnerText.Contains("author(s):", StringComparison.CurrentCultureIgnoreCase))
            .Descendants("a").ToArray();
        List<string> authors = new();
        foreach(HtmlNode authorNode in authorsNodes)
            authors.Add(authorNode.InnerText);

        HtmlNode[] genreNodes = attributes.Descendants("li")
            .First(node => node.InnerText.Contains("genre(s):", StringComparison.CurrentCultureIgnoreCase))
            .Descendants("a").ToArray();
        foreach (HtmlNode genreNode in genreNodes)
            tags.Add(genreNode.InnerText);

        HtmlNode yearNode = attributes.Descendants("li")
            .First(node => node.InnerText.Contains("released:", StringComparison.CurrentCultureIgnoreCase))
            .Descendants("a").First();
        int year = Convert.ToInt32(yearNode.InnerText);

        HtmlNode[] statusNodes = attributes.Descendants("li")
            .First(node => node.InnerText.Contains("status:", StringComparison.CurrentCultureIgnoreCase))
            .Descendants("a").ToArray();
        foreach(HtmlNode statusNode in statusNodes)
            if (statusNode.InnerText.Contains("publish", StringComparison.CurrentCultureIgnoreCase))
                status = statusNode.InnerText.Split(' ')[0];
        
        HtmlNode descriptionNode = attributes.Descendants("li").First(node => node.InnerText.Contains("description:", StringComparison.CurrentCultureIgnoreCase)).Descendants("div").First();
        string description = descriptionNode.InnerText;
        
        int i = 0;
        foreach(string at in a)
            altTitles.Add((i++).ToString(), at);
        
        return new Publication(sortName, authors, description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, status, publicationId);
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

    public override Chapter[] GetChapters(Publication publication, string language="en")
    {
        Log($"Getting chapters {publication}");
        XDocument doc = XDocument.Load($"https://mangasee123.com/rss/{publication.publicationId}.xml");
        XElement[] chapterItems = doc.Descendants("item").ToArray();
        List<Chapter> chapters = new();
        foreach (XElement chapter in chapterItems)
        {
            string volumeNumber = "1";
            string chapterName = chapter.Descendants("title").First().Value;
            string chapterNumber = Regex.Matches(chapterName, "[0-9]+")[^1].ToString();

            string url = chapter.Descendants("link").First().Value;
            url = url.Replace(Regex.Matches(url,"(-page-[0-9])")[0].ToString(),"");
            chapters.Add(new Chapter(publication, "", volumeNumber, chapterNumber, url));
        }

        //Return Chapters ordered by Chapter-Number
        NumberFormatInfo chapterNumberFormatInfo = new()
        {
            NumberDecimalSeparator = "."
        };
        Log($"Got {chapters.Count} chapters. {publication}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        while (this._browser is null && !(progressToken?.cancellationRequested??false))
        {
            Log("Waiting for headless browser to download...");
            Thread.Sleep(1000);
        }
        if (progressToken?.cancellationRequested??false)
            return HttpStatusCode.RequestTimeout;
        
        Log($"Retrieving chapter-info {chapter} {chapter.parentPublication}");
        IPage page = _browser!.NewPageAsync().Result;
        IResponse response = page.GoToAsync(chapter.url).Result;
        if (response.Ok)
        {
            HtmlDocument document = new ();
            document.LoadHtml(page.GetContentAsync().Result);

            HtmlNode gallery = document.DocumentNode.Descendants("div").First(div => div.HasClass("ImageGallery"));
            HtmlNode[] images = gallery.Descendants("img").Where(img => img.HasClass("img-fluid")).ToArray();
            List<string> urls = new();
            foreach(HtmlNode galleryImage in images)
                urls.Add(galleryImage.GetAttributeValue("src", ""));
            
            string comicInfoPath = Path.GetTempFileName();
            File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
            return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), 1, comicInfoPath, progressToken:progressToken);
        }
        return response.Status;
    }
}