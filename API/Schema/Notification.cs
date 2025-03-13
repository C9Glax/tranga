using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("NotificationId")]
public class Notification(string title, string message = "", NotificationUrgency urgency = NotificationUrgency.Normal, DateTime? date = null)
{
    [StringLength(64)]
    [Required]
    public string NotificationId { get; init; } = TokenGen.CreateToken("Notification");

    [Required]
    public NotificationUrgency Urgency { get; init; } = urgency;

    [StringLength(128)]
    [Required]
    public string Title { get; init; } = title;

    [StringLength(512)]
    [Required]
    public string Message { get; init; } = message;
    
    [Required]
    public DateTime Date { get; init; } = date ?? DateTime.UtcNow;
    
    public Notification() : this("") { }
}