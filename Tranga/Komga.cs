using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga;

/// <summary>
/// Provides connectivity to Komga-API
/// Can fetch and update libraries
/// </summary>
public class Komga
{
    public string baseUrl { get; }
    public string auth { get; } //Base64 encoded, if you use your password everywhere, you have problems

    private Logger? logger;
    
    /// <param name="baseUrl">Base-URL of Komga instance, no trailing slashes(/)</param>
    /// <param name="username">Komga Username</param>
    /// <param name="password">Komga password, will be base64 encoded. yea</param>
    public Komga(string baseUrl, string username, string password, Logger? logger)
    {
        this.baseUrl = baseUrl;
        this.auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
        this.logger = logger;
    }
    
    /// <param name="baseUrl">Base-URL of Komga instance, no trailing slashes(/)</param>
    /// <param name="auth">Base64 string of username and password (username):(password)</param>
    [JsonConstructor]
    public Komga(string baseUrl, string auth, Logger? logger)
    {
        this.baseUrl = baseUrl;
        this.auth = auth;
        this.logger = logger;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    public KomgaLibrary[] GetLibraries()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Libraries");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries", auth);
        if (data == Stream.Null)
        {
            logger?.WriteLine(this.GetType().ToString(), $"No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            logger?.WriteLine(this.GetType().ToString(), $"No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }

        HashSet<KomgaLibrary> ret = new();

        foreach (JsonNode? jsonNode in result)
        {
            var jObject = (JsonObject?)jsonNode;
            string libraryId = jObject!["id"]!.GetValue<string>();
            string libraryName = jObject!["name"]!.GetValue<string>();
            ret.Add(new KomgaLibrary(libraryId, libraryName));
        }

        return ret.ToArray();
    }

    /// <summary>
    /// Updates library with given id
    /// </summary>
    /// <param name="libraryId">Id of the Komga-Library</param>
    /// <returns>true if successful</returns>
    public bool UpdateLibrary(string libraryId)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Updating Libraries");
        return NetClient.MakePost($"{baseUrl}/api/v1/libraries/{libraryId}/scan", auth);
    }

    public struct KomgaLibrary
    {
        public string id { get; }
        public string name { get; }

        public KomgaLibrary(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }

    private static class NetClient
    {
        public static Stream MakeRequest(string url, string auth)
        {
            HttpClientHandler clientHandler = new ();
            clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
            HttpClient client = new(clientHandler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
            
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            HttpResponseMessage response = client.Send(requestMessage);
            Stream ret;
            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                ret = MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, auth);
            }else
                return response.IsSuccessStatusCode ? response.Content.ReadAsStream() : Stream.Null;
            return ret;
        }

        public static bool MakePost(string url, string auth)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
            HttpClient client = new(clientHandler)
            {
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue("Basic", auth).ToString() }
                }
            };
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            HttpResponseMessage response = client.Send(requestMessage);
            bool ret;
            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                ret = MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, auth);
            }else
                return response.IsSuccessStatusCode;
            return ret;
        }
    }
}