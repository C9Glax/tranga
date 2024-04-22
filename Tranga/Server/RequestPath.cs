using System.Net;
using System.Text.RegularExpressions;

namespace Tranga.Server;

internal struct RequestPath
{
    internal readonly string HttpMethod;
    internal readonly string RegexStr;
    internal readonly Func<GroupCollection, Dictionary<string, string>, ValueTuple<HttpStatusCode, object?>> Method;

    public RequestPath(string httpHttpMethod, string regexStr,
        Func<GroupCollection, Dictionary<string, string>, ValueTuple<HttpStatusCode, object?>> method)
    {
        this.HttpMethod = httpHttpMethod;
        this.RegexStr = regexStr + "(?:/?)";
        this.Method = method;
    }
}