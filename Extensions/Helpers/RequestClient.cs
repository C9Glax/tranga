using System.Net.Http.Json;

namespace Extensions.Helpers;

internal sealed class RequestClient : HttpClient
{
    public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return base.Send(request, cancellationToken);
    }

    public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return base.SendAsync(request, cancellationToken);
    }

    public async Task<T?> SendAsyncAndParseJson<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await this.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }
}