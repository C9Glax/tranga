using System.Text.Json.Nodes;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryConnectors;

/// <summary>
/// Provides connectivity to Komga-API
/// Can fetch and update libraries
/// </summary>
public class Komga : LibraryConnector
{
    public Komga(string baseUrl, string username, string password, GlobalBase clone)
        : base(baseUrl, Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")), LibraryType.Komga, clone)
    {
    }

    [JsonConstructor]
    public Komga(string baseUrl, string auth, GlobalBase clone) : base(baseUrl, auth, LibraryType.Komga, clone)
    {
    }

    public override void UpdateLibrary()
    {
        Log("Updating libraries.");
        foreach (KomgaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", auth, logger);
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private IEnumerable<KomgaLibrary> GetLibraries()
    {
        Log("Getting Libraries");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries", "Basic", auth, logger);
        if (data == Stream.Null)
        {
            Log("No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            Log("No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }

        HashSet<KomgaLibrary> ret = new();

        foreach (JsonNode? jsonNode in result)
        {
            var jObject = (JsonObject?)jsonNode;
            string libraryId = jObject!["id"]!.GetValue<string>();
            string libraryName = jObject["name"]!.GetValue<string>();
            ret.Add(new KomgaLibrary(libraryId, libraryName));
        }

        return ret;
    }

    private struct KomgaLibrary
    {
        public string id { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string name { get; }

        public KomgaLibrary(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}