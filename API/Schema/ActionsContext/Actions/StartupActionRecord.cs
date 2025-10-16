namespace API.Schema.ActionsContext.Actions;

public sealed class StartupActionRecord(string action, DateTime performedAt) : ActionRecord(action, performedAt)
{
    public StartupActionRecord() : this(StartupAction, DateTime.UtcNow) { }
    
    public const string StartupAction = "Tranga.Started";
}