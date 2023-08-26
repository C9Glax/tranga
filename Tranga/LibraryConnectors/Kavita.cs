using System.Text.Json.Nodes;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryConnectors;

public class Kavita : LibraryConnector
{

    public Kavita(GlobalBase clone, string baseUrl, string username, string password) : 
        base(clone, baseUrl, GetToken(baseUrl, username, password), LibraryType.Kavita)
    {
    }
    
    [JsonConstructor]
    public Kavita(GlobalBase clone, string baseUrl, string auth) : base(clone, baseUrl, auth, LibraryType.Kavita)
    {
    }

    public override string ToString()
    {
        return $"Kavita {baseUrl}";
    }

    private static string GetToken(string baseUrl, string username, string password)
    {
        HttpClient client = new()
        {
            DefaultRequestHeaders =
            {
                { "Accept", "application/json" }
            }
        };
        HttpRequestMessage requestMessage = new ()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{baseUrl}/api/Account/login"),
            Content = new StringContent($"{{\"username\":\"{username}\",\"password\":\"{password}\"}}", System.Text.Encoding.UTF8, "application/json")
        };
        
        HttpResponseMessage response = client.Send(requestMessage);
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStream());
        if (result is not null)
            return result["token"]!.GetValue<string>();
        else return "";
    }

    public override void UpdateLibrary()
    {
        Log("Updating libraries.");
        foreach (KavitaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/Library/scan?libraryId={lib.id}", "Bearer", auth, logger);
    }
    
    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private IEnumerable<KavitaLibrary> GetLibraries()
    {
        Log("Getting libraries.");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/Library", "Bearer", auth, logger);
        if (data == Stream.Null)
        {
            Log("No libraries returned");
            return Array.Empty<KavitaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            Log("No libraries returned");
            return Array.Empty<KavitaLibrary>();
        }

        HashSet<KavitaLibrary> ret = new();

        foreach (JsonNode? jsonNode in result)
        {
            var jObject = (JsonObject?)jsonNode;
            int libraryId = jObject!["id"]!.GetValue<int>();
            string libraryName = jObject["name"]!.GetValue<string>();
            ret.Add(new KavitaLibrary(libraryId, libraryName));
        }

        return ret;
    }
    
    private struct KavitaLibrary
    {
        public int id { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string name { get; }

        public KavitaLibrary(int id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}