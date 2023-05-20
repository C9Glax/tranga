using System.Text;

namespace Logging;

public class FormattedConsoleLogger : LoggerBase
{
    
    public FormattedConsoleLogger(TextWriter? stdOut, Encoding? encoding = null) : base(stdOut, encoding)
    {
        
    }

    protected override void Write(LogMessage message)
    {
        //Nothing to do yet
    }
}