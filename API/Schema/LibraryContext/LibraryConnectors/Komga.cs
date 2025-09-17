using System.Text.Json;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace API.Schema.LibraryContext.LibraryConnectors;

public class Komga(string baseUrl, string auth) : LibraryConnector(LibraryType.Komga, baseUrl, auth)
{
    public Komga(string baseUrl, string username, string password)
        : this(baseUrl, Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}")))
    {
    }
    
    public override async Task UpdateLibrary(CancellationToken ct)
    {
        try
        {
            foreach (KomgaLibrary lib in  await GetLibraries(ct))
                await NetClient.MakeRequest($"{BaseUrl}/api/v1/libraries/{lib.Id}/scan", "Basic", Auth, HttpMethod.Post, ct);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    internal override async Task<bool> Test(CancellationToken ct)
    {
        foreach (KomgaLibrary lib in await GetLibraries(ct))
            if (await NetClient.MakeRequest($"{BaseUrl}/api/v1/libraries/{lib.Id}/scan", "Basic", Auth, HttpMethod.Post, ct) is { CanRead: true})
                return true;
        return false;
    }

    /// <summary>
    /// Fetches all libraries available to the user
    /// </summary>
    /// <returns>Array of KomgaLibraries</returns>
    private async Task<IEnumerable<KomgaLibrary>> GetLibraries(CancellationToken ct)
    {
        if (await NetClient.MakeRequest($"{BaseUrl}/api/v1/libraries", "Basic", Auth, HttpMethod.Get, ct) is not { CanRead: true } data)
        {
            Log.Debug("No libraries found");
            return [];
        }

        if (await JsonSerializer.DeserializeAsync<KomgaLibrary[]>(data, JsonSerializerOptions.Web, ct) is not
            { } ret)
        {
            Log.Debug("Parsing libraries failed.");
            return [];
        }

        return ret ;
    }

    private readonly record struct KomgaLibrary
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [JsonProperty("name")]
        public required string Name { get; init; }
    }
}