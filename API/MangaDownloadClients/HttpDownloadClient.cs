﻿using System.Net;
using log4net;

namespace API.MangaDownloadClients;

internal class HttpDownloadClient : IDownloadClient
{
    private static readonly HttpClient Client = new(handler: Tranga.RateLimitHandler)
    {
        Timeout = TimeSpan.FromSeconds(10),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
        DefaultRequestHeaders = { { "User-Agent", Tranga.Settings.UserAgent } }
    };
    private static readonly FlareSolverrDownloadClient FlareSolverrDownloadClient = new(Client);
    private ILog Log { get; } = LogManager.GetLogger(typeof(HttpDownloadClient));
    
    public async Task<HttpResponseMessage> MakeRequest(string url, RequestType requestType, string? referrer = null)
    {
        Log.Debug($"Using {typeof(HttpDownloadClient).FullName} for {url}");
        HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
        if (referrer is not null)
            requestMessage.Headers.Referrer = new (referrer);
        Log.Debug($"Requesting {url}");
        
        try
        {
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            Log.Debug($"Request {url} returned {(int)response.StatusCode} {response.StatusCode}");
            if(response.IsSuccessStatusCode)
                return response;

            if (response.Headers.Server.Any(s =>
                    (s.Product?.Name ?? "").Contains("cloudflare", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log.Debug("Retrying with FlareSolverr!");
                return await FlareSolverrDownloadClient.MakeRequest(url, requestType, referrer);
            }
            
            Log.Debug($"Request returned status code {(int)response.StatusCode} {response.StatusCode}:\n" +
                      $"=====\n" +
                      $"Request:\n" +
                      $"{requestMessage.Method} {requestMessage.RequestUri}\n" +
                      $"{requestMessage.Version} {requestMessage.VersionPolicy}\n" +
                      $"Headers:\n\t{string.Join("\n\t", requestMessage.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}"))}>\n" +
                      $"{requestMessage.Content?.ReadAsStringAsync().Result}" +
                      $"=====\n" +
                      $"Response:\n" +
                      $"{response.Version}\n" +
                      $"Headers:\n\t{string.Join("\n\t", response.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}"))}>\n" +
                      $"{response.Content.ReadAsStringAsync().Result}");
            return new(HttpStatusCode.InternalServerError);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return new(HttpStatusCode.InternalServerError);
        }
    }
}