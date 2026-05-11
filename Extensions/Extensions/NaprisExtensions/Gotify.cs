namespace Extensions.Extensions.NaprisExtensions;

public sealed class Gotify(string serviceUrl) : NapriseExtension(serviceUrl)
{
    public Gotify(bool https, string host, int port, string apptoken) : this(CreateServiceUrl(https, host, port, apptoken))
    {
    }

    public static string CreateServiceUrl(bool https, string host, int port, string apptoken) =>
        $"gotify{(https ? "s" : string.Empty)}://{host}:{port}/{apptoken}";
}