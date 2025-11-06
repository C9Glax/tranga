using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.MangaDownloadClients;

public class FlareSolverrDownloadClient(HttpClient client) : IDownloadClient
{
    private ILog Log { get; } = LogManager.GetLogger(typeof(FlareSolverrDownloadClient));

    public async Task<HttpResponseMessage> MakeRequest(string url, RequestType requestType, string? referrer = null, CancellationToken? cancellationToken = null)
    {
        Log.DebugFormat("Using {0} for {1}", typeof(FlareSolverrDownloadClient).FullName, url);
        if(referrer is not null)
            Log.Warn("Client can not set referrer");
        if (Tranga.Settings.FlareSolverrUrl == string.Empty)
        {
            Log.Error("FlareSolverr URL is empty");
            return new(HttpStatusCode.InternalServerError);
        }
        
        Uri flareSolverrUri = new (Tranga.Settings.FlareSolverrUrl);
        if (flareSolverrUri.Segments.Last() != "v1")
            flareSolverrUri = new UriBuilder(flareSolverrUri)
            {
                Path = "v1"
            }.Uri;
        
        JObject requestObj = new()
        {
            ["cmd"] = "request.get",
            ["url"] = url
        };

        HttpRequestMessage requestMessage = new(HttpMethod.Post, flareSolverrUri)
        {
            Content = new StringContent(JsonConvert.SerializeObject(requestObj)),
        };
        requestMessage.Content.Headers.ContentType = new ("application/json");
        Log.DebugFormat("Requesting {0}", url);
        
        HttpResponseMessage? response;
        try
        {
            response = await client.SendAsync(requestMessage, cancellationToken ?? CancellationToken.None);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return new (HttpStatusCode.InternalServerError);
        }

        if (!response.IsSuccessStatusCode)
        {
            Log.Debug($"Request returned status code {(int)response.StatusCode} {response.StatusCode}:\n" +
                      $"=====\n" +
                      $"Request:\n" +
                      $"{requestMessage.Method} {requestMessage.RequestUri}\n" +
                      $"{requestMessage.Version} {requestMessage.VersionPolicy}\n" +
                      $"Headers:\n\t{string.Join("\n\t", requestMessage.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}"))}>\n" +
                      $"{requestMessage.Content?.ReadAsStringAsync().Result}" +
                      $"=====\n" +
                      $"Response:\n" +
                      $"{response.Version}\n" +
                      $"Headers:\n\t{string.Join("\n\t", response.Headers.Select(h => $"{h.Key}: <{string.Join(">, <", h.Value)}"))}>\n" +
                      $"{response.Content.ReadAsStringAsync().Result}");
            return response;
        }

        string responseString = await response.Content.ReadAsStringAsync(cancellationToken ?? CancellationToken.None);
        JObject responseObj = JObject.Parse(responseString);
        if (!IsInCorrectFormat(responseObj, out string? reason))
        {
            Log.ErrorFormat("Wrong format: {0}", reason);
            return new(HttpStatusCode.InternalServerError);
        }

        string statusResponse = responseObj["status"]!.Value<string>()!;
        if (statusResponse != "ok")
        {
            Log.DebugFormat("Status is not ok: {0}", statusResponse);
            return new(HttpStatusCode.InternalServerError);
        }
        JObject solution = (responseObj["solution"] as JObject)!;

        if (!Enum.TryParse(solution["status"]!.Value<int>().ToString(), out HttpStatusCode statusCode))
        {
            Log.ErrorFormat("Wrong format: Cant parse status code: {0}", solution["status"]!.Value<int>());
            return new(HttpStatusCode.InternalServerError);
        }
        if (statusCode < HttpStatusCode.OK || statusCode >= HttpStatusCode.MultipleChoices)
        {
            Log.DebugFormat("Status is: {0}", statusCode);
            return new (statusCode);
        }

        if (solution["response"]!.Value<string>() is not { } htmlString)
        {
            Log.Error("Wrong format: Cant find response in solution");
            return new(HttpStatusCode.InternalServerError);
        }

        if (IsJson(htmlString, out string? json))
        {
            return new(statusCode) { Content = new StringContent(json) };
        }
        else
        {
            return new(statusCode) { Content = new StringContent(htmlString) };
        }
    }

    private static bool IsInCorrectFormat(JObject responseObj, [NotNullWhen(false)]out string? reason)
    {
        reason = null;
        if (!responseObj.ContainsKey("status"))
        {
            reason = "Cant find status on response";
            return false;
        }

        if (responseObj["solution"] is not JObject solution)
        {
            reason = "Cant find solution";
            return false;
        }

        if (!solution.ContainsKey("status"))
        {
            reason = "Wrong format: Cant find status in solution";
            return false;
        }

        if (!solution.ContainsKey("response"))
        {

            reason = "Wrong format: Cant find response in solution";
            return false;
        }

        return true;
    }

    private static bool IsJson(string htmlString, [NotNullWhen(true)]out string? jsonString)
    {
        jsonString = null;
        HtmlDocument document = new();
        document.LoadHtml(htmlString);

        HtmlNode pre = document.DocumentNode.SelectSingleNode("//pre");
        try
        {
            using JsonDocument _ = JsonDocument.Parse(pre.InnerText);
            jsonString = pre.InnerText;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}