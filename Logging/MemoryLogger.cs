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
        string[] ret = new string[logMessages.Count];
        for (int logMessageIndex = 0; logMessageIndex < ret.Length; logMessageIndex++)
        {
            DateTime logTime = logMessages.GetValueAtIndex(logMessageIndex).logTime;
            string dateTimeString = $"{logTime.ToShortDateString()} {logTime.ToShortTimeString()}";
            string callerString = logMessages.GetValueAtIndex(logMessageIndex).caller.ToString();
            string value = $"[{dateTimeString}] {callerString} | {logMessages.GetValueAtIndex(logMessageIndex).value}";
            ret[logMessageIndex] = value;
        }

        return ret;
    }

    public string[] Tail(uint length)
    {
        string[] ret = new string[length];
        for (int logMessageIndex = logMessages.Count - 1; logMessageIndex > logMessageIndex - length; logMessageIndex--)
            ret[logMessageIndex] = logMessages.GetValueAtIndex(logMessageIndex).ToString();

        return ret.Reverse().ToArray();
    }
}