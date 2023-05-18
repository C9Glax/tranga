using System.Net;

namespace Tranga;

public abstract class Connector
{
    public abstract string name { get; }
    public abstract bool GetPublications(out Publication[] publications);
    public abstract bool GetChapters(Publication publication, out Chapter[] chapters);
    public abstract bool DownloadChapter(Chapter chapter); //where to?

    internal class DownloadClient
    {
        static readonly HttpClient client = new HttpClient();
        public RequestResult GetPage(string url)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage response = client.Send(requestMessage);
            Stream resultString = response.IsSuccessStatusCode ? response.Content.ReadAsStream() : Stream.Null;
            return new RequestResult(response.StatusCode, resultString);
        }

        public struct RequestResult
        {
            public HttpStatusCode statusCode { get; }
            public Stream result { get; }

            public RequestResult(HttpStatusCode statusCode, Stream result)
            {
                this.statusCode = statusCode;
                this.result = result;
            }
        }
    }
}