using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tranga;

public class Komga
{
    private string baseUrl { get; }

    public Komga(string baseUrl)
    {
        this.baseUrl = baseUrl;
    }

    public KomgaLibrary[] GetLibraries()
    {
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries");
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
        return NetClient.MakePost($"{baseUrl}/api/v1/libraries/{libraryId}/scan");
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
        public static Stream MakeRequest(string url)
        {
            HttpClient client = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Get, url);
            HttpResponseMessage response = client.Send(requestMessage);
            Stream resultString = response.IsSuccessStatusCode ? response.Content.ReadAsStream() : Stream.Null;
            return resultString;
        }

        public static bool MakePost(string url)
        {
            HttpClient client = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Post, url);
            HttpResponseMessage response = client.Send(requestMessage);
            return response.IsSuccessStatusCode;
        }
    }
}