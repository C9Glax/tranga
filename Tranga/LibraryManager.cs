using System.Net;
using System.Net.Http.Headers;
using Logging;

namespace Tranga;

public abstract class LibraryManager
{
    public string baseUrl { get; }
    protected string auth { get; } //Base64 encoded, if you use your password everywhere, you have problems
    protected Logger? logger;
    
    /// <param name="baseUrl">Base-URL of Komga instance, no trailing slashes(/)</param>
    /// <param name="auth">Base64 string of username and password (username):(password)</param>
    /// <param name="logger"></param>
    protected LibraryManager(string baseUrl, string auth, Logger? logger)
    {
        this.baseUrl = baseUrl;
        this.auth = auth;
        this.logger = logger;
    }
    public abstract void UpdateLibrary();

    public void AddLogger(Logger newLogger)
    {
        this.logger = newLogger;
    }

    protected static class NetClient
    {
        public static Stream MakeRequest(string url, string auth, Logger? logger)
        {
            HttpClientHandler clientHandler = new ();
            HttpClient client = new(clientHandler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            logger?.WriteLine("LibraryManager", $"GET {url}");
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager", $"{(int)response.StatusCode} {response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, auth, logger);
            else if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStream();
            else
                return Stream.Null;
        }

        public static bool MakePost(string url, string auth, Logger? logger)
        {
            HttpClientHandler clientHandler = new ();
            HttpClient client = new(clientHandler)
            {
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue("Basic", auth).ToString() }
                }
            };
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            logger?.WriteLine("LibraryManager", $"POST {url}");
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager", $"{(int)response.StatusCode} {response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, auth, logger);
            else if (response.IsSuccessStatusCode)
                return true;
            else 
                return false;
        }
    }
}