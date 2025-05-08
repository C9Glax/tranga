using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using log4net;
using PuppeteerSharp;

namespace API.MangaDownloadClients;

internal class ChromiumDownloadClient : DownloadClient
{
    private static IBrowser? _browser;
    private readonly HttpDownloadClient _httpDownloadClient;
    private readonly Thread _closeStalePagesThread;
    private readonly List<KeyValuePair<IPage, DateTime>> _openPages = new ();
    
    private static async Task<IBrowser> StartBrowser(ILog log)
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
        }, new LoggerFactory([new Provider(log)]));
    }

    public ChromiumDownloadClient()
    {
        _httpDownloadClient = new();
        if(_browser is null)
            _browser = StartBrowser(Log).Result;
        _closeStalePagesThread = new Thread(CheckStalePages);
        _closeStalePagesThread.Start();
    }

    private void CheckStalePages()
    {
        while (true)
        {
            Thread.Sleep(TimeSpan.FromHours(1));
            Log.Debug("Removing stale pages");
            foreach ((IPage? key, DateTime value) in _openPages.Where(kv => kv.Value.Subtract(DateTime.Now) > TimeSpan.FromHours(1)))
            {
                Log.Debug($"Closing {key.Url}");
                key.CloseAsync().Wait();
            }
        }
    }

    private readonly Regex _imageUrlRex = new(@"https?:\/\/.*\.(?:p?jpe?g|gif|a?png|bmp|avif|webp)(\?.*)?");
    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        Log.Debug($"Requesting {url}");
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
            Log.Debug($"Page loaded. {url}");
        }
        catch (Exception e)
        {
            Log.Info($"Could not load Page {url}\n{e.Message}");
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

    private class Provider(ILog log) : ILoggerProvider
    {
        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ChromiumLogger(log);
        }
    }

    private class ChromiumLogger(ILog log) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter.Invoke(state, exception);
            switch(logLevel)
            {
                case LogLevel.Critical: log.Fatal(message); break;
                case LogLevel.Error: log.Error(message); break;
                case LogLevel.Warning: log.Warn(message); break;
                case LogLevel.Information: log.Info(message); break;
                case LogLevel.Debug: log.Debug(message); break;
                default: log.Info(message); break;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }
    }
}