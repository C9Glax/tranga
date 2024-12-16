using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("NotificationId")]
public class Notification(string title, string message = "", byte urgency = 2)
{
    [MaxLength(64)]
    public string NotificationId { get; init; } = TokenGen.CreateToken("Notification", 64);

    public byte Urgency { get; init; } = urgency;

    public string Title { get; init; } = title;

    public string Message { get; init; } = message;
}