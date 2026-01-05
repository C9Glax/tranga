using System.Net.Http.Headers;
using API.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.Schema.LibraryContext.LibraryConnectors;

public class Kavita(string baseUrl, string auth) : LibraryConnector(LibraryType.Kavita, baseUrl, auth)
{
    private readonly HttpClient _netClient = new HttpClient()
    {
        DefaultRequestHeaders =
        {
            Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
        }
    };
    
    /// <summary>
    /// Get a new JWT Token
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ParsingException"></exception>
    private async Task<string> GetToken()
    {
        Log.Debug("Getting Token...");
        string apiKey = $"apiKey={Auth}";
        string pluginName = "pluginName=Tranga";
        string path = $"/api/Plugin/authenticate?{apiKey}&{pluginName}";

        if (await _netClient.PostAsync(BuildUri(path), null) is not { IsSuccessStatusCode: true } responseMessage)
        {
            throw new ParsingException("Could not connect to the Library instance");
        }
        
        if(JObject.Parse(await responseMessage.Content.ReadAsStringAsync()) is not { } data)
        {
            throw new ParsingException("Could not parse the response");
        }

        return data.TryGetValue("token", out JToken? token) ? token.Value<string>()! : throw new ParsingException("Could not parse the response");
    }

    /// <summary>
    /// Refreshes the JWT Token
    /// </summary>
    private async Task RefreshAuth()
    {
        Log.Debug("Refreshing auth...");
        string token = await GetToken();
        _netClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public override async Task UpdateLibrary(CancellationToken ct)
    {
        Log.Debug("Updating Libraries...");
        List<int> ids = await GetLibraries(ct);
        JObject requestData = new () { { "ids", JsonConvert.SerializeObject(ids) } };

        await RefreshAuth();
        await _netClient.PostAsJsonAsync(BuildUri("/api/Library/scan-multiple"), requestData, ct);
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KavitaLibrary</returns>
    private async Task<List<int>> GetLibraries(CancellationToken ct)
    {
        Log.Debug("Getting Libraries...");
        await RefreshAuth();
        if(await _netClient.GetStringAsync(BuildUri("/api/Library/libraries"), ct) is not { } responseData)
        {
            Log.Error("Unable to fetch libraries");
            return [];
        }

        JArray librariesJson = JArray.Parse(responseData);
        return librariesJson.SelectTokens("id").Values<int>().ToList();
    }

    internal override async Task<bool> Test(CancellationToken ct)
    {
        Log.Debug("Testing...");
        await RefreshAuth();
        if(await _netClient.GetAsync(BuildUri("/api/Account"), ct) is not { IsSuccessStatusCode: true })
        {
            Log.Error("Unable to fetch account");
            return false;
        }

        return true;
    }
}