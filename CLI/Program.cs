using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GlaxLogger;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Tranga;

var app = new CommandApp<TrangaCli>();
return app.Run(args);

internal sealed class TrangaCli : Command<TrangaCli.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Directory to which downloaded Manga are saved")]
        [CommandOption("-d|--downloadLocation")]
        [DefaultValue(null)]
        public string? downloadLocation { get; init; }
        
        [Description("Directory in which application-data is saved")]
        [CommandOption("-w|--workingDirectory")]
        [DefaultValue(null)]
        public string? workingDirectory { get; init; }
        
        [Description("Path to save logfile to")]
        [CommandOption("-f|--fileLogger")]
        [DefaultValue(null)]
        public string? fileLoggerPath { get; init; }
        
        [Description("LogLevel")]
        [CommandOption("-l|--loglevel")]
        [DefaultValue(LogLevel.Information)]
        public LogLevel level { get; init; }
        
        [Description("Port on which to run API on")]
        [CommandOption("-p|--port")]
        [DefaultValue(null)]
        public int? apiPort { get; init; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        Logger logger = new (settings.level, settings.fileLoggerPath, Console.Out);

        TrangaSettings? trangaSettings = null;
        
        if (settings.downloadLocation is not null && settings.workingDirectory is not null)
        {
            trangaSettings = new TrangaSettings(settings.downloadLocation, settings.workingDirectory);
        }else if (settings.downloadLocation is not null)
        {
            if (trangaSettings is null)
                trangaSettings = new TrangaSettings(downloadLocation: settings.downloadLocation);
            else
                trangaSettings = new TrangaSettings(downloadLocation: settings.downloadLocation, settings.workingDirectory);
        }else if (settings.workingDirectory is not null)
        {
            if (trangaSettings is null)
                trangaSettings = new TrangaSettings(downloadLocation: settings.workingDirectory);
            else
                trangaSettings = new TrangaSettings(settings.downloadLocation, settings.workingDirectory);
        }
        else
        {
            trangaSettings = new TrangaSettings();
        }

        Directory.CreateDirectory(trangaSettings.downloadLocation);
        Directory.CreateDirectory(trangaSettings.workingDirectory);

        Tranga.Tranga? api = null;

        Thread trangaApi = new Thread(() =>
        {
            api = new(logger, trangaSettings);
        });
        trangaApi.Start();
        
        HttpClient client = new();
        
        bool exit = false;
        while (!exit)
        {
            string menuSelect = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Menu")
                    .PageSize(10)
                    .MoreChoicesText("Up/Down")
                    .AddChoices(new[]
                    {
                        "CustomRequest",
                        "Exit"
                    }));

            switch (menuSelect)
            {
                case "CustomRequest":
                    HttpMethod requestMethod = AnsiConsole.Prompt(
                        new SelectionPrompt<HttpMethod>()
                            .Title("Request Type")
                            .AddChoices(new[]
                            {
                                HttpMethod.Get,
                                HttpMethod.Delete, 
                                HttpMethod.Post
                            }));
                    string requestPath = AnsiConsole.Prompt(
                        new TextPrompt<string>("Request Path:"));
                    List<ValueTuple<string, string>> parameters = new();
                    while (AnsiConsole.Confirm("Add Parameter?"))
                    {
                        string name = AnsiConsole.Ask<string>("Parameter Name:");
                        string value = AnsiConsole.Ask<string>("Parameter Value:");
                        parameters.Add(new ValueTuple<string, string>(name, value));
                    }

                    string requestString = $"http://localhost:{trangaSettings.apiPortNumber}/{requestPath}";
                    if (parameters.Any())
                    {
                        requestString += "?";
                        foreach (ValueTuple<string, string> parameter in parameters)
                            requestString += $"{parameter.Item1}={parameter.Item2}&";
                    }

                    HttpRequestMessage request = new (requestMethod, requestString);
                    AnsiConsole.WriteLine($"Request: {request.Method} {request.RequestUri}");
                    HttpResponseMessage response;
                    if (AnsiConsole.Confirm("Send Request?"))
                        response = client.Send(request);
                    else break;
                    AnsiConsole.WriteLine($"Response: {(int)response.StatusCode} {response.StatusCode}");
                    AnsiConsole.WriteLine(response.Content.ReadAsStringAsync().Result);
                    break;
                case "Exit":
                    exit = true;
                    break;
            }
        }

        if (api is not null)
            api.keepRunning = false;
        
        return 0;
    }
}