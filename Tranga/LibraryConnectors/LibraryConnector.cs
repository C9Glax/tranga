using System.Net;
using System.Net.Http.Headers;
using Logging;

namespace Tranga.LibraryConnectors;

public abstract class LibraryConnector : GlobalBase
{
    public enum LibraryType : byte
    {
        Komga = 0,
        Kavita = 1
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public LibraryType libraryType { get; }
    public string baseUrl { get; }
    // ReSharper disable once MemberCanBeProtected.Global
    public string auth { get; } //Base64 encoded, if you use your password everywhere, you have problems
    
    protected LibraryConnector(GlobalBase clone, string baseUrl, string auth, LibraryType libraryType) : base(clone)
    {
        Log($"Creating libraryConnector {Enum.GetName(libraryType)}");
        if (!baseUrlRex.IsMatch(baseUrl))
            throw new ArgumentException("Base url does not match pattern");
        if(auth == "")
            throw new ArgumentNullException(nameof(auth), "Auth can not be empty");
        this.baseUrl = baseUrlRex.Match(baseUrl).Value;
        this.auth = auth;
        this.libraryType = libraryType;
    }
    public abstract void UpdateLibrary();
    internal abstract bool Test();

    protected static class NetClient
    {
        public static Stream MakeRequest(string url, string authScheme, string auth, Logger? logger)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, auth);
            
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            try
            {

                HttpResponseMessage response = client.Send(requestMessage);
                logger?.WriteLine("LibraryManager.NetClient",
                    $"GET {url} -> {(int)response.StatusCode}: {response.ReasonPhrase}");

                if (response.StatusCode is HttpStatusCode.Unauthorized &&
                    response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                    return MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, logger);
                else if (response.IsSuccessStatusCode)
                    return response.Content.ReadAsStream();
                else
                    return Stream.Null;
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case HttpRequestException:
                        logger?.WriteLine("LibraryManager.NetClient", $"Failed to make Request:\n\r{e}\n\rContinuing.");
                        break;
                    default:
                        throw;
                }
                return Stream.Null;
            }
        }

        public static bool MakePost(string url, string authScheme, string auth, Logger? logger)
        {
            HttpClient client = new()
            {
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue(authScheme, auth).ToString() }
                }
            };
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            HttpResponseMessage response = client.Send(requestMessage);
            logger?.WriteLine("LibraryManager.NetClient", $"POST {url} -> {(int)response.StatusCode}: {response.ReasonPhrase}");
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, logger);
            else if (response.IsSuccessStatusCode)
                return true;
            else 
                return false;
        }
    }
}