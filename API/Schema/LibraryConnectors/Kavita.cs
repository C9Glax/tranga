using System.Text.Json;
using System.Text.Json.Nodes;

namespace API.Schema.LibraryConnectors;

public class Kavita(string baseUrl, string auth)
    : LibraryConnector(TokenGen.CreateToken(typeof(Kavita), 64), LibraryType.Kavita, baseUrl, auth)
{
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
        try
        {
            HttpResponseMessage response = client.Send(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                JsonObject? result = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStream());
                if (result is not null)
                    return result["token"]!.GetValue<string>();
            }
            else
            {
            }
        }
        catch (HttpRequestException e)
        {
        }
        return "";
    }

    protected override void UpdateLibraryInternal()
    {
        foreach (KavitaLibrary lib in GetLibraries())
            NetClient.MakePost($"{baseUrl}/api/Library/scan?libraryId={lib.id}", "Bearer", auth);
    }

    internal override bool Test()
    {
        foreach (KavitaLibrary lib in GetLibraries())
            if (NetClient.MakePost($"{baseUrl}/api/Library/scan?libraryId={lib.id}", "Bearer", auth))
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private IEnumerable<KavitaLibrary> GetLibraries()
    {
        Stream data = NetClient.MakeRequest($"{baseUrl}/api/Library/libraries", "Bearer", auth);
        if (data == Stream.Null)
        {
            return Array.Empty<KavitaLibrary>();
        }
        JsonArray? result = JsonSerializer.Deserialize<JsonArray>(data);
        if (result is null)
        {
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