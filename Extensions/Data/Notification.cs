namespace Extensions.Data;

public sealed record Notification(string Title, Level Level = Level.Info, string? Text = null, string? Markdown = null)
{
    public string Title { get; } = Title;
    public Level Level { get; } = Level;
    public string? Text { get; } = Text;
    public string? Markdown { get; } = Markdown;
}

public enum Level
{
    Info,
    Success,
    Warning,
    Error,
}