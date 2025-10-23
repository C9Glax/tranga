using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using log4net;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using API;  // For Tranga.Settings.UserAgent
using API.MangaDownloadClients;  // For HttpDownloadClient

namespace API.MangaDownloadClients;

internal class ChromiumDownloadClient : DownloadClient, IDisposable
{
    private new static readonly ILog Log = LogManager.GetLogger(typeof(ChromiumDownloadClient));
    private static IBrowser? _browser;  // Static: Shared across all instances (v1 style)
    private static readonly object _lock = new();  // Thread-safe init
    private readonly HttpDownloadClient _httpFallback;
    private static readonly Regex _imageUrlRex = new(@"https?:\/\/.*\.(?:p?jpe?g|gif|a?png|bmp|avif|webp)(\?.*)?");  // v1 image fallback regex
    private static readonly SemaphoreSlim _pageSemaphore = new(2, 2);  // Limit concurrent pages to 2
    
    public ChromiumDownloadClient()
    {
        _httpFallback = new();  // Fallback for direct images
        EnsureBrowserInitialized();  // Lazy init if needed
    }

    private void EnsureBrowserInitialized()
    {
        if (_browser != null) return;

        lock (_lock)
        {
            if (_browser != null) return;  // Double-check lock

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));  // Increased to 60s
            try
            {
                Log.Debug("Starting Chromium init (global).");

                // Retry logic (up to 3x)
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        Task.Run(async () =>
                        {
                            // Download default revision (no args overload)
                            await new BrowserFetcher().DownloadAsync();

                            var launchOptions = new LaunchOptions
                            {
                                Headless = true,
                                Args = new[] { 
                                    "--no-sandbox", 
                                    "--disable-setuid-sandbox", 
                                    "--disable-dev-shm-usage",
                                    "--disable-gpu"  // v1 arg for stability
                                }
                            };
                            // Launch with options and null loggerFactory (no CancellationToken overload)
                            _browser = await Puppeteer.LaunchAsync(launchOptions, null);
                        }).Wait(cts.Token);  // Block until complete

                        Log.Debug("Chromium browser initialized successfully (global).");
                        return;
                    }
                    catch (Exception ex) when (attempt < 3)
                    {
                        Log.Warn($"Chromium init attempt {attempt} failed: {ex.Message}. Retrying...");
                        cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));  // Shorter retry timeout
                    }
                }
                throw new TimeoutException("Chromium init failed after 3 retries.");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize Chromium browser: {ex.Message}");
                _browser = null;
            }
            finally
            {
                cts?.Dispose();
            }
        }
    }

    public void Dispose()
    {
        // No per-instance dispose; global browser closed manually if needed (e.g., app shutdown)
        // To close globally: Call CloseGlobalBrowser() elsewhere if desired
    }

    public static void CloseGlobalBrowser()
    {
        lock (_lock)
        {
            _browser?.CloseAsync().GetAwaiter().GetResult();
            _browser?.DisposeAsync().GetAwaiter().GetResult();
            _browser = null;
            Log.Debug("Global Chromium browser closed.");
        }
    }

    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        // v1 fallback: Use HTTP for direct images (faster, no browser)
        if (_imageUrlRex.IsMatch(url))
        {
            return _httpFallback.MakeRequestInternal(url, referrer, clickButton);
        }

        if (_browser is null)
        {
            Log.Warn("Browser not initialized; falling back to empty result.");
            EnsureBrowserInitialized();  // Retry init
            if (_browser is null)
            {
                return new RequestResult(HttpStatusCode.ServiceUnavailable, null!, Stream.Null);
            }
        }

        _pageSemaphore.Wait();  // Limit concurrent pages
        try
        {
            var pageTask = _browser.NewPageAsync();
            pageTask.Wait(); // Sync wait
            var page = pageTask.Result;

            // Set consistent User-Agent to match HttpDownloadClient (anti-bot)
            page.SetUserAgentAsync(Tranga.Settings.UserAgent).GetAwaiter().GetResult();

            // v1-style: Set referrer header only if provided (skip if null to avoid invalid)
            if (!string.IsNullOrEmpty(referrer))
            {
                var headers = new Dictionary<string, string> { { "Referer", referrer } };
                page.SetExtraHttpHeadersAsync(headers).GetAwaiter().GetResult();
            }

            if (!string.IsNullOrEmpty(clickButton))
            {
                Log.Warn("Chromium client ignoring clickButton parameter (not implemented).");
            }

            // v1-style: Simple GoToAsync without ReferrerPolicy (avoids protocol error)
            var navOptions = new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Load } };  // Changed to Load for faster
            bool success = false;
            Exception lastEx = null;

            // Retry (3x) for timeout
            for (int retry = 0; retry < 3; retry++)
            {
                try
                {
                    var gotoTask = page.GoToAsync(url, navOptions);
                    gotoTask.Wait();
                    if (gotoTask.Result != null)
                    {
                        Log.Debug($"Page loaded on retry {retry + 1}. {url}");
                        success = true;
                        break;
                    }
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex.Message.Contains("Timeout"))
                {
                    lastEx = ex;
                    Log.Warn($"Timeout for {url} on retry {retry + 1}; waiting {1000 * (retry + 1)}ms...");
                    Task.Delay(1000 * (retry + 1)).Wait();  // Backoff
                    page.CloseAsync().GetAwaiter().GetResult();
                    pageTask = _browser.NewPageAsync();
                    pageTask.Wait();
                    page = pageTask.Result;
                }
            }

            if (!success)
            {
                Log.Error($"Chromium request failed for {url} after retries: {lastEx?.Message}");
                page.CloseAsync().GetAwaiter().GetResult();
                return new RequestResult(HttpStatusCode.GatewayTimeout, null!, Stream.Null);
            }

            Log.Debug($"Page loaded. {url}");  // v1 log

            // Trigger lazy-load by scrolling (v2 enhancement for Asura images)
            page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight);").GetAwaiter().GetResult();
            Task.Delay(2000).Wait();  // Wait after scroll

            var contentTask = page.GetContentAsync();
            contentTask.Wait();
            string html = contentTask.Result ?? string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
            stream.Position = 0;  // Ensure stream is readable from start

            page.CloseAsync().GetAwaiter().GetResult();

            Log.Debug($"Chromium rendered {url} successfully.");
            return new RequestResult(HttpStatusCode.OK, doc, stream, false, url);
        }
        catch (Exception ex)
        {
            Log.Error($"Chromium request failed for {url}: {ex.Message}");
            return new RequestResult(HttpStatusCode.InternalServerError, null!, Stream.Null);
        }
        finally
        {
            _pageSemaphore.Release();
        }
    }
}