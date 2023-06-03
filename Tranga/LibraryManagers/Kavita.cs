using System.Text.Json;
using System.Text.Json.Nodes;
using Logging;

namespace Tranga.LibraryManagers;

public class Kavita : LibraryManager
{
    public Kavita(string baseUrl, string username, string password, Logger? logger)
        : base(baseUrl, Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")), logger)
    {
        this.libraryType = LibraryType.Kavita;
    }
    
    public Kavita(string baseUrl, string auth, Logger? logger) : base(baseUrl, auth, logger)
    {
        this.libraryType = LibraryType.Kavita;
    }

    public override void UpdateLibrary()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Updating Libraries");
        foreach (KavitaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/Library/scan?libraryId={lib.id}", auth, logger);
    }
    
    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private IEnumerable<KavitaLibrary> GetLibraries()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Libraries");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/Library", auth, logger);
        if (data == Stream.Null)
        {
            logger?.WriteLine(this.GetType().ToString(), $"No libraries returned");
            return Array.Empty<KavitaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            logger?.WriteLine(this.GetType().ToString(), $"No libraries returned");
            return Array.Empty<KavitaLibrary>();
        }

        HashSet<KavitaLibrary> ret = new();

        foreach (JsonNode? jsonNode in result)
        {
            var jObject = (JsonObject?)jsonNode;
            string libraryId = jObject!["id"]!.GetValue<string>();
            string libraryName = jObject!["name"]!.GetValue<string>();
            ret.Add(new KavitaLibrary(libraryId, libraryName));
        }

        return ret;
    }
    
    private struct KavitaLibrary
    {
        public string id { get; }
        public string name { get; }

        public KavitaLibrary(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}