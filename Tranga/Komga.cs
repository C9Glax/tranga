using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga;

public class Komga
{
    [System.Text.Json.Serialization.JsonRequired]public string baseUrl { get; }
    [System.Text.Json.Serialization.JsonRequired]public string auth { get; }

    public Komga(string baseUrl, string username, string password)
    {
        this.baseUrl = baseUrl;
        this.auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
    }

    [JsonConstructor]
    public Komga(string baseUrl, string auth)
    {
        this.baseUrl = baseUrl;
        this.auth = auth;
    }

    public KomgaLibrary[] GetLibraries()
    {
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries", auth);
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
            return Array.Empty<KomgaLibrary>();

        HashSet<KomgaLibrary> ret = new();

        foreach (JsonNode jsonNode in result)
        {
            var jObject = (JsonObject?)jsonNode;
            string libraryId = jObject!["id"]!.GetValue<string>();
            string libraryName = jObject!["name"]!.GetValue<string>();
            ret.Add(new KomgaLibrary(libraryId, libraryName));
        }

        return ret.ToArray();
    }

    public bool UpdateLibrary(string libraryId)
    {
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
            HttpClient client = new();
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue("Basic", auth).ToString() }
                }
            };
            HttpResponseMessage response = client.Send(requestMessage);
            Stream resultString = response.IsSuccessStatusCode ? response.Content.ReadAsStream() : Stream.Null;
            return resultString;
        }

        public static bool MakePost(string url, string auth)
        {
            HttpClient client = new();
            HttpRequestMessage requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue("Basic", auth).ToString() }
                }
            };
            HttpResponseMessage response = client.Send(requestMessage);
            return response.IsSuccessStatusCode;
        }
    }
}