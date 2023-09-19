namespace Logging;

public readonly struct LogMessage
{
    public DateTime logTime { get; }
    public string caller { get; }
    public string value { get; }
    public string formattedMessage => ToString();

    public LogMessage(DateTime messageTime, string caller, string value)
    {
        this.logTime = messageTime;
        this.caller = caller;
        this.value = value;
    }

    public override string ToString()
    {
        string dateTimeString = $"{logTime.ToShortDateString()} {logTime.ToLongTimeString()}.{logTime.Millisecond,-3}";
        string name = caller.Split(new char[] { '.', '+' }).Last();
        return $"[{dateTimeString}] {name.Substring(0, name.Length >= 13 ? 13 : name.Length),13} | {value}";
    }
}