using Logging;

namespace Tranga;

public abstract class GlobalBase
{
    protected Logger? logger { get; init; }
    protected TrangaSettings settings { get; init; }

    public GlobalBase(GlobalBase clone)
    {
        this.logger = clone.logger;
        this.settings = clone.settings;
    }

    public GlobalBase(Logger? logger, TrangaSettings settings)
    {
        this.logger = logger;
        this.settings = settings;
    }

    protected void Log(string message)
    {
        logger?.WriteLine(this.GetType().Name, message);
    }

    protected void Log(string fStr, params object?[] replace)
    {
        Log(string.Format(fStr, replace));
    }

    protected bool IsFileInUse(string filePath)
    {
        if (!File.Exists(filePath))
            return false;
        try
        {
            using FileStream stream = new (filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
            return false;
        }
        catch (IOException)
        {
            Log($"File is in use {filePath}");
            return true;
        }
    }
}