using System.Net;
using System.Text.RegularExpressions;
using Tranga.MangaConnectors;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2Settings(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.AsJObject());
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsUserAgent(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.userAgent);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsUserAgent(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (!requestParameters.TryGetValue("value", out string? userAgent))
        {
            TrangaSettings.UpdateUserAgent(null);
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Accepted, null);
        }
        else
        {
            TrangaSettings.UpdateUserAgent(userAgent);
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
        }
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsRateLimitTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, Enum.GetValues<RequestType>().ToDictionary(b =>(byte)b, b => Enum.GetName(b)) );
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsRateLimit(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.requestLimits);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsRateLimit(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        foreach (KeyValuePair<string, string> kv in requestParameters)
        {
            if(!Enum.TryParse(kv.Key, out RequestType requestType) ||
               !int.TryParse(kv.Value, out int requestsPerMinute))
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, null);
            TrangaSettings.UpdateRateLimit(requestType, requestsPerMinute);
        }
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.requestLimits);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsRateLimitType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !Enum.TryParse(groups[1].Value, out RequestType requestType))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"RequestType {groups[1].Value}");
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.requestLimits[requestType]);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsRateLimitType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !Enum.TryParse(groups[1].Value, out RequestType requestType))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"RequestType {groups[1].Value}");
        if (!requestParameters.TryGetValue("value", out string? requestsPerMinuteStr) ||
            !int.TryParse(requestsPerMinuteStr, out int requestsPerMinute))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "Errors parsing requestsPerMinute");
        TrangaSettings.UpdateRateLimit(requestType, requestsPerMinute);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsAprilFoolsMode(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.aprilFoolsMode);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsAprilFoolsMode(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (!requestParameters.TryGetValue("value", out string? trueFalseStr) ||
         !bool.TryParse(trueFalseStr, out bool trueFalse))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "Errors parsing 'value'");
        TrangaSettings.UpdateAprilFoolsMode(trueFalse);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsCompressImages(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.compressImages);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsCompressImages(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (!requestParameters.TryGetValue("value", out string? trueFalseStr) ||
            !bool.TryParse(trueFalseStr, out bool trueFalse))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "Errors parsing 'value'");
        TrangaSettings.UpdateCompressImages(trueFalse);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2SettingsBwImages(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, TrangaSettings.bwImages);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsBwImages(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (!requestParameters.TryGetValue("value", out string? trueFalseStr) ||
            !bool.TryParse(trueFalseStr, out bool trueFalse))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "Errors parsing 'value'");
        TrangaSettings.UpdateBwImages(trueFalse);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2SettingsDownloadLocation(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (!requestParameters.TryGetValue("location", out string? folderPath))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, "Missing Parameter 'location'");
        try
        {
            bool moveFiles = requestParameters.TryGetValue("moveFiles", out string? moveFilesStr) switch
            {
                false => true,
                true => bool.Parse(moveFilesStr!)
            };
            TrangaSettings.UpdateDownloadLocation(folderPath, moveFiles);
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
        }
        catch (FormatException)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "Error Parsing Parameter 'moveFiles'");
        }
    }
}