using Common.Helpers;

namespace Extensions.Helpers;

/// <summary>Shared page/cover download logic of the download extensions.</summary>
internal static class ImageDownloader
{
    internal const int DefaultAttempts = 3;

    /// <summary>
    /// Downloads one image, retrying with a short backoff on transport errors and non-success statuses (e.g. 429 or a
    /// flaky CDN node). Returns null when all attempts fail.
    /// </summary>
    /// <param name="client">Client to send the request with</param>
    /// <param name="requestFactory">Creates a fresh request per attempt (a request cannot be sent twice)</param>
    /// <param name="decode">Optional in-place transformation of the downloaded bytes (e.g. decryption)</param>
    /// <param name="ct"></param>
    /// <param name="attempts">How often to try</param>
    internal static async Task<TrangaImage?> DownloadAsync(HttpClient client, Func<HttpRequestMessage> requestFactory,
        Action<byte[]>? decode, CancellationToken ct, int attempts = DefaultAttempts)
    {
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using HttpRequestMessage request = requestFactory();
                using HttpResponseMessage response = await client.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    byte[] data = await response.Content.ReadAsByteArrayAsync(ct);
                    decode?.Invoke(data);
                    TrangaImage image = new();
                    await image.WriteAsync(data, ct);
                    return image;
                }
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // Retried below.
            }

            if (attempt < attempts)
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
        }

        return null;
    }
}
