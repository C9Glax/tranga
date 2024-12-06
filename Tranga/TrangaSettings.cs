using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.MangaConnectors;

namespace Tranga;

public static class TrangaSettings
{
    [JsonIgnore] internal static readonly string DefaultUserAgent = $"Tranga ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")}) / 1.0";
    public static string downloadLocation { get; private set; } = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Downloads"));
    public static string workingDirectory { get; private set; } = Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
    public static int apiPortNumber { get; private set; } = 6531;
    public static string userAgent { get; private set; } = DefaultUserAgent;
    public static int compression{ get; private set; } = 40;
    public static bool bwImages { get; private set; } = false;
    [JsonIgnore] public static string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public static bool aprilFoolsMode { get; private set; } = true;
    [JsonIgnore]internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 250},
        {RequestType.MangaDexFeed, 250},
        {RequestType.MangaDexImage, 40},
        {RequestType.MangaImage, 60},
        {RequestType.MangaCover, 250},
        {RequestType.Default, 60}
    };

    public static Dictionary<RequestType, int> requestLimits { get; set; } = DefaultRequestLimits;

    public static JObject AsJObject()
    {
        JObject jobj = new JObject();
        jobj.Add("downloadLocation", JToken.FromObject(downloadLocation));
        jobj.Add("workingDirectory", JToken.FromObject(workingDirectory));
        jobj.Add("apiPortNumber", JToken.FromObject(apiPortNumber));
        jobj.Add("userAgent", JToken.FromObject(userAgent));
        jobj.Add("aprilFoolsMode", JToken.FromObject(aprilFoolsMode));
        jobj.Add("requestLimits", JToken.FromObject(requestLimits));
        jobj.Add("compression", JToken.FromObject(compression));
        jobj.Add("bwImages", JToken.FromObject(bwImages));
        return jobj;
    }

    public static string Serialize() => AsJObject().ToString();

    public static void Deserialize(string serialized)
    {
        JObject jobj = JObject.Parse(serialized);
        if (jobj.TryGetValue("downloadLocation", out JToken? dl))
            downloadLocation = dl.Value<string>()!;
        if (jobj.TryGetValue("workingDirectory", out JToken? wd))
            workingDirectory = wd.Value<string>()!;
        if (jobj.TryGetValue("apiPortNumber", out JToken? apn))
            apiPortNumber = apn.Value<int>();
        if (jobj.TryGetValue("userAgent", out JToken? ua))
            userAgent = ua.Value<string>()!;
        if (jobj.TryGetValue("aprilFoolsMode", out JToken? afm))
            aprilFoolsMode = afm.Value<bool>()!;
        if (jobj.TryGetValue("requestLimits", out JToken? rl))
            requestLimits = rl.ToObject<Dictionary<RequestType, int>>()!;
        if (jobj.TryGetValue("compression", out JToken? ci))
            compression = ci.Value<int>()!;
        if (jobj.TryGetValue("bwImages", out JToken? bwi))
            bwImages = bwi.Value<bool>()!;
    }
}