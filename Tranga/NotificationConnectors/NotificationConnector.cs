using System.Text.RegularExpressions;
using log4net;
using log4net.Config;

namespace Tranga.NotificationConnectors;

public abstract class NotificationConnector
{
    protected readonly ILog log;
    protected API.Schema.NotificationConnectors.NotificationConnector info;
    protected static readonly Regex BaseUrlRex = new(@"https?:\/\/[0-9A-z\.-]+(:[0-9]+)?");
    protected readonly HttpClient _client = new();

    protected NotificationConnector(API.Schema.NotificationConnectors.NotificationConnector info)
    {
        log = LogManager.GetLogger(this.GetType());
        BasicConfigurator.Configure();
        this.info = info;
    }
    
    public abstract void SendNotification(string title, string notificationText);
}