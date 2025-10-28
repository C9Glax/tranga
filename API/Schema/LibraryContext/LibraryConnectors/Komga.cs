using Newtonsoft.Json.Linq;

namespace API.Schema.LibraryContext.LibraryConnectors;

public sealed class Komga(string baseUrl, string auth) : LibraryConnector(LibraryType.Komga, baseUrl, auth)
{
    private readonly HttpClient _httpClient = new HttpClient()
    {
        BaseAddress = new Uri(baseUrl),
        DefaultRequestHeaders =
        {
            { "X-API-Key", auth }
        }
    };
    
    public override async Task UpdateLibrary(CancellationToken ct)
    {
        List<string> libraryIds = await GetLibraryIds(ct);
        foreach (string libraryId in libraryIds)
        {
            await _httpClient.PostAsync($"/api/v1/libraries/{libraryId}/scan", null, ct);
        }
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private async Task<List<string>> GetLibraryIds(CancellationToken ct)
    {
        if (await _httpClient.GetStringAsync("/api/v1/libraries", ct) is not { } responseData)
        {
            Log.Error("Unable to fetch libraries");
            return [];
        }

        JArray librariesJson = JArray.Parse(responseData);
        return librariesJson.SelectTokens("id").Values<string>().ToList()!;
    }

    internal override async Task<bool> Test(CancellationToken ct)
    {
        if (await _httpClient.GetAsync("/api/v2/users/me", ct) is not { IsSuccessStatusCode: true })
        {
            Log.Error("Unable to fetch account");
            return false;
        }

        return true;
    }
}