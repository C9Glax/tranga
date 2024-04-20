using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2Jobs(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobsRunning(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobsWaiting(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobsMonitoring(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateMonitorInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateDownloadNewChaptersInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateUpdateMetadata(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateUpdateMetadataInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobJobId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2JobJobId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobJobIdProgress(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobJobIdStartNow(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobJobIdCancel(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
}