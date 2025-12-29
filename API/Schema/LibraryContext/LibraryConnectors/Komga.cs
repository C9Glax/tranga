using Newtonsoft.Json.Linq;

namespace API.Schema.LibraryContext.LibraryConnectors;

public sealed class Komga(string baseUrl, string auth) : LibraryConnector(LibraryType.Komga, baseUrl, auth)
{
    private readonly HttpClient _httpClient = new ()
    {
        DefaultRequestHeaders =
        {
            { "X-API-Key", auth }
        }
    };
    
    public override async Task UpdateLibrary(CancellationToken ct)
    {
        Log.Debug("Updating Libraries...");
        List<string> libraryIds = await GetLibraryIds(ct);
        foreach (string libraryId in libraryIds)
        {
            if (await _httpClient.PostAsync(BuildUri($"api/v1/libraries/{libraryId}/scan"), null, ct) is { IsSuccessStatusCode: false } res)
            {
                Log.ErrorFormat("Unable to update library {0}: {1} {3} {2}", libraryId, res.StatusCode, res.Content.ReadAsStringAsync(ct), res.RequestMessage?.RequestUri);
            }
        }
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private async Task<List<string>> GetLibraryIds(CancellationToken ct)
    {
        Log.Debug("Getting Libraries...");
        if (await _httpClient.GetStringAsync(BuildUri("api/v1/libraries"), ct) is not { } responseData)
        {
            Log.Error("Unable to fetch libraries");
            return [];
        }

        JArray librariesJson = JArray.Parse(responseData);
        return librariesJson.SelectTokens("id").Values<string>().ToList()!;
    }

    internal override async Task<bool> Test(CancellationToken ct)
    {
        Log.Debug("Testing...");
        if (await _httpClient.GetAsync(BuildUri("api/v2/users/me"), ct) is { IsSuccessStatusCode: false } res)
        {
            Log.ErrorFormat("Unable to fetch account: {0} {2} {1}", res.StatusCode, await res.Content.ReadAsStringAsync(ct), res.RequestMessage?.RequestUri);
            return false;
        }

        return true;
    }
}