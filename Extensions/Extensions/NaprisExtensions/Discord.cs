namespace Extensions.Extensions.NaprisExtensions;

public sealed class Discord(string serviceUrl) : NapriseExtension(serviceUrl)
{
    public Discord(string webhookId, string webhookToken) : this(CreateServiceUrl(webhookId, webhookToken))
    {
    }
    
    public static string CreateServiceUrl(string webhookId, string webhookToken) =>
        $"discord://{webhookId}/{webhookToken}?username=Tranga";
}