using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace Tranga.MangaConnectors;

internal class ChromiumDownloadClient : DownloadClient
{
    private IBrowser browser { get; set; }
    private const string ChromiumVersion = "1154303";
    private const int StartTimeoutMs = 30000;
    private readonly HttpDownloadClient _httpDownloadClient;
    
    private async Task<IBrowser> DownloadBrowser()
    {
        BrowserFetcher browserFetcher = new ();
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
        
        Log($"Starting Browser. ({StartTimeoutMs}ms timeout)");
        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = browserFetcher.GetExecutablePath(ChromiumVersion),
            Args = new [] {
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--no-sandbox"},
            Timeout = StartTimeoutMs
        });
    }

    public ChromiumDownloadClient(GlobalBase clone) : base(clone)
    {
        this.browser = DownloadBrowser().Result;
        _httpDownloadClient = new(this);
    }

    private readonly Regex _imageUrlRex = new(@"https?:\/\/.*\.(?:p?jpe?g|gif|a?png|bmp|avif|webp)(\?.*)?");
    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        return _imageUrlRex.IsMatch(url)
            ? _httpDownloadClient.MakeRequestInternal(url, referrer)
            : MakeRequestBrowser(url, referrer, clickButton);
    }

    private RequestResult MakeRequestBrowser(string url, string? referrer = null, string? clickButton = null)
    {
        IPage page = this.browser.NewPageAsync().Result;
        page.DefaultTimeout = 10000;
        IResponse response;
        try
        {
            response = page.GoToAsync(url, WaitUntilNavigation.Networkidle0).Result;
            Log("Page loaded.");
        }
        catch (Exception e)
        {
            Log($"Could not load Page:\n{e.Message}");
            return new RequestResult(HttpStatusCode.InternalServerError, null, Stream.Null);
        }

        Stream stream = Stream.Null;
        HtmlDocument? document = null;

        if (response.Headers.TryGetValue("Content-Type", out string? content))
        {
            if (content.Contains("text/html"))
            {
                if (clickButton is not null && page.QuerySelectorAsync(clickButton).Result is not null)
                    page.ClickAsync(clickButton).Wait();
                string htmlString = page.GetContentAsync().Result;
                stream = new MemoryStream(Encoding.Default.GetBytes(htmlString));
                document = new ();
                document.LoadHtml(htmlString);
            }else if (content.Contains("image"))
            {
                stream = new MemoryStream(response.BufferAsync().Result);
            }
        }
        else
        {
            page.CloseAsync();
            return new RequestResult(HttpStatusCode.InternalServerError, null, Stream.Null);
        }
        
        page.CloseAsync();
        return new RequestResult(response.Status, document, stream, false, "");
    }

    public override void Close()
    {
        this.browser.CloseAsync();
    }
}