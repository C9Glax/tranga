using System.ComponentModel.DataAnnotations;
using API.Schema.ActionsContext.Actions;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.ActionsContext;

[PrimaryKey("Key")]
public abstract class ActionRecord(ActionsEnum action, DateTime performedAt) : Identifiable
{
    /// <summary>
    /// Constant string that describes the performed Action
    /// </summary>
    [StringLength(128)]
    public ActionsEnum Action { get; init; } = action;
    
    /// <summary>
    /// UTC Time when Action was performed
    /// </summary>
    public DateTime PerformedAt { get; init; } = performedAt;
}