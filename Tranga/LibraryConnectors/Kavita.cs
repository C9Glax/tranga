using System.Text.Json.Nodes;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.LibraryConnectors;

public class Kavita(API.Schema.LibraryConnectors.LibraryConnector info) : LibraryConnector(info)
{
    public string GetAuth(string username, string password)
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
            RequestUri = new Uri($"{info.BaseUrl}/api/Account/login"),
            Content = new StringContent($"{{\"username\":\"{username}\",\"password\":\"{password}\"}}", System.Text.Encoding.UTF8, "application/json")
        };
        try
        {
            HttpResponseMessage response = client.Send(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                JsonObject? result = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStream());
                if (result is not null)
                    return result["token"]!.GetValue<string>();
            }
        }
        catch (HttpRequestException e)
        {
            
        }
        return "";
    }

    public override void UpdateLibrary()
    {
        log.Info("Updating libraries.");
        foreach (KavitaLibrary lib in GetLibraries())
            NetClient.MakePost($"{info.BaseUrl}/api/Library/scan?libraryId={lib.id}", "Bearer", info.Auth);
    }

    public override bool Test()
    {
        foreach (KavitaLibrary lib in GetLibraries())
            if (NetClient.MakePost($"{info.BaseUrl}/api/Library/scan?libraryId={lib.id}", "Bearer", info.Auth))
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private IEnumerable<KavitaLibrary> GetLibraries()
    {
        log.Info("Getting libraries.");

        if (!NetClient.MakeRequest($"{info.BaseUrl}/api/Library/libraries", "Bearer", info.Auth, out Stream? data) || data is null)
        {
            log.Info("No libraries returned");
            return Array.Empty<KavitaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
            log.Info("No libraries returned");
            return Array.Empty<KavitaLibrary>();
        }

        List<KavitaLibrary> ret = new();

        foreach (JsonNode? jsonNode in result)
        {
            JsonObject? jObject = (JsonObject?)jsonNode;
            if(jObject is null)
                continue;
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