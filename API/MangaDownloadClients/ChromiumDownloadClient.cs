using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using PuppeteerSharp;

namespace API.MangaDownloadClients;

internal class ChromiumDownloadClient : IDownloadClient, IAsyncDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ChromiumDownloadClient));
    private IBrowser? _browser;  // Instance-level: Avoids shared state races
    private readonly HttpDownloadClient _httpFallback;
    private readonly object _lock = new();  // Instance lock for init
    private static readonly Regex _imageUrlRex = new(@"https?:\/\/.*\.(?:p?jpe?g|gif|a?png|bmp|avif|webp)(\?.*)?");  // v1 image fallback regex
    private long _activePages = 0;  // Manual counter for active pages
    private readonly int _maxPages = 2;  // Limit to 2 concurrent pages

    public ChromiumDownloadClient()
    {
        _httpFallback = new();  // Fallback for direct images
    }

    private void EnsureBrowserInitialized()
    {
        if (_browser != null) return;

        lock (_lock)
        {
            if (_browser != null) return;  // Double-check lock
            try
            {
                Log.Debug("Starting Chromium init.");

                // Check for local Chrome path from ENV (skip download if present)
                string? localPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH") ?? Environment.GetEnvironmentVariable("CHROME_BIN");
                if (string.IsNullOrEmpty(localPath) || !System.IO.File.Exists(localPath))
                {
                    throw new InvalidOperationException($"Local Chromium binary not found at {localPath}. Set PUPPETEER_EXECUTABLE_PATH or CHROME_BIN.");
                }
                Log.InfoFormat("Using local Chromium at {0}", localPath);

                LaunchOptions launchOptions = new()
                {
                    Headless = true,
                    Timeout = 60000, 
                    ExecutablePath = localPath,
                    Args = Environment.GetEnvironmentVariable("PUPPETEER_ARGS")?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new[] { 
                        "--no-sandbox", 
                        "--disable-setuid-sandbox", 
                        "--disable-dev-shm-usage",
                        "--disable-gpu"
                    }
                };
                // Launch with options and null loggerFactory
                _browser = Puppeteer.LaunchAsync(launchOptions, null).GetAwaiter().GetResult();

                Log.Debug("Chromium browser initialized successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize Chromium browser: {ex.Message}");
                _browser = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            try
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
                Log.Debug("Chromium browser disposed.");
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Error disposing browser: {0}", ex.Message);
            }
            _browser = null;
        }
    }

    public async Task<HttpResponseMessage> MakeRequest(string url, RequestType requestType, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        Log.DebugFormat("Using {0} for {1}", typeof(ChromiumDownloadClient).FullName, url);

        if (_imageUrlRex.IsMatch(url))
        {
            HttpDownloadClient httpClient = new();
            return await httpClient.MakeRequest(url, requestType, referrer, cancellationToken);
        }

        EnsureBrowserInitialized();  // Lazy init if needed

        if (_browser is null)
        {
            Log.Warn("Browser not initialized; returning error.");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        // Wait for available slot (async poll loop)
        while (Interlocked.Read(ref _activePages) >= _maxPages)
        {
            await Task.Delay(50, cancellationToken ?? CancellationToken.None);  // Poll every 50ms
        }

        Interlocked.Increment(ref _activePages);  // Increment counter
        IPage? page = null;
        try
        {
            page = await _browser.NewPageAsync();

            await page.SetUserAgentAsync(Tranga.Settings.UserAgent);

            if (!string.IsNullOrEmpty(referrer))
            {
                Dictionary<string, string> headers = new() { { "Referer", referrer } };
                await page.SetExtraHttpHeadersAsync(headers);
            }

            NavigationOptions navOptions = new() { WaitUntil = new[] { WaitUntilNavigation.Load } };
            string waitTimeStr = Environment.GetEnvironmentVariable("CHROMIUM_WAIT_TIME") ?? "15";
            if (int.TryParse(waitTimeStr, out int waitTimeSeconds) && waitTimeSeconds > 0)
            {
                navOptions.Timeout = waitTimeSeconds * 1000;
            }
            else
            {
                navOptions.Timeout = 15000;  // Default 15s
            }

            bool success = false;
            Exception? lastEx = null;
            for (int retry = 0; retry < 3; retry++)
            {
                try
                {
                    // Simple GoToAsync overload (no navOptions/CT to avoid generic issues)
                    await page.GoToAsync(url);
                    success = true;
                    Log.DebugFormat("Page loaded on retry {0}. {1}", retry + 1, url);
                    break;
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex.Message.Contains("Timeout"))
                {
                    lastEx = ex;
                    Log.WarnFormat("Timeout for {0} on retry {1}; waiting {2}ms...", url, retry + 1, 1000 * (retry + 1));
                    await Task.Delay(1000 * (retry + 1));
                    await page.CloseAsync();
                    page = await _browser.NewPageAsync();
                }
            }

            if (!success)
            {
                Log.ErrorFormat("Chromium request failed for {0} after retries: {1}", url, lastEx?.Message);
                return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
            }

            Log.DebugFormat("Page loaded. {0}", url);

            await page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight);");
            await Task.Delay(2000);  // Hardcoded scroll wait

            string html = await page.GetContentAsync();

            StringContent content = new(html, Encoding.UTF8, "text/html");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            HttpResponseMessage responseMessage = new(HttpStatusCode.OK) { Content = content };

            Log.DebugFormat("Chromium rendered {0} successfully.", url);
            return responseMessage;
        }
        finally
        {
            if (page != null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Error closing page: {0}", ex.Message);
                }
            }
            Interlocked.Decrement(ref _activePages);  // Decrement counter
        }
    }
}