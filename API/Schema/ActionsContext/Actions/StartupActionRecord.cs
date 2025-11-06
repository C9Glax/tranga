namespace API.Schema.ActionsContext.Actions;

public sealed class StartupActionRecord(Actions action, DateTime performedAt) : ActionRecord(action, performedAt)
{
    public StartupActionRecord() : this(Actions.Startup, DateTime.UtcNow) { }
}