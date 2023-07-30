using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Tranga.LibraryManagers;
using Tranga.NotificationManagers;
using Tranga.TrangaTasks;

namespace Tranga;

public static class Migrator
{
    private static readonly ushort CurrentVersion = 17;
    public static void Migrate(string settingsFilePath)
    {
        if (!File.Exists(settingsFilePath))
            return;
        JsonNode settingsNode = JsonNode.Parse(File.ReadAllText(settingsFilePath))!;
        ushort version = settingsNode["version"]!.GetValue<ushort>();
        switch (version)
        {
            case 15:
                MoveToCommonObjects(settingsFilePath);
                TrangaSettings.SettingsJsonObject sjo = JsonConvert.DeserializeObject<TrangaSettings.SettingsJsonObject>(File.ReadAllText(settingsFilePath))!;
                RemoveUpdateLibraryTask(sjo.ts!);
                break;
            case 16:
                MoveToCommonObjects(settingsFilePath);
                break;
        }
        TrangaSettings.SettingsJsonObject sjo2 = JsonConvert.DeserializeObject<TrangaSettings.SettingsJsonObject>(File.ReadAllText(settingsFilePath))!;
        sjo2.ts!.version = CurrentVersion;
        sjo2.ts!.ExportSettings();
    }

    private static void RemoveUpdateLibraryTask(TrangaSettings settings)
    {
        if (!File.Exists(settings.tasksFilePath))
            return;

        string tasksJsonString = File.ReadAllText(settings.tasksFilePath);
        HashSet<TrangaTask> tasks = JsonConvert.DeserializeObject<HashSet<TrangaTask>>(tasksJsonString, new JsonSerializerSettings { Converters = { new TrangaTask.TrangaTaskJsonConverter() } })!;
        tasks.RemoveWhere(t => t.task == TrangaTask.Task.UpdateLibraries);
        File.WriteAllText(settings.tasksFilePath, JsonConvert.SerializeObject(tasks));
    }

    public static void MoveToCommonObjects(string settingsFilePath)
    {
        if (!File.Exists(settingsFilePath))
            return;

        JsonNode node = JsonNode.Parse(File.ReadAllText(settingsFilePath))!;
        TrangaSettings settings = new(
            node["downloadLocation"]!.GetValue<string>(),
            node["workingDirectory"]!.GetValue<string>());
        JsonArray libraryManagers = node["libraryManagers"]!.AsArray();
        JsonNode? komgaNode = libraryManagers.FirstOrDefault(lm => lm["libraryType"].GetValue<byte>() == (byte)LibraryManager.LibraryType.Komga);
        JsonNode? kavitaNode = libraryManagers.FirstOrDefault(lm => lm["libraryType"].GetValue<byte>() == (byte)LibraryManager.LibraryType.Kavita);
        HashSet<LibraryManager> lms = new();
        if (komgaNode is not null)
            lms.Add(new Komga(komgaNode["baseUrl"]!.GetValue<string>(), komgaNode["auth"]!.GetValue<string>(), null));
        if (kavitaNode is not null)
            lms.Add(new Kavita(kavitaNode["baseUrl"]!.GetValue<string>(), kavitaNode["auth"]!.GetValue<string>(), null));
        
        JsonArray notificationManagers = node["notificationManagers"]!.AsArray();
        JsonNode? gotifyNode = notificationManagers.FirstOrDefault(nm =>
            nm["notificationManagerType"].GetValue<byte>() == (byte)NotificationManager.NotificationManagerType.Gotify);
        JsonNode? lunaSeaNode = notificationManagers.FirstOrDefault(nm =>
            nm["notificationManagerType"].GetValue<byte>() == (byte)NotificationManager.NotificationManagerType.LunaSea);
        HashSet<NotificationManager> nms = new();
        if (gotifyNode is not null)
            nms.Add(new Gotify(gotifyNode["endpoint"]!.GetValue<string>(), gotifyNode["appToken"]!.GetValue<string>()));
        if (lunaSeaNode is not null)
            nms.Add(new LunaSea(lunaSeaNode["id"]!.GetValue<string>()));

        CommonObjects co = new (lms, nms, null, settingsFilePath);

        TrangaSettings.SettingsJsonObject sjo = new(settings, co);
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(sjo));
    }
}