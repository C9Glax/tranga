using System.Net;
using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Tranga.MangaConnectors;

internal class ChromiumDownloadClient : DownloadClient
{
    private IBrowser browser { get; set; }
    private const string ChromiumVersion = "1154303";
    
    private async Task<IBrowser> DownloadBrowser()
    {
        BrowserFetcher browserFetcher = new BrowserFetcher();
        foreach(string rev in browserFetcher.LocalRevisions().Where(rev => rev != ChromiumVersion))
            browserFetcher.Remove(rev);
        if (!browserFetcher.LocalRevisions().Contains(ChromiumVersion))
        {
            logger?.LogInformation("Downloading headless browser");
            DateTime last = DateTime.Now.Subtract(TimeSpan.FromSeconds(5));
            browserFetcher.DownloadProgressChanged += (_, args) =>
            {
                double currentBytes = Convert.ToDouble(args.BytesReceived) / Convert.ToDouble(args.TotalBytesToReceive);
                if (args.TotalBytesToReceive == args.BytesReceived)
                    logger?.LogDebug("Browser downloaded.");
                else if (DateTime.Now > last.AddSeconds(1))
                {
                    logger?.LogDebug($"Browser download progress: {currentBytes:P2}");
                    last = DateTime.Now;
                }

            };
            if (!browserFetcher.CanDownloadAsync(ChromiumVersion).Result)
            {
                logger?.LogError($"Can't download browser version {ChromiumVersion}");
                throw new Exception();
            }
            await browserFetcher.DownloadAsync(ChromiumVersion);
        }
        
        logger?.LogInformation("Starting Browser.");
        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = browserFetcher.GetExecutablePath(ChromiumVersion),
            Args = new [] {
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--no-sandbox"},
            Timeout = 10000
        });
    }

    public ChromiumDownloadClient(GlobalBase clone) : base(clone)
    {
        this.browser = DownloadBrowser().Result;
    }

    protected override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        IPage page = this.browser.NewPageAsync().Result;
        page.DefaultTimeout = 10000;
        IResponse response;
        try
        {
            response = page.GoToAsync(url, WaitUntilNavigation.Networkidle0).Result;
            logger?.LogDebug("Page loaded.");
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Could not load Page");
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