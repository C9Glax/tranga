namespace Extensions.Extensions.NaprisExtensions;

public sealed class Telegram(string serviceUrl) : NapriseExtension(serviceUrl)
{
    public Telegram(string token, string chatId) : this(CreateServiceUrl(token, chatId))
    {
    }
    
    
    public static string CreateServiceUrl(string token, string chatId) => $"telegram://{token}@{chatId}";
}