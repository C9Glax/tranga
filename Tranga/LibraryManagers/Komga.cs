using System.Text.Json.Nodes;
using Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryManagers;

/// <summary>
/// Provides connectivity to Komga-API
/// Can fetch and update libraries
/// </summary>
public class Komga : LibraryManager
{
    public Komga(string baseUrl, string username, string password, Logger? logger)
        : base(baseUrl, Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")), logger)
    {
        this.libraryType = LibraryType.Komga;
    }

    [JsonConstructor]
    public Komga(string baseUrl, string auth, Logger? logger) : base(baseUrl, auth, logger)
    {
        this.libraryType = LibraryType.Komga;
    }

    public override void UpdateLibrary()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Updating Libraries");
        foreach (KomgaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/v1/libraries/{lib.id}/scan", auth, logger);
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private IEnumerable<KomgaLibrary> GetLibraries()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Libraries");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries", auth, logger);
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

        return ret;
    }

    private struct KomgaLibrary
    {
        public string id { get; }
        public string name { get; }

        public KomgaLibrary(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}