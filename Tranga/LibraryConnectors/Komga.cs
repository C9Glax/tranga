using System.Text.Json.Nodes;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryConnectors;

/// <summary>
/// Provides connectivity to Komga-API
/// Can fetch and update libraries
/// </summary>
public class Komga(API.Schema.LibraryConnectors.LibraryConnector info) : LibraryConnector(info)
{
    public static string GetAuth(string username, string password) =>
        Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));

    public override void UpdateLibrary()
    {
        log.Info("Updating libraries.");
        foreach (KomgaLibrary lib in GetLibraries())
            NetClient.MakePost($"{info.BaseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", info.Auth);
    }

    public override bool Test()
    {
        foreach (KomgaLibrary lib in GetLibraries())
            if (NetClient.MakePost($"{info.BaseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", info.Auth))
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private IEnumerable<KomgaLibrary> GetLibraries()
    {
        log.Info("Getting Libraries");
        
        if (!NetClient.MakeRequest($"{info.BaseUrl}/api/v1/libraries", "Basic", info.Auth, out Stream? data) || data is null)
        {
            log.Info("No libraries returned");
            return Array.Empty<KomgaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            log.Info("No libraries returned");
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