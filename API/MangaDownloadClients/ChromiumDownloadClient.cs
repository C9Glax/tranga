using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using PuppeteerSharp;

namespace API.MangaDownloadClients;

internal class ChromiumDownloadClient : DownloadClient
{
    private static IBrowser? _browser;
    private readonly HttpDownloadClient _httpDownloadClient;
    private readonly Thread _closeStalePagesThread;
    private readonly List<KeyValuePair<IPage, DateTime>> _openPages = new ();
    
    private static async Task<IBrowser> StartBrowser()
    {
        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new [] {
                "--disable-gpu",
                "--disable-dev-shm-usage",
                "--disable-setuid-sandbox",
                "--no-sandbox"},
            Timeout = 30000
        });
    }

    public ChromiumDownloadClient()
    {
        _httpDownloadClient = new();
        if(_browser is null)
            _browser = StartBrowser().Result;
        _closeStalePagesThread = new Thread(CheckStalePages);
        _closeStalePagesThread.Start();
    }

    private void CheckStalePages()
    {
        while (true)
        {
            Thread.Sleep(TimeSpan.FromHours(1));
            foreach ((IPage? key, DateTime value) in _openPages.Where(kv => kv.Value.Subtract(DateTime.Now) > TimeSpan.FromHours(1)))
            {
                key.CloseAsync().Wait();
            }
        }
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
        if (_browser is null)
            return new RequestResult(HttpStatusCode.InternalServerError, null, Stream.Null);
        IPage page = _browser.NewPageAsync().Result;
        _openPages.Add(new(page, DateTime.Now));
        page.SetExtraHttpHeadersAsync(new() { { "Referer", referrer } });
        page.DefaultTimeout = 30000;
        IResponse response;
        try
        {
            response = page.GoToAsync(url, WaitUntilNavigation.Networkidle0).Result;
            //Log($"Page loaded. {url}");
        }
        catch (Exception e)
        {
            //Log($"Could not load Page {url}\n{e.Message}");
            page.CloseAsync();
            _openPages.Remove(_openPages.Find(i => i.Key == page));
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
            page.CloseAsync().Wait();
            _openPages.Remove(_openPages.Find(i => i.Key == page));
            return new RequestResult(HttpStatusCode.InternalServerError, null, Stream.Null);
        }
        
        page.CloseAsync().Wait();
        _openPages.Remove(_openPages.Find(i => i.Key == page));
        return new RequestResult(response.Status, document, stream, false, "");
    }
}