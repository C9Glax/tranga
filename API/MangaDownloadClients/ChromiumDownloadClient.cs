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
using API.MangaDownloadClients;  // For HttpDownloadClient and IDownloadClient

namespace API.MangaDownloadClients;

internal class ChromiumDownloadClient : IDownloadClient, IDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ChromiumDownloadClient));
    private IBrowser? _browser;  // Instance-level: Avoids shared state races
    private readonly HttpDownloadClient _httpFallback;
    private readonly object _lock = new();  // Instance lock for init
    private static readonly Regex _imageUrlRex = new(@"https?:\/\/.*\.(?:p?jpe?g|gif|a?png|bmp|avif|webp)(\?.*)?");  // v1 image fallback regex
    private static readonly SemaphoreSlim _pageSemaphore = new(2, 2);  // Limit concurrent pages to 2
    
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

    public void Dispose()
    {
        if (_browser != null)
        {
            try
            {
                _browser.CloseAsync().GetAwaiter().GetResult();
                _browser.DisposeAsync().GetAwaiter().GetResult();
                Log.Debug("Chromium browser disposed.");
            }
            catch (Exception ex)
            {
                Log.Warn($"Error disposing browser: {ex.Message}");
            }
            _browser = null;
        }
    }

    public async Task<HttpResponseMessage> MakeRequest(string url, RequestType requestType, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        // v1 fallback: Use HTTP for direct images (faster, no browser)
        if (_imageUrlRex.IsMatch(url))
        {
            // Use public MakeRequest with default RequestType
            return _httpFallback.MakeRequest(url, RequestType.Default, referrer).GetAwaiter().GetResult();
        }

        EnsureBrowserInitialized();  // Lazy init if needed

        if (_browser is null)
        {
            Log.Warn("Browser not initialized; falling back to empty result.");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        _pageSemaphore.Wait();  // Limit concurrent pages
        IPage? page = null;
        try
        {
            var pageTask = _browser.NewPageAsync();
            pageTask.Wait(); // Sync wait
            page = pageTask.Result;

            // Set consistent User-Agent to match HttpDownloadClient (anti-bot)
            page.SetUserAgentAsync(Tranga.Settings.UserAgent).GetAwaiter().GetResult();

            // v1-style: Set referrer header only if provided (skip if null to avoid invalid)
            if (!string.IsNullOrEmpty(referrer))
            {
                var headers = new Dictionary<string, string> { { "Referer", referrer } };
                page.SetExtraHttpHeadersAsync(headers).GetAwaiter().GetResult();
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
            Exception lastEx = null;
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
                    // Recreate page on retry
                    page.CloseAsync().GetAwaiter().GetResult();
                    pageTask = _browser.NewPageAsync();
                    pageTask.Wait();
                    page = pageTask.Result;
                }
            }

            if (!success)
            {
                Log.Error($"Chromium request failed for {url} after retries: {lastEx?.Message}");
                return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
            }

            Log.Debug($"Page loaded. {url}");  // v1 log

            // Trigger lazy-load by scrolling (v2 enhancement for Asura images)
            page.EvaluateExpressionAsync("window.scrollTo(0, document.body.scrollHeight);").GetAwaiter().GetResult();
            Task.Delay(2000).Wait();  // Wait after scroll

            var contentTask = page.GetContentAsync();
            contentTask.Wait();
            string html = contentTask.Result ?? string.Empty;

            var content = new StringContent(html, Encoding.UTF8, "text/html");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };

            Log.Debug($"Chromium rendered {url} successfully.");
            return responseMessage;
        }
        catch (Exception ex)
        {
            Log.Error($"Chromium request failed for {url}: {ex.Message}");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
        finally
        {
            if (page != null)
            {
                try
                {
                    page.CloseAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Warn($"Error closing page: {ex.Message}");
                }
            }
            _pageSemaphore.Release();
        }
    }
}