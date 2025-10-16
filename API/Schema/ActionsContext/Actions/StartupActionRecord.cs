namespace API.Schema.ActionsContext.Actions;

public sealed class StartupActionRecord(ActionsEnum action, DateTime performedAt) : ActionRecord(action, performedAt)
{
    public StartupActionRecord() : this(ActionsEnum.Startup, DateTime.UtcNow) { }
}