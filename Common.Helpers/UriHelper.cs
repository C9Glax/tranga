namespace Common.Helpers;

public static class UriHelper
{
    public static UriBuilder AddQueryParameter(this UriBuilder uri, string parameter, string value)
    {
        uri.Query += $"{parameter}={value}&";
        return uri;
    }
}