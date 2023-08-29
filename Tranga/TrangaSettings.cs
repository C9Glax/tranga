﻿using System.Runtime.InteropServices;
using Logging;
using Newtonsoft.Json;
using Tranga.LibraryConnectors;
using Tranga.NotificationConnectors;
using static System.IO.UnixFileMode;

namespace Tranga;

public class TrangaSettings
{
    public string downloadLocation { get; private set; }
    public string workingDirectory { get; private set; }
    public int apiPortNumber { get; init; }
    [JsonIgnore] public string settingsFilePath => Path.Join(workingDirectory, "settings.json");
    [JsonIgnore] public string libraryConnectorsFilePath => Path.Join(workingDirectory, "libraryConnectors.json");

    [JsonIgnore] public string notificationConnectorsFilePath => Path.Join(workingDirectory, "notificationConnectors.json");
    [JsonIgnore] public string tasksFilePath => Path.Join(workingDirectory, "tasks.json");
    [JsonIgnore] public string coverImageCache => Path.Join(workingDirectory, "imageCache");
    public ushort? version { get; set; }

    public TrangaSettings(string? downloadLocation = null, string? workingDirectory = null, int? apiPortNumber = null)
    {
        string lockFilePath = $"{settingsFilePath}.lock";
        if (File.Exists(settingsFilePath) && !File.Exists(lockFilePath))
        {//Load from settings file
            FileStream lockFile = File.Create(lockFilePath,0, FileOptions.DeleteOnClose);
            string settingsStr = File.ReadAllText(settingsFilePath);
            TrangaSettings settings = JsonConvert.DeserializeObject<TrangaSettings>(settingsStr)!;
            this.downloadLocation = downloadLocation ?? settings.downloadLocation;
            this.workingDirectory = workingDirectory ?? settings.workingDirectory;
            this.apiPortNumber = apiPortNumber ?? settings.apiPortNumber;
            lockFile.Close();
        }
        else if(!File.Exists(settingsFilePath))
        {//No settings file exists
            if (downloadLocation?.Length < 1 || workingDirectory?.Length < 1)
                throw new ArgumentException("Download-location and working-directory paths can not be empty!");
            this.apiPortNumber = apiPortNumber ?? 6531;
            this.downloadLocation = downloadLocation ?? Path.Join(Directory.GetCurrentDirectory(), "Downloads");
            this.workingDirectory = workingDirectory ?? Directory.GetCurrentDirectory();
        }
        else
        {//Settingsfile is locked
            this.apiPortNumber = apiPortNumber!.Value;
            this.downloadLocation = downloadLocation!;
            this.workingDirectory = workingDirectory!;
        }
        UpdateDownloadLocation(this.downloadLocation!, false);
    }

    public HashSet<LibraryConnector> LoadLibraryConnectors()
    {
        if (!File.Exists(libraryConnectorsFilePath))
            return new HashSet<LibraryConnector>();
        return JsonConvert.DeserializeObject<HashSet<LibraryConnector>>(File.ReadAllText(libraryConnectorsFilePath),
            new JsonSerializerSettings()
            {
                Converters =
                {
                    new LibraryManagerJsonConverter()
                }
            })!;
    }

    public HashSet<NotificationConnector> LoadNotificationConnectors()
    {
        if (!File.Exists(notificationConnectorsFilePath))
            return new HashSet<NotificationConnector>();
        return JsonConvert.DeserializeObject<HashSet<NotificationConnector>>(File.ReadAllText(libraryConnectorsFilePath),
            new JsonSerializerSettings()
            {
                Converters =
                {
                    new NotificationManagerJsonConverter()
                }
            })!;
    }

    public void UpdateDownloadLocation(string newPath, bool moveFiles = true)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        
        if (moveFiles && Directory.Exists(this.downloadLocation))
            Directory.Move(this.downloadLocation, newPath);

        this.downloadLocation = newPath;
        ExportSettings();
    }

    public void UpdateWorkingDirectory(string newPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Directory.CreateDirectory(newPath,
                GroupRead | GroupWrite | None | OtherRead | OtherWrite | UserRead | UserWrite);
        else
            Directory.CreateDirectory(newPath);
        Directory.Move(this.workingDirectory, newPath);
        this.workingDirectory = newPath;
        ExportSettings();
    }

    public void ExportSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            bool inUse = true;
            while (inUse)
            {
                try
                {
                    using FileStream stream = new (settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    stream.Close();
                    inUse = false;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }
        File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(this));
    }
}