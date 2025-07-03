using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.MangaDownloadClients;

public class FlareSolverrDownloadClient : DownloadClient
{
    
    
    internal override RequestResult MakeRequestInternal(string url, string? referrer = null, string? clickButton = null)
    {
        if (clickButton is not null)
            Log.Warn("Client can not click button");
        if(referrer is not null)
            Log.Warn("Client can not set referrer");
        if (Tranga.Settings.FlareSolverrUrl == string.Empty)
        {
            Log.Error("FlareSolverr URL is empty");
            return new(HttpStatusCode.InternalServerError, null, Stream.Null);
        }
        
        Uri flareSolverrUri = new (Tranga.Settings.FlareSolverrUrl);
        if (flareSolverrUri.Segments.Last() != "v1")
            flareSolverrUri = new UriBuilder(flareSolverrUri)
            {
                Path = "v1"
            }.Uri;
        
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            DefaultRequestHeaders = { { "User-Agent", Tranga.Settings.UserAgent } }
        };

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
        Log.Debug($"Requesting {url}");
        
        HttpResponseMessage? response;
        try
        {
            response = client.Send(requestMessage);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return new (HttpStatusCode.Unused, null, Stream.Null);
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
            return new (response.StatusCode,  null, Stream.Null);
        }

        string responseString = response.Content.ReadAsStringAsync().Result;
        JObject responseObj = JObject.Parse(responseString);
        if (!IsInCorrectFormat(responseObj, out string? reason))
        {
            Log.Error($"Wrong format: {reason}");
            return new(HttpStatusCode.Unused, null, Stream.Null);
        }

        string statusResponse = responseObj["status"]!.Value<string>()!;
        if (statusResponse != "ok")
        {
            Log.Debug($"Status is not ok: {statusResponse}");
            return new(HttpStatusCode.Unused, null, Stream.Null);
        }
        JObject solution = (responseObj["solution"] as JObject)!;

        if (!Enum.TryParse(solution["status"]!.Value<int>().ToString(), out HttpStatusCode statusCode))
        {
            Log.Error($"Wrong format: Cant parse status code: {solution["status"]!.Value<int>()}");
            return new(HttpStatusCode.Unused, null, Stream.Null);
        }
        if (statusCode < HttpStatusCode.OK || statusCode >= HttpStatusCode.MultipleChoices)
        {
            Log.Debug($"Status is: {statusCode}");
            return new(statusCode, null, Stream.Null);
        }

        if (solution["response"]!.Value<string>() is not { } htmlString)
        {
            Log.Error("Wrong format: Cant find response in solution");
            return new(HttpStatusCode.Unused, null, Stream.Null);
        }

        if (IsJson(htmlString, out HtmlDocument document, out string? json))
        {
            MemoryStream ms = new();
            ms.Write(Encoding.UTF8.GetBytes(json));
            ms.Position = 0;
            return new(statusCode, document, ms);
        }
        else
        {
            MemoryStream ms = new();
            ms.Write(Encoding.UTF8.GetBytes(htmlString));
            ms.Position = 0;
            return new(statusCode, document, ms);
        }
    }

    private bool IsInCorrectFormat(JObject responseObj, [NotNullWhen(false)]out string? reason)
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

    private bool IsJson(string htmlString, out HtmlDocument document, [NotNullWhen(true)]out string? jsonString)
    {
        jsonString = null;
        document = new();
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