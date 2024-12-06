using System.Net;
using System.Net.Http.Headers;
using log4net;

namespace Tranga.LibraryConnectors;

internal static class NetClient
{
    private static readonly ILog log = LogManager.GetLogger(typeof(NetClient));

    private static bool Send(HttpMethod method, string url, string authScheme, string auth, out Stream? stream)
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, auth);
        HttpRequestMessage requestMessage = new()
        {
            Method = method,
            RequestUri = new Uri(url)
        };
        HttpResponseMessage response = client.Send(requestMessage);
        log.Info($"{method} {url} {(int)response.StatusCode}");
        if (response.StatusCode is HttpStatusCode.Unauthorized &&
            !response.RequestMessage!.RequestUri!.AbsoluteUri.Equals(url))
        {
            log.Info("Redirected");
            return Send(method, response.RequestMessage!.RequestUri!.AbsoluteUri, authScheme, auth, out stream);
        }else if (!response.IsSuccessStatusCode)
        {
            stream = null;
            log.Info("Not successful.");
            return false;
        }
        else
        {
            stream = response.Content.ReadAsStream();
            return true;
        }
    }

    public static bool MakeRequest(string url, string authScheme, string auth, out Stream? stream) =>
        Send(HttpMethod.Get, url, authScheme, auth, out stream);

    public static bool MakePost(string url, string authScheme, string auth) =>
        Send(HttpMethod.Post, url, authScheme, auth, out Stream? _);
}