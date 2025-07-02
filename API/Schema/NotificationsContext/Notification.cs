using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.NotificationsContext;

[PrimaryKey(nameof(Key))]
public class Notification : Identifiable
{
    [Required]
    public NotificationUrgency Urgency { get; init; }

    [StringLength(128)]
    [Required]
    public string Title { get; init; }

    [StringLength(512)]
    [Required]
    public string Message { get; init; }
    
    [Required]
    public DateTime Date { get; init; }

    public Notification(string title, string message = "", NotificationUrgency urgency = NotificationUrgency.Normal, DateTime? date = null)
        : base(TokenGen.CreateToken("Notification"))
    {
        this.Title = title;
        this.Message = message;
        this.Urgency = urgency;
        this.Date = date ?? DateTime.UtcNow;
    }

    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    public Notification(string key, string title, string message, NotificationUrgency urgency, DateTime date)
        : base(key)
    {
        this.Title = title;
        this.Message = message;
        this.Urgency = urgency;
        this.Date = date;
    }

    public override string ToString() => $"{base.ToString()} {Urgency} {Title}";
}

public enum NotificationUrgency : byte
{
    Low = 1,
    Normal = 3,
    High = 5
}