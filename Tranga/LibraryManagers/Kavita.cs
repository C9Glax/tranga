using System.Text.Json.Nodes;
using Logging;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryManagers;

public class Kavita : LibraryManager
{

    public Kavita(string baseUrl, string username, string password, Logger? logger) : base(baseUrl, GetToken(baseUrl, username, password), logger, LibraryType.Kavita)
    {
    }
    
    [JsonConstructor]
    public Kavita(string baseUrl, string auth, Logger? logger) : base(baseUrl, auth, logger, LibraryType.Kavita)
    {
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
        logger?.WriteLine(this.GetType().ToString(), $"Updating Libraries");
        foreach (KavitaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/Library/scan?libraryId={lib.id}", "Bearer", auth, logger);
    }
    
    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private IEnumerable<KavitaLibrary> GetLibraries()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Libraries");
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/Library", "Bearer", auth, logger);
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