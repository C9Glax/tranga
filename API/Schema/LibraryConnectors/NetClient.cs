using System.Net;
using System.Net.Http.Headers;

namespace API.Schema.LibraryConnectors;

public class NetClient
{
    public static Stream MakeRequest(string url, string authScheme, string auth)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authScheme, auth);
            
            HttpRequestMessage requestMessage = new ()
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
                        
                        break;
                    default:
                        throw;
                }
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