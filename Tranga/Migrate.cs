using Logging;
using Newtonsoft.Json;

namespace Tranga;

public static class Migrate
{
    private static readonly ushort CurrentVersion = 16;
    public static void Files(TrangaSettings settings)
    {
        settings.version ??= 15;
        switch (settings.version)
        {
            case 15:
                RemoveUpdateLibraryTask(settings);
                break;
        }

        settings.version = CurrentVersion;
        settings.ExportSettings();
    }

    private static void RemoveUpdateLibraryTask(TrangaSettings settings)
    {
        if (!File.Exists(settings.tasksFilePath))
            return;

        string tasksJsonString = File.ReadAllText(settings.tasksFilePath);
        List<TrangaTask> tasks = JsonConvert.DeserializeObject<List<TrangaTask>>(tasksJsonString, new JsonSerializerSettings { Converters = { new TrangaTask.TrangaTaskJsonConverter() } })!;
        tasks.RemoveAll(t => t.task == TrangaTask.Task.UpdateLibraries);
    }
}