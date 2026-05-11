namespace Extensions.Extensions.NaprisExtensions;

public sealed class NtfySh(string serviceUrl) : NapriseExtension(serviceUrl)
{
   
    public NtfySh(bool https, string host, int port, string topic, string? user = null, string? password = null) : this(CreateServiceUrl(https, host, port, topic, user, password))
    {
    }

    public static string CreateServiceUrl(bool https, string host, int port, string topic, string? user = null,
        string? password = null) =>
        user is not null && password is not null
            ? $"ntfy{(https ? "s" : string.Empty)}://{user}:{password}@{host}:{port}/{topic}"
            : $"ntfy{(https ? "s" : string.Empty)}://{host}:{port}/{topic}";
}