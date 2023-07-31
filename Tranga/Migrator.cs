using System.Text.Json.Nodes;
using Logging;
using Newtonsoft.Json;
using Tranga.LibraryManagers;
using Tranga.NotificationManagers;
using Tranga.TrangaTasks;

namespace Tranga;

public static class Migrator
{
    internal static readonly ushort CurrentVersion = 17;
    public static void Migrate(string settingsFilePath, Logger? logger)
    {
        if (!File.Exists(settingsFilePath))
            return;
        JsonNode settingsNode = JsonNode.Parse(File.ReadAllText(settingsFilePath))!;
        ushort version = settingsNode["version"] is not null
            ? settingsNode["version"]!.GetValue<ushort>()
            : settingsNode["ts"]!["version"]!.GetValue<ushort>();
        logger?.WriteLine("Migrator", $"Migrating {version} -> {CurrentVersion}");
        switch (version)
        {
            case 15:
                MoveToCommonObjects(settingsFilePath, logger);
                TrangaSettings.SettingsJsonObject sjo = JsonConvert.DeserializeObject<TrangaSettings.SettingsJsonObject>(File.ReadAllText(settingsFilePath))!;
                RemoveUpdateLibraryTask(sjo.ts!, logger);
                break;
            case 16:
                MoveToCommonObjects(settingsFilePath, logger);
                break;
        }

        TrangaSettings.SettingsJsonObject sjo2 = JsonConvert.DeserializeObject<TrangaSettings.SettingsJsonObject>(
            File.ReadAllText(settingsFilePath),
            new JsonSerializerSettings
            {
                Converters =
                {
                    new TrangaTask.TrangaTaskJsonConverter(),
                    new NotificationManager.NotificationManagerJsonConverter(),
                    new LibraryManager.LibraryManagerJsonConverter()
                }
            })!;
        sjo2.ts!.version = CurrentVersion;
        sjo2.ts!.ExportSettings();
    }

    private static void RemoveUpdateLibraryTask(TrangaSettings settings, Logger? logger)
    {
        if (!File.Exists(settings.tasksFilePath))
            return;

        logger?.WriteLine("Migrator", "Removing old/deprecated UpdateLibraryTasks (v16)");
        string tasksJsonString = File.ReadAllText(settings.tasksFilePath);
        HashSet<TrangaTask> tasks = JsonConvert.DeserializeObject<HashSet<TrangaTask>>(tasksJsonString,
            new JsonSerializerSettings { Converters = { new TrangaTask.TrangaTaskJsonConverter() } })!;
        tasks.RemoveWhere(t => t.task == TrangaTask.Task.UpdateLibraries);
        File.WriteAllText(settings.tasksFilePath, JsonConvert.SerializeObject(tasks));
    }

    public static void MoveToCommonObjects(string settingsFilePath, Logger? logger)
    {
        if (!File.Exists(settingsFilePath))
            return;

        logger?.WriteLine("Migrator", "Moving Settings to commonObjects-structure (v17)");
        JsonNode node = JsonNode.Parse(File.ReadAllText(settingsFilePath))!;
        TrangaSettings ts = new(
            node["downloadLocation"]!.GetValue<string>(),
            node["workingDirectory"]!.GetValue<string>());
        JsonArray libraryManagers = node["libraryManagers"]!.AsArray();
        logger?.WriteLine("Migrator", $"\tGot {libraryManagers.Count} libraryManagers.");
        JsonNode? komgaNode = libraryManagers.FirstOrDefault(lm => lm["libraryType"].GetValue<byte>() == (byte)LibraryManager.LibraryType.Komga);
        JsonNode? kavitaNode = libraryManagers.FirstOrDefault(lm => lm["libraryType"].GetValue<byte>() == (byte)LibraryManager.LibraryType.Kavita);
        HashSet<LibraryManager> lms = new();
        if (komgaNode is not null)
            lms.Add(new Komga(komgaNode["baseUrl"]!.GetValue<string>(), komgaNode["auth"]!.GetValue<string>(), null));
        if (kavitaNode is not null)
            lms.Add(new Kavita(kavitaNode["baseUrl"]!.GetValue<string>(), kavitaNode["auth"]!.GetValue<string>(), null));
        
        JsonArray notificationManagers = node["notificationManagers"]!.AsArray();
        logger?.WriteLine("Migrator", $"\tGot {notificationManagers.Count} notificationManagers.");
        JsonNode? gotifyNode = notificationManagers.FirstOrDefault(nm =>
            nm["notificationManagerType"].GetValue<byte>() == (byte)NotificationManager.NotificationManagerType.Gotify);
        JsonNode? lunaSeaNode = notificationManagers.FirstOrDefault(nm =>
            nm["notificationManagerType"].GetValue<byte>() == (byte)NotificationManager.NotificationManagerType.LunaSea);
        HashSet<NotificationManager> nms = new();
        if (gotifyNode is not null)
            nms.Add(new Gotify(gotifyNode["endpoint"]!.GetValue<string>(), gotifyNode["appToken"]!.GetValue<string>()));
        if (lunaSeaNode is not null)
            nms.Add(new LunaSea(lunaSeaNode["id"]!.GetValue<string>()));

        CommonObjects co = new (lms, nms, logger, settingsFilePath);

        TrangaSettings.SettingsJsonObject sjo = new(ts, co);
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(sjo));
    }
}