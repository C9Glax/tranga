namespace Common.Helpers;

public static class UriHelper
{
    public static UriBuilder AddQueryParameter(this UriBuilder uri, string parameter, string value)
    {
        uri.Query += $"{parameter}={value}&";
        return uri;
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="url"/> is an absolute http(s) URL.
    /// </summary>
    public static bool IsWebUrl(string url, out Uri? uri) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// Splits the path of <paramref name="url"/> into its non-empty segments, provided it is an absolute http(s) URL
    /// on <paramref name="host"/> (with or without a leading <c>www.</c>, case-insensitive).
    /// </summary>
    /// <param name="url">The URL to inspect.</param>
    /// <param name="host">The expected host, without <c>www.</c> (e.g. <c>anilist.co</c>).</param>
    /// <returns>The path segments, or <see langword="null"/> if <paramref name="url"/> is not on <paramref name="host"/>.</returns>
    public static string[]? GetPathSegmentsOnHost(string url, string host)
    {
        if (!IsWebUrl(url, out Uri? uri) || uri is null)
            return null;
        string actualHost = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        if (!actualHost.Equals(host, StringComparison.OrdinalIgnoreCase))
            return null;
        return uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }
}
