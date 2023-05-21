using System.Text;

namespace Logging;

public class MemoryLogger : LoggerBase
{
    private SortedList<DateTime, LogMessage> logMessages = new();

    public MemoryLogger(TextWriter? stdOut, Encoding? encoding = null) : base(stdOut, encoding)
    {
        
    }

    protected override void Write(LogMessage value)
    {
        logMessages.Add(value.logTime, value);
    }

    public string[] GetLogMessage()
    {
        return Tail(Convert.ToUInt32(logMessages.Count));
    }

    public string[] Tail(uint? length)
    {
        int retLength;
        if (length is null || length > logMessages.Count)
            retLength = logMessages.Count;
        else
            retLength = (int)length;
        
        string[] ret = new string[retLength];
        
        for (int logMessageIndex = logMessages.Count - retLength; logMessageIndex < logMessages.Count; logMessageIndex++)
            ret[logMessageIndex + retLength - logMessages.Count] = logMessages.GetValueAtIndex(logMessageIndex).ToString();

        return ret;
    }
}