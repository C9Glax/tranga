using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tranga.MangaConnectors;

namespace Tranga;

public class TrangaSettings
{
    [JsonIgnore] internal static readonly string DefaultUserAgent = $"Tranga ({Enum.GetName(Environment.OSVersion.Platform)}; {(Environment.Is64BitOperatingSystem ? "x64" : "")}) / 1.0";
    public string downloadLocation { get; private set; } = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(Directory.GetCurrentDirectory(), "Downloads"));
    public string workingDirectory { get; private set; } = Path.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/usr/share" : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "tranga-api");
    public int apiPortNumber { get; private set; } = 6531;
    public string userAgent { get; private set; } = DefaultUserAgent;
    public bool bufferLibraryUpdates { get; private set; } = false;
    public bool bufferNotifications { get; private set; } = false;
    public int compression{ get; private set; } = 40;
    public bool bwImages { get; private set; } = false;
    [JsonIgnore] public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public string libraryConnectorsFilePath => Path.Join(workingDirectory, "libraryConnectors.json");
    [JsonIgnore] public string notificationConnectorsFilePath => Path.Join(workingDirectory, "notificationConnectors.json");
    [JsonIgnore] public string jobsFolderPath => Path.Join(workingDirectory, "jobs");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    [JsonIgnore] public string mangaCacheFolderPath => Path.Join(workingDirectory, "mangaCache");
    public bool aprilFoolsMode { get; private set; } = true;
    [JsonIgnore]internal static readonly Dictionary<RequestType, int> DefaultRequestLimits = new ()
    {
        {RequestType.MangaInfo, 250},
        {RequestType.MangaDexFeed, 250},
        {RequestType.MangaDexImage, 40},
        {RequestType.MangaImage, 60},
        {RequestType.MangaCover, 250},
        {RequestType.Default, 60}
    };

    public Dictionary<RequestType, int> requestLimits { get; set; } = DefaultRequestLimits;

    public JObject AsJObject()
    {
        JObject jobj = new JObject();
        jobj.Add("downloadLocation", JToken.FromObject(downloadLocation));
        jobj.Add("workingDirectory", JToken.FromObject(workingDirectory));
        jobj.Add("apiPortNumber", JToken.FromObject(apiPortNumber));
        jobj.Add("userAgent", JToken.FromObject(userAgent));
        jobj.Add("aprilFoolsMode", JToken.FromObject(aprilFoolsMode));
        jobj.Add("requestLimits", JToken.FromObject(requestLimits));
        jobj.Add("bufferLibraryUpdates", JToken.FromObject(bufferLibraryUpdates));
        jobj.Add("bufferNotifications", JToken.FromObject(bufferNotifications));
        jobj.Add("compression", JToken.FromObject(compression));
        jobj.Add("bwImages", JToken.FromObject(bwImages));
        return jobj;
    }

    public string Serialize() => AsJObject().ToString();

    public static TrangaSettings Deserialize(string serialized)
    {
        TrangaSettings ret = new();
        JObject jobj = JObject.Parse(serialized);
        if (jobj.TryGetValue("downloadLocation", out JToken? dl))
            ret.downloadLocation = dl.Value<string>()!;
        if (jobj.TryGetValue("workingDirectory", out JToken? wd))
            ret.workingDirectory = wd.Value<string>()!;
        if (jobj.TryGetValue("apiPortNumber", out JToken? apn))
            ret.apiPortNumber = apn.Value<int>();
        if (jobj.TryGetValue("userAgent", out JToken? ua))
            ret.userAgent = ua.Value<string>()!;
        if (jobj.TryGetValue("aprilFoolsMode", out JToken? afm))
            ret.aprilFoolsMode = afm.Value<bool>()!;
        if (jobj.TryGetValue("requestLimits", out JToken? rl))
            ret.requestLimits = rl.ToObject<Dictionary<RequestType, int>>()!;
        if (jobj.TryGetValue("bufferLibraryUpdates", out JToken? blu))
            ret.bufferLibraryUpdates = blu.Value<bool>()!;
        if (jobj.TryGetValue("bufferNotifications", out JToken? bn))
            ret.bufferNotifications = bn.Value<bool>()!;
        if (jobj.TryGetValue("compression", out JToken? ci))
            ret.compression = ci.Value<int>()!;
        if (jobj.TryGetValue("bwImages", out JToken? bwi))
            ret.bwImages = bwi.Value<bool>()!;
        return ret;
    }
}