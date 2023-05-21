using System.Text;

namespace Logging;

public class MemoryLogger : LoggerBase
{
    private readonly SortedList<DateTime, LogMessage> _logMessages = new();
    private int _lastLogMessageIndex = 0;

    public MemoryLogger(TextWriter? stdOut, Encoding? encoding = null) : base(stdOut, encoding)
    {
        
    }

    protected override void Write(LogMessage value)
    {
        _logMessages.Add(value.logTime, value);
    }

    public string[] GetLogMessage()
    {
        return Tail(Convert.ToUInt32(_logMessages.Count));
    }

    public string[] Tail(uint? length)
    {
        int retLength;
        if (length is null || length > _logMessages.Count)
            retLength = _logMessages.Count;
        else
            retLength = (int)length;
        
        string[] ret = new string[retLength];

        for (int retIndex = 0; retIndex < ret.Length; retIndex++)
        {
            ret[retIndex] = _logMessages.GetValueAtIndex(_logMessages.Count - retLength + retIndex).ToString();
        }

        _lastLogMessageIndex = _logMessages.Count - 1;
        return ret;
    }

    public string[] GetNewLines()
    {
        int logMessageCount = _logMessages.Count;
        string[] ret = new string[logMessageCount - _lastLogMessageIndex];

        for (int retIndex = 0; retIndex < ret.Length; retIndex++)
        {
            ret[retIndex] = _logMessages.GetValueAtIndex(_lastLogMessageIndex + retIndex).ToString();
        }

        _lastLogMessageIndex = logMessageCount;
        return ret;
    }
}