using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace API.Schema.LibraryContext.LibraryConnectors;

public class Kavita(string baseUrl, string auth) : LibraryConnector(LibraryType.Kavita, baseUrl, auth)
{
    public Kavita(string baseUrl, string username, string password) : 
        this(baseUrl, GetToken(baseUrl, username, password))
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
        catch (HttpRequestException)
        {
            
        }
        return "";
    }

    public override async Task UpdateLibrary(CancellationToken ct)
    {
        try
        {
            foreach (KavitaLibrary lib in await GetLibraries(ct))
                await NetClient.MakeRequest($"{BaseUrl}/api/ToFileLibrary/scan?libraryId={lib.Id}", "Bearer", Auth, HttpMethod.Post, ct);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    internal override async Task<bool> Test(CancellationToken ct)
    {
        foreach (KavitaLibrary lib in await GetLibraries(ct))
            if (await NetClient.MakeRequest($"{BaseUrl}/api/ToFileLibrary/scan?libraryId={lib.Id}", "Bearer", Auth, HttpMethod.Post, ct) is { CanRead: true })
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private async Task<IEnumerable<KavitaLibrary>> GetLibraries(CancellationToken ct)
    {
        if(await NetClient.MakeRequest($"{BaseUrl}/api/ToFileLibrary/libraries", "Bearer", Auth, HttpMethod.Get, ct) is not { CanRead: true } data)
        {
            Log.Info("No libraries found");
            return [];
        }
        if(await JsonSerializer.DeserializeAsync<KavitaLibrary[]>(data, JsonSerializerOptions.Web, ct) is not { } ret)
        {
            Log.Debug("Parsing libraries failed.");
            return [];
        }

        return ret;
    }
    
    private struct KavitaLibrary
    {
        [JsonProperty("id")]
        public required int Id { get; init; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [JsonProperty("name")]
        public required string Name { get; init; }
    }
}