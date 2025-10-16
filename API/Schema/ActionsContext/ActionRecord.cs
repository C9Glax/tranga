using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.ActionsContext;

[PrimaryKey("Key")]
public abstract class ActionRecord(string action, DateTime performedAt) : Identifiable
{
    /// <summary>
    /// Constant string that describes the performed Action
    /// </summary>
    [StringLength(128)]
    public string Action { get; init; } = action;
    
    /// <summary>
    /// UTC Time when Action was performed
    /// </summary>
    public DateTime PerformedAt { get; init; } = performedAt;
}