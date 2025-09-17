using System.Net;
using System.Net.Http.Headers;
using log4net;
using HttpMethod = System.Net.Http.HttpMethod;

namespace API.Schema.LibraryContext.LibraryConnectors;

public class NetClient
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(NetClient));
    private static readonly HttpClient Client = new();

    public static async Task<Stream> MakeRequest(string url, string authScheme, string auth, HttpMethod? method = null, CancellationToken? cancellationToken = null)
    {
        Log.Debug($"Requesting {url}");
        method ??= HttpMethod.Get;
        CancellationToken ct = cancellationToken ?? CancellationToken.None;
        Client.DefaultRequestHeaders.Authorization = new (authScheme, auth);
        try
        {
            HttpRequestMessage requestMessage = new()
            {
                Method = method,
                RequestUri = new (url),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue(authScheme, auth).ToString() }
                }
            };
            
            HttpResponseMessage response = await Client.SendAsync(requestMessage, ct);

            if (response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage?.RequestUri?.AbsoluteUri is { } absoluteUri && absoluteUri != url)
                return await MakeRequest(absoluteUri, authScheme, auth, method, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStreamAsync(ct);
            return Stream.Null;
        }
        catch (Exception e)
        {
            switch (e)
            {
                case HttpRequestException:
                    Log.Debug(e);
                    break;
                default:
                    throw;
            }
            Log.Info("Failed to make request");
            return Stream.Null;
        }
    }
}