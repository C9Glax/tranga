using System.Text;

namespace Logging;

public abstract class LoggerBase : TextWriter
{
    public override Encoding Encoding { get; }

    public LoggerBase(Encoding? encoding = null)
    {
        this.Encoding = encoding ?? Encoding.ASCII;
    }

    public void Write(string caller, string? value)
    {
        if (value is null)
            return;

        LogMessage message = new (DateTime.Now, caller, value);
        
        Write(message);
    }

    protected abstract void Write(LogMessage message);
}