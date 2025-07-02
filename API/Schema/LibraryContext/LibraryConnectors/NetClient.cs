using System.Net;
using System.Net.Http.Headers;
using log4net;

namespace API.Schema.LibraryContext.LibraryConnectors;

public class NetClient
{
    private static ILog Log = LogManager.GetLogger(typeof(NetClient));

    public static Stream MakeRequest(string url, string authScheme, string auth)
    {
        Log.Debug($"Requesting {url}");
        HttpClient client = new();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, auth);

        HttpRequestMessage requestMessage = new()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url)
        };
        try
        {
            HttpResponseMessage response = client.Send(requestMessage);

            if (response.StatusCode is HttpStatusCode.Unauthorized &&
                response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakeRequest(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth);
            else if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStream();
            else
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

    public static bool MakePost(string url, string authScheme, string auth)
        {
            HttpClient client = new()
            {
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" },
                    { "Authorization", new AuthenticationHeaderValue(authScheme, auth).ToString() }
                }
            };
            HttpRequestMessage requestMessage = new ()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            HttpResponseMessage response = client.Send(requestMessage);
            
            if(response.StatusCode is HttpStatusCode.Unauthorized && response.RequestMessage!.RequestUri!.AbsoluteUri != url)
                return MakePost(response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth);
            else if (response.IsSuccessStatusCode)
                return true;
            else 
                return false;
        }
}