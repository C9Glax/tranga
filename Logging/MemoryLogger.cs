using System.Text;

namespace Logging;

public class MemoryLogger : LoggerBase
{
    private readonly SortedList<DateTime, LogMessage> _logMessages = new();
    private int _lastLogMessageIndex = 0;
    private bool _lockLogMessages = false;

    public MemoryLogger(Encoding? encoding = null) : base(encoding)
    {
        
    }

    protected override void Write(LogMessage value)
    {
        if (!_lockLogMessages)
        {
            _lockLogMessages = true;
            while(!_logMessages.TryAdd(DateTime.Now, value))
                Thread.Sleep(10);
            _lockLogMessages = false;
        }
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
            if (!_lockLogMessages)
            {
                _lockLogMessages = true;
                ret[retIndex] = _logMessages.GetValueAtIndex(_logMessages.Count - retLength + retIndex).ToString();
                _lockLogMessages = false;
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
                if (!_lockLogMessages)
                {
                    _lockLogMessages = true;
                    ret.Add(_logMessages.GetValueAtIndex(_lastLogMessageIndex + retIndex).ToString());
                    _lockLogMessages = false;
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