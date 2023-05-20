using System.Text;

namespace Logging;

public abstract class LoggerBase : TextWriter
{
    public override Encoding Encoding { get; }
    private TextWriter? stdOut { get; }

    public LoggerBase(TextWriter? stdOut, Encoding? encoding = null)
    {
        this.Encoding = encoding ?? Encoding.ASCII;
        this.stdOut = stdOut;
    }
    
    public void WriteLine(object caller, string? value)
    {
        value = value is null ? Environment.NewLine : string.Join(value, Environment.NewLine);

        LogMessage message = new LogMessage(DateTime.Now, caller, value);
        
        Write(message);
    }

    public void Write(object caller, string? value)
    {
        if (value is null)
            return;

        LogMessage message = new LogMessage(DateTime.Now, caller, value);
        
        stdOut?.Write(message.ToString());
        
        Write(message);
    }

    protected abstract void Write(LogMessage message);

    public class LogMessage
    {
        public DateTime logTime { get; }
        public Type caller { get; }
        public string value { get; }

        public LogMessage(DateTime now, object caller, string value)
        {
            this.logTime = now;
            this.caller = caller.GetType();
            this.value = value;
        }

        public override string ToString()
        {
            string dateTimeString = $"{logTime.ToShortDateString()} {logTime.ToShortTimeString()}";
            string callerString = caller.ToString();
            return $"[{dateTimeString}] {callerString,-15} | {value}";
        }
    }
}