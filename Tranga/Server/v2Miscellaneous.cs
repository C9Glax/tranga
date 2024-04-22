using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2LogFile(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (logger is null || !File.Exists(logger?.logFilePath))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, "Missing Logfile");
        }

        FileStream logFile = new (logger.logFilePath, FileMode.Open, FileAccess.Read);
        FileStream content = new(Path.GetTempFileName(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 0, FileOptions.DeleteOnClose);
        logFile.Position = 0;
        logFile.CopyTo(content);
        content.Position = 0;
        logFile.Dispose();
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, content);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2Ping(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, "Pong!");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2Ping(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, "Pong!");
    }
}