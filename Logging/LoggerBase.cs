using System.Text;

namespace Logging;

public abstract class LoggerBase : TextWriter
{
    public override Encoding Encoding { get; }

    public LoggerBase(Encoding? encoding = null)
    {
        this.Encoding = encoding ?? Encoding.ASCII;
    }
    protected abstract void Write(LogMessage message);
}