using System.Text.Json;
using System.Text.Json.Nodes;

namespace API.Schema.LibraryConnectors;

public class Komga(string baseUrl, string auth)
    : LibraryConnector(TokenGen.CreateToken(typeof(Komga), 64), LibraryType.Komga, baseUrl, auth)
{
    protected override void UpdateLibraryInternal()
    {
        foreach (KomgaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", auth);
    }

    internal override bool Test()
    {
        foreach (KomgaLibrary lib in GetLibraries())
            if (NetClient.MakePost($"{baseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", auth))
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private IEnumerable<KomgaLibrary> GetLibraries()
    {
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/v1/libraries", "Basic", auth);
        if (data == Stream.Null)
        {
            return Array.Empty<KomgaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
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