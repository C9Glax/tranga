using System.Text.Json;
using System.Text.Json.Nodes;

namespace API.Schema.LibraryContext.LibraryConnectors;

public class Komga : LibraryConnector
{
    public Komga(string baseUrl, string auth) : base(LibraryType.Komga, baseUrl, auth)
    {
    }
    
    public Komga(string baseUrl, string username, string password)
        : this(baseUrl, Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")))
    {
    }
    
    protected override void UpdateLibraryInternal()
    {
        foreach (KomgaLibrary lib in GetLibraries())
            NetClient.MakePost($"{BaseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", Auth);
    }

    internal override bool Test()
    {
        foreach (KomgaLibrary lib in GetLibraries())
            if (NetClient.MakePost($"{BaseUrl}/api/v1/libraries/{lib.id}/scan", "Basic", Auth))
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private IEnumerable<KomgaLibrary> GetLibraries()
    {
        Stream data = NetClient.MakeRequest($"{BaseUrl}/api/v1/libraries", "Basic", Auth);
        if (data == Stream.Null)
        {
            Log.Info("No libraries found");
            return [];
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            Log.Info("No libraries found");
            return [];
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