using System.Text;

namespace Logging;

public class FormattedConsoleLogger : LoggerBase
{
    private readonly TextWriter _stdOut;
    public FormattedConsoleLogger(TextWriter stdOut, Encoding? encoding = null) : base(encoding)
    {
        this._stdOut = stdOut;
    }

    protected override void Write(LogMessage message)
    {
        this._stdOut.Write(message.formattedMessage);
    }
}