using System.Text;

namespace Logging;

public class MemoryLogger : LoggerBase
{
    private readonly SortedList<DateTime, LogMessage> _logMessages = new();
    private int _lastLogMessageIndex = 0;

    public MemoryLogger(Encoding? encoding = null) : base(encoding)
    {
        
    }

    protected override void Write(LogMessage value)
    {
        lock (_logMessages)
        {
            _logMessages.Add(DateTime.Now, value);
        }
    }

    public string[] GetLogMessages()
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
            lock (_logMessages)
            {
                ret[retIndex] = _logMessages.GetValueAtIndex(_logMessages.Count - retLength + retIndex).ToString();
            }
        }

        _lastLogMessageIndex = _logMessages.Count - 1;
        return ret;
    }

    public string[] GetNewLines()
    {
        int logMessageCount = _logMessages.Count;
        List<string> ret = new();

        int retIndex = 0;
        for (; retIndex < logMessageCount - _lastLogMessageIndex; retIndex++)
        {
            try
            {
                lock(_logMessages)
                {
                    ret.Add(_logMessages.GetValueAtIndex(_lastLogMessageIndex + retIndex).ToString());
                }
            }
            catch (NullReferenceException e)//Called when LogMessage has not finished writing
            {
                break;
            }
        }

        _lastLogMessageIndex = _lastLogMessageIndex + retIndex;
        return ret.ToArray();
    }
}