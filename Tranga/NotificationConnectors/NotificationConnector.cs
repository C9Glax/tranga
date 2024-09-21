namespace Tranga.NotificationConnectors;

public abstract class NotificationConnector : GlobalBase
{
    public readonly NotificationConnectorType notificationConnectorType;
    private DateTime? _notificationRequested = null;
    private readonly Thread? _notificationBufferThread = null;
    private const int NoChangeTimeout = 3, BiggestInterval = 30;
    private List<KeyValuePair<string, string>> _notifications = new();

    protected NotificationConnector(GlobalBase clone, NotificationConnectorType notificationConnectorType) : base(clone)
    {
        Log($"Creating notificationConnector {Enum.GetName(notificationConnectorType)}");
        this.notificationConnectorType = notificationConnectorType;
        
        
        if (TrangaSettings.bufferLibraryUpdates)
        {
            _notificationBufferThread = new(CheckNotificationBuffer);
            _notificationBufferThread.Start();
        }
    }
    
    private void CheckNotificationBuffer()
    {
        while (true)
        {
            if (_notificationRequested is not null && DateTime.Now.Subtract((DateTime)_notificationRequested) > TimeSpan.FromMinutes(NoChangeTimeout)) //If no updates have been requested for NoChangeTimeout minutes, update library
            {
                string[] uniqueTitles = _notifications.DistinctBy(n => n.Key).Select(n => n.Key).ToArray();
                Log($"Notification Buffer sending! Notifications: {string.Join(", ", uniqueTitles)}");
                foreach (string ut in uniqueTitles)
                {
                    string[] texts = _notifications.Where(n => n.Key == ut).Select(n => n.Value).ToArray();
                    SendNotificationInternal($"{ut} ({texts.Length})", string.Join('\n', texts));
                }
                _notificationRequested = null;
            }
            Thread.Sleep(100);
        }
    }
    
    public enum NotificationConnectorType : byte { Gotify = 0, LunaSea = 1, Ntfy = 2 }

    public void SendNotification(string title, string notificationText, bool buffer = false)
    {
        _notificationRequested ??= DateTime.Now;
        if (!TrangaSettings.bufferLibraryUpdates || !buffer)
        {
            SendNotificationInternal(title, notificationText);
            return;
        }
        _notifications.Add(new(title, notificationText));
        if (_notificationRequested is not null &&
                  DateTime.Now.Subtract((DateTime)_notificationRequested) > TimeSpan.FromMinutes(BiggestInterval)) //If the last update has been more than BiggestInterval minutes ago, update library
        {
            string[] uniqueTitles = _notifications.DistinctBy(n => n.Key).Select(n => n.Key).ToArray();
            foreach (string ut in uniqueTitles)
            {
                string[] texts = _notifications.Where(n => n.Key == ut).Select(n => n.Value).ToArray();
                SendNotificationInternal(ut, string.Join('\n', texts));
            }
            _notificationRequested = null;
        }
        else if(_notificationRequested is not null)
        {
            Log($"Buffering Notifications (Updates in latest {((DateTime)_notificationRequested).Add(TimeSpan.FromMinutes(BiggestInterval)).Subtract(DateTime.Now)} or {((DateTime)_notificationRequested).Add(TimeSpan.FromMinutes(NoChangeTimeout)).Subtract(DateTime.Now)})");
        }
    }
    
    protected abstract void SendNotificationInternal(string title, string notificationText);
}