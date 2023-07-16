using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Logging;
using Newtonsoft.Json;
using PuppeteerSharp;
using Tranga.TrangaTasks;

namespace Tranga.Connectors;

public class Mangasee : Connector
{
    public override string name { get; }
    private IBrowser? _browser = null;
    private const string ChromiumVersion = "1154303";

    public Mangasee(TrangaSettings settings, Logger? logger = null) : base(settings, logger)
    {
        this.name = "Mangasee";
        this.downloadClient = new DownloadClient(new Dictionary<byte, int>()
        {
            { (byte)1, 60 }
        }, logger);

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
            logger?.WriteLine(this.GetType().ToString(), "Downloading headless browser");
            DateTime last = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));
            browserFetcher.DownloadProgressChanged += (sender, args) =>
            {
                double currentBytes = Convert.ToDouble(args.BytesReceived) / Convert.ToDouble(args.TotalBytesToReceive);
                if (args.TotalBytesToReceive == args.BytesReceived)
                {
                    logger?.WriteLine(this.GetType().ToString(), "Browser downloaded.");
                }
                else if (DateTime.Now > last.AddSeconds(5))
                {
                    logger?.WriteLine(this.GetType().ToString(), $"Browser download progress: {currentBytes:P2}");
                    last = DateTime.Now;
                }

            };
            if (!browserFetcher.CanDownloadAsync(ChromiumVersion).Result)
            {
                logger?.WriteLine(this.GetType().ToString(), $"Can't download browser version {ChromiumVersion}");
                return;
            }
            await browserFetcher.DownloadAsync(ChromiumVersion);
        }
        
        logger?.WriteLine(this.GetType().ToString(), "Starting browser.");
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

    protected override Publication[] GetPublicationsInternal(string publicationTitle = "")
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Publications (title={publicationTitle})");
        string requestUrl = $"https://mangasee123.com/_search.php";
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, (byte)1);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Publication>();

        return ParsePublicationsFromHtml(requestResult.result, publicationTitle);
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

        HashSet<Publication> ret = new();
        List<SearchResultItem> orderedFiltered =
            queryFiltered.OrderBy(item => item.Value).ToDictionary(item => item.Key, item => item.Value).Keys.ToList();

        foreach (SearchResultItem orderedItem in orderedFiltered)
        {
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest($"https://mangasee123.com/manga/{orderedItem.i}", (byte)1);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
                return Array.Empty<Publication>();
            ret.Add(ParseSinglePublicationFromHtml(requestResult.result, orderedItem.s, orderedItem.i, orderedItem.a));
        }
        return ret.ToArray();
    }

    
    private Publication ParseSinglePublicationFromHtml(Stream html, string sortName, string publicationId, string[] a)
    {
        StreamReader reader = new (html);
        string htmlString = reader.ReadToEnd();
        HtmlDocument document = new ();
        document.LoadHtml(htmlString);

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
#pragma warning disable CS8618 //Will always be set
        public string i { get; set; }
        public string s { get; set; }
        public string[] a { get; set; }
#pragma warning restore CS8618

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

    public override Chapter[] GetChapters(Publication publication, string language = "")
    {
        XDocument doc = XDocument.Load($"https://mangasee123.com/rss/{publication.publicationId}.xml");
        XElement[] chapterItems = doc.Descendants("item").ToArray();
        List<Chapter> ret = new();
        foreach (XElement chapter in chapterItems)
        {
            string? volumeNumber = "1";
            string chapterName = chapter.Descendants("title").First().Value;
            string chapterNumber = Regex.Matches(chapterName, "[0-9]+")[^1].ToString();

            string url = chapter.Descendants("link").First().Value;
            url = url.Replace(Regex.Matches(url,"(-page-[0-9])")[0].ToString(),"");
            ret.Add(new Chapter(publication, "", volumeNumber, chapterNumber, url));
        }

        //Return Chapters ordered by Chapter-Number
        NumberFormatInfo chapterNumberFormatInfo = new()
        {
            NumberDecimalSeparator = "."
        };
        logger?.WriteLine(this.GetType().ToString(), $"Done getting Chapters for {publication.internalId}");
        return ret.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
    }

    public override HttpStatusCode DownloadChapter(Publication publication, Chapter chapter, DownloadChapterTask parentTask, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        while (this._browser is null && !(cancellationToken?.IsCancellationRequested??false))
        {
            logger?.WriteLine(this.GetType().ToString(), "Waiting for headless browser to download...");
            Thread.Sleep(1000);
        }
        if (cancellationToken?.IsCancellationRequested??false)
            return HttpStatusCode.RequestTimeout;
        
        logger?.WriteLine(this.GetType().ToString(), $"Downloading Chapter-Info {publication.sortName} {publication.internalId} {chapter.volumeNumber}-{chapter.chapterNumber}");
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
        
            return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), (byte)1, parentTask, comicInfoPath, cancellationToken:cancellationToken);
        }
        return response.Status;
    }
}