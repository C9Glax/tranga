using System.ComponentModel.DataAnnotations;

namespace API.Schema.ActionsContext.Actions;

public sealed class DataMovedActionRecord(Actions action, DateTime performedAt, string from, string to) : ActionRecord(action, performedAt)
{
    public DataMovedActionRecord(string from, string to) : this(Actions.DataMoved, DateTime.UtcNow, from, to) { }
    
    /// <summary>
    /// From path
    /// </summary>
    [StringLength(2048)]
    public string From { get; init; } = from;
    
    /// <summary>
    /// To path
    /// </summary>
    [StringLength(2048)]
    public string To { get; init; } = to;
}